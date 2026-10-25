using System.IO;
using DesiMerge.Analytics;
using DesiMerge.Configuration;
using DesiMerge.Persistence;
using DesiMerge.Platform;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DesiMerge.Core
{
    /// <summary>
    /// Composition root. Lives in the Bootstrap scene (build index 0), builds <see cref="GameServices"/>
    /// once, survives scene loads, and hands the services to each loaded scene's <see cref="ISceneEntryPoint"/>.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class GameBootstrapper : MonoBehaviour
    {
        private const string SaveFolderName = "saves";

        private static GameBootstrapper _instance;

        [SerializeField] private GameConfig config;

        private ISaveStore _saveStoreOverride;
        private AppLifecycleHook _lifecycle;

        public GameServices Services { get; private set; }

        /// <summary>Test hook: call on an inactive GameObject before activating it.</summary>
        internal void Configure(GameConfig gameConfig, ISaveStore saveStore)
        {
            config = gameConfig;
            _saveStoreOverride = saveStore;
        }

        private void Awake()
        {
            if (_instance != null)
            {
                Destroy(gameObject);
                return;
            }

            if (config == null)
            {
                Debug.LogError("[Bootstrap] GameConfig is not assigned. Run 'Desi Merge > Setup > Create Foundation Assets'.", this);
                enabled = false;
                return;
            }

            foreach (var error in config.Validate())
            {
                Debug.LogError($"[Bootstrap] GameConfig: {error}", config);
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = config.TargetFrameRate;

            var saveStore = _saveStoreOverride ?? new FileSaveStore(Path.Combine(Application.persistentDataPath, SaveFolderName));
            Services = GameServicesFactory.Create(config, saveStore, Debug.isDebugBuild);

            _lifecycle = gameObject.AddComponent<AppLifecycleHook>();
            _lifecycle.Paused += Services.FlushAll;
            _lifecycle.Quitting += Services.FlushAll;
            SceneManager.sceneLoaded += OnSceneLoaded;

            Services.Analytics.TrackEvent(AnalyticsEvents.AppStarted);
        }

        private void Start()
        {
            if (Services != null && !string.IsNullOrEmpty(config.FirstSceneName))
            {
                SceneManager.LoadSceneAsync(config.FirstSceneName, LoadSceneMode.Single);
            }
        }

        private void OnDestroy()
        {
            if (_instance != this)
            {
                return;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
            _lifecycle.Paused -= Services.FlushAll;
            _lifecycle.Quitting -= Services.FlushAll;
            Services.FlushAll();
            _instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneEntryPoints.InitializeAll(scene, Services);
        }
    }
}
