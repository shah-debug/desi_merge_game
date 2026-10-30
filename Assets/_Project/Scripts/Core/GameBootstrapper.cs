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

        [Tooltip("Optional intro (an ISplashScreen) shown while the first scene loads.")]
        [SerializeField] private MonoBehaviour splash;

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
            // Audio sources live on this DontDestroyOnLoad object: one music player for the whole session.
            Services = GameServicesFactory.Create(config, saveStore, Debug.isDebugBuild, gameObject);

            _lifecycle = gameObject.AddComponent<AppLifecycleHook>();
            _lifecycle.Paused += FlushAll;
            _lifecycle.FocusLost += FlushAll;
            _lifecycle.Quitting += FlushAll;
            SceneManager.sceneLoaded += OnSceneLoaded;

            Services.Analytics.TrackEvent(AnalyticsEvents.AppStarted);
        }

        private void Start()
        {
            if (Services == null || string.IsNullOrEmpty(config.FirstSceneName))
            {
                return;
            }

            var load = SceneManager.LoadSceneAsync(config.FirstSceneName, LoadSceneMode.Single);
            if (load != null && splash is ISplashScreen intro)
            {
                // The menu activates when both the load and the short intro are done; there is no extra wait.
                load.allowSceneActivation = false;
                intro.Play(() => load.allowSceneActivation = true);
            }
        }

        private void OnDestroy()
        {
            if (_instance != this)
            {
                return;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
            _lifecycle.Paused -= FlushAll;
            _lifecycle.FocusLost -= FlushAll;
            _lifecycle.Quitting -= FlushAll;
            Services.FlushAll();
            Services.Dispose();
            _instance = null;
        }

        /// <summary>Saves pending data; each service only writes if it changed, so repeated calls are cheap.</summary>
        private void FlushAll() => Services.FlushAll();

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneEntryPoints.InitializeAll(scene, Services);
        }
    }
}
