using System;
using DesiMerge.Core;
using DesiMerge.UI.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.UI
{
    /// <summary>
    /// The in-game menu (hamburger, top-left). It drops down over the paused game and opens the real screens
    /// (Records, Collection, Daily Rewards, Missions, Settings, How to Play) inside the Gameplay scene, without
    /// loading another scene. One screen at a time; the gameplay loop owns the pause, so closing the menu resumes.
    /// </summary>
    public sealed class GameplayMenu : MonoBehaviour
    {
        [SerializeField] private PanelTransition transition;
        [SerializeField] private Button outsideButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button recordsButton;
        [SerializeField] private Button achievementsButton;
        [SerializeField] private Button collectionButton;
        [SerializeField] private Button dailyRewardsButton;
        [SerializeField] private Button missionsButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button helpButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private GameObject dailyRewardsDot;
        [SerializeField] private GameObject missionsDot;

        [Header("Screens")]
        [SerializeField] private RecordsPanel records;
        [SerializeField] private CollectionPanel collection;
        [SerializeField] private SettingsPanel settings;
        [SerializeField] private DailyRewardsPanel dailyRewards;
        [SerializeField] private MissionsPanel missions;
        [SerializeField] private RewardPresenter rewardPresenter;
        [SerializeField] private SkinsPanel skins;

        private bool _skinsOnly;

        private GameServices _services;
        private bool _opening;

        /// <summary>Close, outside tap, or "Return to game".</summary>
        public event Action ResumeRequested;
        public event Action HomeRequested;
        public event Action HelpRequested;

        public bool IsOpen => transition != null ? transition.IsVisible : gameObject.activeSelf;
        public RecordsPanel Records => records;
        public CollectionPanel Collection => collection;
        public SettingsPanel Settings => settings;
        public DailyRewardsPanel DailyRewards => dailyRewards;
        public MissionsPanel Missions => missions;
        public RewardPresenter Rewards => rewardPresenter;
        public SkinsPanel Skins => skins;
        internal Button OutsideButton => outsideButton;
        internal Button ResumeButton => resumeButton;
        internal Button RecordsButton => recordsButton;
        internal Button AchievementsButton => achievementsButton;
        internal Button CollectionButton => collectionButton;
        internal Button DailyRewardsButton => dailyRewardsButton;
        internal Button MissionsButton => missionsButton;
        internal Button SettingsButton => settingsButton;
        internal Button HelpButton => helpButton;
        internal Button HomeButton => homeButton;

        /// <summary>True while a screen opened from the menu (or the reward popup) is showing.</summary>
        public bool AnyScreenOpen =>
            (rewardPresenter != null && rewardPresenter.IsShowing) ||
            (records != null && records.IsOpen) ||
            (collection != null && collection.IsOpen) ||
            (settings != null && settings.IsOpen) ||
            (dailyRewards != null && dailyRewards.IsOpen) ||
            (missions != null && missions.IsOpen) ||
            (skins != null && skins.IsOpen);

        private void Awake()
        {
            Listen(outsideButton, RequestResume);
            Listen(closeButton, RequestResume);
            Listen(resumeButton, RequestResume);
            Listen(recordsButton, () => OpenScreen(() => records.Open(RecordsTab.Score)));
            Listen(achievementsButton, () => OpenScreen(() => records.Open(RecordsTab.Achievements)));
            Listen(collectionButton, () => OpenScreen(collection.Open));
            Listen(dailyRewardsButton, () => OpenScreen(dailyRewards.Open));
            Listen(missionsButton, () => OpenScreen(missions.Open));
            Listen(settingsButton, () => OpenScreen(settings.Open));
            Listen(helpButton, () => HelpRequested?.Invoke());
            Listen(homeButton, () => HomeRequested?.Invoke());
            if (skins != null)
            {
                skins.Closed += OnSkinsClosed;
            }

            if (dailyRewards != null)
            {
                dailyRewards.Closed += OnSkinsClosed;
            }

            if (!_opening)
            {
                SetVisible(false, immediate: true);
            }
        }

        private void OnDestroy()
        {
            if (_services != null)
            {
                _services.DailyRewards.Changed -= RefreshDots;
                _services.Missions.Changed -= RefreshDots;
            }

            foreach (var button in new[] { outsideButton, closeButton, resumeButton, recordsButton, achievementsButton, collectionButton, dailyRewardsButton, missionsButton, settingsButton, helpButton, homeButton })
            {
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                }
            }
        }

        public void Bind(GameServices services)
        {
            if (_services != null || services == null)
            {
                return;
            }

            _services = services;
            var audio = services.Audio;
            rewardPresenter?.Bind(audio, services.PowerUps);
            records?.Bind(services.Achievements, services.Progress, services.Milestones, services.ItemContent, services.Clock, audio);
            collection?.Bind(services.Progress, services.ItemContent, audio);
            settings?.Bind(services.Settings, audio);
            dailyRewards?.Bind(services.DailyRewards, services.PowerUps, rewardPresenter, audio);
            dailyRewards?.SetClock(services.Clock);
            missions?.Bind(services.Missions, services.DailyChallenges, services.Progress, services.PowerUps, services.Clock, rewardPresenter, audio);
            skins?.Bind(services.Cosmetics, services.Progress, services.ItemContent, audio);
            collection?.BindSkins(services.Cosmetics);
            foreach (var feedback in GetComponentsInChildren<ButtonFeedback>(true))
            {
                feedback.Bind(audio);
            }

            services.DailyRewards.Changed += RefreshDots;
            services.Missions.Changed += RefreshDots;
            RefreshDots();
        }

        public void Open()
        {
            _opening = true;
            _services?.DailyRewards.Refresh();
            _services?.Missions.Refresh();
            RefreshDots();
            SetVisible(true, immediate: false);
        }

        /// <summary>Closes the menu and anything opened from it (the game resumes).</summary>
        public void Close()
        {
            if (rewardPresenter != null && rewardPresenter.IsShowing)
            {
                rewardPresenter.Collect();
            }

            records?.Close();
            dailyRewards?.Close();
            missions?.Close();
            _skinsOnly = false;
            skins?.Close();
            if (collection != null && collection.IsOpen)
            {
                collection.Close();
            }

            if (settings != null && settings.IsOpen)
            {
                settings.Close();
            }

            if (IsOpen)
            {
                SetVisible(false, immediate: false);
            }
        }

        /// <summary>Android back inside the menu: closes the topmost screen. False when only the menu itself is open.</summary>
        public bool CloseTopScreen()
        {
            if (rewardPresenter != null && rewardPresenter.IsShowing)
            {
                rewardPresenter.Collect();
                return true;
            }

            if (records != null && records.IsOpen)
            {
                records.Close();
                return true;
            }

            if (skins != null && skins.IsOpen)
            {
                skins.Close();
                return true;
            }

            if (dailyRewards != null && dailyRewards.IsOpen)
            {
                dailyRewards.Close();
                return true;
            }

            if (missions != null && missions.IsOpen)
            {
                missions.Close();
                return true;
            }

            if (collection != null && collection.IsOpen)
            {
                collection.Close();
                return true;
            }

            if (settings != null && settings.IsOpen)
            {
                settings.Close();
                return true;
            }

            return false;
        }

        /// <summary>Opens only the Skins screen (from the Skin tag); closing it returns straight to the game.</summary>
        public void OpenSkinsOnly() => OpenOnly(skins);

        /// <summary>Opens only Daily Rewards (from the diamonds "+"); closing it returns straight to the game.</summary>
        public void OpenDailyRewardsOnly() => OpenOnly(dailyRewards);

        private void OpenOnly(MenuPanel panel)
        {
            if (panel == null || _services == null)
            {
                return;
            }

            _opening = true;
            _skinsOnly = true;
            panel.Open();
        }

        private void OnSkinsClosed()
        {
            if (_skinsOnly)
            {
                _skinsOnly = false;
                ResumeRequested?.Invoke();
            }
        }

        private void RequestResume()
        {
            if (!AnyScreenOpen)
            {
                ResumeRequested?.Invoke();
            }
        }

        private void OpenScreen(Action open)
        {
            if (_services != null && IsOpen && !AnyScreenOpen)
            {
                open();
            }
        }

        private void RefreshDots()
        {
            if (_services == null)
            {
                return;
            }

            if (dailyRewardsDot != null)
            {
                dailyRewardsDot.SetActive(_services.DailyRewards.CanClaim);
            }

            if (missionsDot != null)
            {
                missionsDot.SetActive(_services.Missions.ClaimableCount > 0 || _services.DailyChallenges.CanClaim);
            }
        }

        private void SetVisible(bool visible, bool immediate)
        {
            if (transition == null)
            {
                gameObject.SetActive(visible);
            }
            else if (immediate)
            {
                transition.SetVisibleImmediate(visible);
            }
            else if (visible)
            {
                transition.Show();
            }
            else
            {
                transition.Hide();
            }
        }

        private static void Listen(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }
    }
}
