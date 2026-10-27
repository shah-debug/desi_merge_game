using DesiMerge.Audio;
using DesiMerge.Configuration;
using DesiMerge.Core;
using DesiMerge.Persistence;
using DesiMerge.UI.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.UI
{
    /// <summary>
    /// Wires the MainMenu scene: Play opens the real Gameplay scene through the scene navigator; Collection,
    /// Achievements and Settings open their panels; the daily challenge card shows today's challenge.
    /// All data comes from services; nothing here reads or writes save files.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuEntryPoint : MonoBehaviour, ISceneEntryPoint
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button collectionButton;
        [SerializeField] private Button achievementsButton;
        [SerializeField] private Text bestScoreText;
        [SerializeField] private Text saveNoticeText;
        [SerializeField] private SettingsPanel settingsPanel;
        [SerializeField] private CollectionPanel collectionPanel;
        [SerializeField] private AchievementsPanel achievementsPanel;
        [SerializeField] private DailyChallengeCard dailyChallengeCard;

        [Tooltip("Editor/development only: config used when this scene is played without Bootstrap.")]
        [SerializeField] private GameConfig developmentConfig;

        private GameServices _services;
        private GameServices _ownedServices;
        private bool _initializeAttempted;

        public bool IsInitialized => _services != null;
        public Button PlayButton => playButton;
        public Button SettingsButton => settingsButton;
        public Button CollectionButton => collectionButton;
        public Button AchievementsButton => achievementsButton;
        public SettingsPanel Settings => settingsPanel;
        public CollectionPanel Collection => collectionPanel;
        public AchievementsPanel Achievements => achievementsPanel;
        public DailyChallengeCard DailyCard => dailyChallengeCard;
        public GameServices Services => _services;

        public void Initialize(GameServices services)
        {
            if (_initializeAttempted)
            {
                return;
            }

            _initializeAttempted = true;
            _services = services;
            var content = services.ItemContent;

            if (services.GameState.Current != GameState.MainMenu)
            {
                services.GameState.TryTransitionTo(GameState.MainMenu);
            }

            // The date may have changed since the last screen; this is the only "timer" the daily challenge needs.
            services.DailyChallenges.Refresh();

            if (bestScoreText != null)
            {
                bestScoreText.text = services.Progress.HighScore > 0 ? $"Best  {services.Progress.HighScore}" : string.Empty;
            }

            ShowSaveNotice(services.Progress.LoadOutcome);

            foreach (var feedback in GetComponentsInChildren<ButtonFeedback>(true))
            {
                feedback.Bind(services.Audio);
            }

            if (settingsPanel != null)
            {
                settingsPanel.Bind(services.Settings, services.Audio);
            }

            if (collectionPanel != null)
            {
                collectionPanel.Bind(services.Progress, content, services.Audio);
            }

            if (achievementsPanel != null)
            {
                achievementsPanel.Bind(services.Achievements, services.Progress, content, services.Audio);
            }

            if (dailyChallengeCard != null)
            {
                dailyChallengeCard.Bind(services.DailyChallenges, services.Progress);
            }

            AddListener(playButton, OnPlayClicked);
            AddListener(settingsButton, OnSettingsClicked);
            AddListener(collectionButton, OnCollectionClicked);
            AddListener(achievementsButton, OnAchievementsClicked);
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
            RemoveListener(playButton, OnPlayClicked);
            RemoveListener(settingsButton, OnSettingsClicked);
            RemoveListener(collectionButton, OnCollectionClicked);
            RemoveListener(achievementsButton, OnAchievementsClicked);
            _ownedServices?.Dispose();
            _ownedServices = null;
        }

        private void ShowSaveNotice(LoadOutcome outcome)
        {
            if (saveNoticeText == null)
            {
                return;
            }

            saveNoticeText.text = outcome == LoadOutcome.RecoveredFromBackup
                ? "Your progress was restored from a backup."
                : outcome == LoadOutcome.ResetToDefaults
                    ? "Saved progress couldn't be read, so a fresh profile was started. A copy of the old file was kept."
                    : string.Empty;
            saveNoticeText.gameObject.SetActive(saveNoticeText.text.Length > 0);
        }

        private void OnPlayClicked()
        {
            if (_services == null || !_services.Navigator.LoadGameplay())
            {
                return;
            }

            // The navigator already ignores repeat requests; disabling the buttons makes that visible.
            foreach (var button in new[] { playButton, settingsButton, collectionButton, achievementsButton })
            {
                if (button != null)
                {
                    button.interactable = false;
                }
            }
        }

        private void OnSettingsClicked()
        {
            if (settingsPanel != null && CanOpenPanel())
            {
                settingsPanel.Open();
            }
        }

        private void OnCollectionClicked()
        {
            if (collectionPanel != null && CanOpenPanel())
            {
                collectionPanel.Open();
            }
        }

        private void OnAchievementsClicked()
        {
            if (achievementsPanel != null && CanOpenPanel())
            {
                achievementsPanel.Open();
            }
        }

        private bool CanOpenPanel() => _services == null || !_services.Navigator.IsLoading;

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(action);
            }
        }
    }
}
