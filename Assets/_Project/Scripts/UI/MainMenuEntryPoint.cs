using DesiMerge.Audio;
using DesiMerge.Configuration;
using DesiMerge.Core;
using DesiMerge.UI.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.UI
{
    /// <summary>
    /// Wires the MainMenu scene: Play opens the real Gameplay scene through the scene navigator, Settings opens the
    /// settings panel. Only features that exist are shown (no collection/shop buttons yet).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuEntryPoint : MonoBehaviour, ISceneEntryPoint
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Text bestScoreText;
        [SerializeField] private SettingsPanel settingsPanel;

        [Tooltip("Editor/development only: config used when this scene is played without Bootstrap.")]
        [SerializeField] private GameConfig developmentConfig;

        private GameServices _services;
        private GameServices _ownedServices;
        private bool _initializeAttempted;

        public bool IsInitialized => _services != null;
        public Button PlayButton => playButton;
        public Button SettingsButton => settingsButton;
        public SettingsPanel Settings => settingsPanel;
        public GameServices Services => _services;

        public void Initialize(GameServices services)
        {
            if (_initializeAttempted)
            {
                return;
            }

            _initializeAttempted = true;
            _services = services;

            if (services.GameState.Current != GameState.MainMenu)
            {
                services.GameState.TryTransitionTo(GameState.MainMenu);
            }

            if (bestScoreText != null)
            {
                bestScoreText.text = services.Progress.HighScore > 0 ? $"Best  {services.Progress.HighScore}" : string.Empty;
            }

            foreach (var feedback in GetComponentsInChildren<ButtonFeedback>(true))
            {
                feedback.Bind(services.Audio);
            }

            if (settingsPanel != null)
            {
                settingsPanel.Bind(services.Settings, services.Audio);
            }

            if (playButton != null)
            {
                playButton.onClick.AddListener(OnPlayClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            services.Audio.PlayMusic(MusicTrack.Menu);
        }

        private void Start()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_initializeAttempted)
            {
                Debug.LogWarning("[MainMenu] Started without Bootstrap; using temporary in-memory development services.", this);
                _ownedServices = DevelopmentServices.Create(developmentConfig, gameObject);
                Initialize(_ownedServices);
            }
#endif
        }

        private void OnDestroy()
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveListener(OnPlayClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(OnSettingsClicked);
            }

            _ownedServices?.Dispose();
            _ownedServices = null;
        }

        private void OnPlayClicked()
        {
            if (_services == null || !_services.Navigator.LoadGameplay())
            {
                return;
            }

            // The navigator already ignores repeat requests; disabling the buttons makes that visible.
            playButton.interactable = false;
            if (settingsButton != null)
            {
                settingsButton.interactable = false;
            }
        }

        private void OnSettingsClicked()
        {
            if (settingsPanel != null && (_services == null || !_services.Navigator.IsLoading))
            {
                settingsPanel.Open();
            }
        }
    }
}
