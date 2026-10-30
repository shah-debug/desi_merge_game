using DesiMerge.Audio;
using DesiMerge.Configuration;
using DesiMerge.Core;
using DesiMerge.Platform;
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
        [SerializeField] private GameObject bestScoreBadge;
        [SerializeField] private Text saveNoticeText;
        [SerializeField] private SettingsPanel settingsPanel;
        [SerializeField] private CollectionPanel collectionPanel;
        [SerializeField] private AchievementsPanel achievementsPanel;
        [SerializeField] private DailyChallengeCard dailyChallengeCard;

        [Header("Rewards and progression")]
        [SerializeField] private Button dailyRewardsButton;
        [SerializeField] private Button missionsButton;
        [SerializeField] private Button rankButton;
        [SerializeField] private GameObject dailyRewardsDot;
        [SerializeField] private GameObject missionsDot;
        [SerializeField] private GameObject rankDot;
        [SerializeField] private Text rankTitleText;
        [SerializeField] private Text rankProgressText;
        [SerializeField] private Image rankProgressFill;
        [SerializeField] private DailyRewardsPanel dailyRewardsPanel;
        [SerializeField] private MissionsPanel missionsPanel;
        [SerializeField] private RanksPanel ranksPanel;
        [SerializeField] private RewardPresenter rewardPresenter;

        [Tooltip("Editor/development only: config used when this scene is played without Bootstrap.")]
        [SerializeField] private GameConfig developmentConfig;

        private GameServices _services;
        private GameServices _ownedServices;
        private BackButton _backButton;
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
        public Button DailyRewardsButton => dailyRewardsButton;
        public Button MissionsButton => missionsButton;
        public Button RankButton => rankButton;
        public DailyRewardsPanel DailyRewards => dailyRewardsPanel;
        public MissionsPanel Missions => missionsPanel;
        public RanksPanel Ranks => ranksPanel;
        public RewardPresenter Rewards => rewardPresenter;
        public bool DailyRewardsDotVisible => dailyRewardsDot != null && dailyRewardsDot.activeSelf;
        public bool MissionsDotVisible => missionsDot != null && missionsDot.activeSelf;
        public string RankTitle => rankTitleText != null ? rankTitleText.text : string.Empty;

        /// <summary>True while any menu panel or the reward popup is open.</summary>
        public bool AnyPanelOpen =>
            (rewardPresenter != null && rewardPresenter.IsShowing) ||
            (settingsPanel != null && settingsPanel.IsOpen) ||
            (collectionPanel != null && collectionPanel.IsOpen) ||
            (achievementsPanel != null && achievementsPanel.IsOpen) ||
            (dailyRewardsPanel != null && dailyRewardsPanel.IsOpen) ||
            (missionsPanel != null && missionsPanel.IsOpen) ||
            (ranksPanel != null && ranksPanel.IsOpen);
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

            if (bestScoreBadge != null)
            {
                bestScoreBadge.SetActive(services.Progress.HighScore > 0);
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

            services.DailyRewards.Refresh();
            services.Missions.Refresh();
            if (rewardPresenter != null)
            {
                rewardPresenter.Bind(services.Audio, services.PowerUps);
            }

            if (dailyRewardsPanel != null)
            {
                dailyRewardsPanel.Bind(services.DailyRewards, services.PowerUps, rewardPresenter, services.Audio);
            }

            if (missionsPanel != null)
            {
                missionsPanel.Bind(services.Missions, services.DailyChallenges, services.Progress, services.PowerUps, services.Clock, rewardPresenter, services.Audio);
            }
            else if (dailyChallengeCard != null)
            {
                dailyChallengeCard.Bind(services.DailyChallenges, services.Progress, services.PowerUps);
            }

            if (ranksPanel != null)
            {
                ranksPanel.Bind(services.Milestones, services.Progress, services.PowerUps, rewardPresenter, services.Audio);
            }

            services.DailyRewards.Changed += RefreshBadges;
            services.Missions.Changed += RefreshBadges;
            services.Milestones.Changed += RefreshBadges;
            services.DailyChallenges.Changed += RefreshBadges;
            RefreshBadges();
            AddListener(dailyRewardsButton, OnDailyRewardsClicked);
            AddListener(missionsButton, OnMissionsClicked);
            AddListener(rankButton, OnRankClicked);

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

        private void OnEnable() => _backButton = new BackButton(OnBackPressed);

        private void OnDisable()
        {
            _backButton?.Dispose();
            _backButton = null;
        }

        /// <summary>
        /// Android back: closes the open panel. On the bare menu it exits the app, as Android players expect
        /// (the bootstrapper saves on quit). Ignored while Gameplay is loading.
        /// </summary>
        private void OnBackPressed()
        {
            if (!HandleBack() && _services != null && !_services.Navigator.IsLoading && !Application.isEditor)
            {
                Application.Quit();
            }
        }

        /// <summary>Closes the topmost open panel. Returns false when no panel was open.</summary>
        internal bool HandleBack()
        {
            if (rewardPresenter != null && rewardPresenter.IsShowing)
            {
                rewardPresenter.Collect();
                return true;
            }

            if (dailyRewardsPanel != null && dailyRewardsPanel.IsOpen)
            {
                dailyRewardsPanel.Close();
                return true;
            }

            if (missionsPanel != null && missionsPanel.IsOpen)
            {
                missionsPanel.Close();
                return true;
            }

            if (ranksPanel != null && ranksPanel.IsOpen)
            {
                ranksPanel.Close();
                return true;
            }

            if (settingsPanel != null && settingsPanel.IsOpen)
            {
                settingsPanel.Close();
                return true;
            }

            if (collectionPanel != null && collectionPanel.IsOpen)
            {
                collectionPanel.Close();
                return true;
            }

            if (achievementsPanel != null && achievementsPanel.IsOpen)
            {
                achievementsPanel.Close();
                return true;
            }

            return false;
        }

        private void OnDestroy()
        {
            RemoveListener(playButton, OnPlayClicked);
            RemoveListener(settingsButton, OnSettingsClicked);
            RemoveListener(collectionButton, OnCollectionClicked);
            RemoveListener(achievementsButton, OnAchievementsClicked);
            RemoveListener(dailyRewardsButton, OnDailyRewardsClicked);
            RemoveListener(missionsButton, OnMissionsClicked);
            RemoveListener(rankButton, OnRankClicked);
            if (_services != null)
            {
                _services.DailyRewards.Changed -= RefreshBadges;
                _services.Missions.Changed -= RefreshBadges;
                _services.Milestones.Changed -= RefreshBadges;
                _services.DailyChallenges.Changed -= RefreshBadges;
            }

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
                    : outcome == LoadOutcome.ReadFailed
                        ? "Your saved progress couldn't be opened right now. It is safe; restart the game to load it."
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
            foreach (var button in new[] { playButton, settingsButton, collectionButton, achievementsButton, dailyRewardsButton, missionsButton, rankButton })
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

        private void OnDailyRewardsClicked()
        {
            if (dailyRewardsPanel != null && CanOpenPanel())
            {
                dailyRewardsPanel.Open();
            }
        }

        private void OnMissionsClicked()
        {
            if (missionsPanel != null && CanOpenPanel())
            {
                missionsPanel.Open();
            }
        }

        private void OnRankClicked()
        {
            if (ranksPanel != null && CanOpenPanel())
            {
                ranksPanel.Open();
            }
        }

        /// <summary>Notification dots and the rank card. Event-driven: called when a reward service changes.</summary>
        private void RefreshBadges()
        {
            if (_services == null)
            {
                return;
            }

            SetActive(dailyRewardsDot, _services.DailyRewards.CanClaim);
            SetActive(missionsDot, _services.Missions.ClaimableCount > 0 || _services.DailyChallenges.CanClaim);
            SetActive(rankDot, _services.Milestones.ClaimableCount > 0);

            var milestones = _services.Milestones;
            var current = milestones.Current;
            var next = milestones.Next;
            if (rankTitleText != null)
            {
                rankTitleText.text = current != null ? current.title : "New Player";
            }

            if (rankProgressText != null)
            {
                rankProgressText.text = next != null ? $"Next: {next.title} at {next.score}" : "Top rank reached!";
            }

            if (rankProgressFill != null)
            {
                rankProgressFill.fillAmount = milestones.ProgressToNext;
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        /// <summary>One panel at a time, and none while a scene is loading.</summary>
        private bool CanOpenPanel() => (_services == null || !_services.Navigator.IsLoading) && !AnyPanelOpen;

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
