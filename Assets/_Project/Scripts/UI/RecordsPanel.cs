using System;
using System.Collections.Generic;
using DesiMerge.Audio;
using DesiMerge.Configuration;
using DesiMerge.Core;
using DesiMerge.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.UI
{
    public enum RecordsTab
    {
        Achievements = 0,
        Score = 1,
    }

    /// <summary>
    /// Records: the player's real achievements (progress, locked/unlocked, reward) and local score records
    /// (best score, statistics, rank, the last games). Everything is read from the existing services; nothing is
    /// calculated or stored here. These are personal records on this phone; there is no online leaderboard.
    /// </summary>
    public sealed class RecordsPanel : MenuPanel
    {
        [SerializeField] private Button[] tabButtons = Array.Empty<Button>();
        [SerializeField] private Image[] tabBackgrounds = Array.Empty<Image>();
        [SerializeField] private Text[] tabLabels = Array.Empty<Text>();
        [SerializeField] private Color tabSelectedColor = Color.white;
        [SerializeField] private Color tabNormalColor = Color.white;
        [SerializeField] private Color tabSelectedText = Color.white;
        [SerializeField] private Color tabNormalText = Color.white;
        [SerializeField] private GameObject achievementsPage;
        [SerializeField] private GameObject scorePage;
        [SerializeField] private ScrollRect achievementsScroll;
        [SerializeField] private ScrollRect scoreScroll;

        [Header("Achievements")]
        [SerializeField] private RectTransform achievementList;
        [SerializeField] private ProgressCard achievementTemplate;
        [SerializeField] private Text achievementSummary;
        [SerializeField] private Sprite gamesIcon;
        [SerializeField] private Sprite mergesIcon;
        [SerializeField] private Sprite scoreIcon;
        [SerializeField] private Sprite discoverIcon;
        [SerializeField] private Sprite streakIcon;

        [Header("Score")]
        [SerializeField] private Text bestText;
        [SerializeField] private Text gamesText;
        [SerializeField] private Text mergesText;
        [SerializeField] private Text streakText;
        [SerializeField] private Text highestItemText;
        [SerializeField] private Image highestItemImage;
        [SerializeField] private Text rankText;
        [SerializeField] private RectTransform recentList;
        [SerializeField] private ScoreRecordRow recentTemplate;
        [SerializeField] private Text recentEmptyText;

        private readonly List<ProgressCard> _achievementRows = new List<ProgressCard>();
        private readonly List<ScoreRecordRow> _recentRows = new List<ScoreRecordRow>();
        private readonly UnityEngine.Events.UnityAction[] _tabActions = new UnityEngine.Events.UnityAction[2];
        private IAchievementService _achievements;
        private IPlayerProgressService _progress;
        private IMilestoneService _milestones;
        private MergeProgression _content;
        private IClock _clock;

        public RecordsTab Tab { get; private set; }
        public IReadOnlyList<ProgressCard> AchievementRows => _achievementRows;
        public IReadOnlyList<ScoreRecordRow> RecentRows => _recentRows;
        public string BestLabel => bestText != null ? bestText.text : string.Empty;
        public string RankLabel => rankText != null ? rankText.text : string.Empty;
        public bool AchievementsPageVisible => achievementsPage != null && achievementsPage.activeSelf;
        public bool ScorePageVisible => scorePage != null && scorePage.activeSelf;

        public void Bind(IAchievementService achievements, IPlayerProgressService progress, IMilestoneService milestones, MergeProgression content,
            IClock clock, IAudioService audio)
        {
            Unbind();
            _achievements = achievements ?? throw new ArgumentNullException(nameof(achievements));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _milestones = milestones;
            _content = content;
            _clock = clock ?? new SystemClock();
            _progress.Changed += Refresh;
            _achievements.Unlocked += OnUnlocked;
            for (var i = 0; i < tabButtons.Length && i < _tabActions.Length; i++)
            {
                var tab = (RecordsTab)i;
                _tabActions[i] = () => SelectTab(tab);
                if (tabButtons[i] != null)
                {
                    tabButtons[i].onClick.AddListener(_tabActions[i]);
                }
            }

            if (achievementTemplate != null)
            {
                achievementTemplate.gameObject.SetActive(false);
            }

            if (recentTemplate != null)
            {
                recentTemplate.gameObject.SetActive(false);
            }

            BindAudio(audio);
            SelectTab(RecordsTab.Achievements);
        }

        /// <summary>Opens the panel on a given tab.</summary>
        public void Open(RecordsTab tab)
        {
            Open();
            SelectTab(tab);
        }

        protected override void OnDestroy()
        {
            Unbind();
            base.OnDestroy();
        }

        protected override void OnOpening() => Refresh();

        public void SelectTab(RecordsTab tab)
        {
            Tab = tab;
            for (var i = 0; i < tabBackgrounds.Length; i++)
            {
                if (tabBackgrounds[i] != null)
                {
                    tabBackgrounds[i].color = i == (int)tab ? tabSelectedColor : tabNormalColor;
                }

                if (i < tabLabels.Length && tabLabels[i] != null)
                {
                    tabLabels[i].color = i == (int)tab ? tabSelectedText : tabNormalText;
                }
            }

            if (achievementsPage != null)
            {
                achievementsPage.SetActive(tab == RecordsTab.Achievements);
            }

            if (scorePage != null)
            {
                scorePage.SetActive(tab == RecordsTab.Score);
            }

            var scroll = tab == RecordsTab.Achievements ? achievementsScroll : scoreScroll;
            if (scroll != null)
            {
                scroll.verticalNormalizedPosition = 1f;
            }

            Refresh();
        }

        public void Refresh()
        {
            if (_achievements == null)
            {
                return;
            }

            RefreshAchievements();
            RefreshScore();
        }

        private void OnUnlocked(AchievementDefinition achievement) => Refresh();

        private void RefreshAchievements()
        {
            var definitions = _achievements.Definitions;
            if (achievementSummary != null)
            {
                achievementSummary.text = $"{_achievements.UnlockedCount} / {definitions.Count} unlocked";
            }

            if (achievementTemplate == null || achievementList == null)
            {
                return;
            }

            while (_achievementRows.Count < definitions.Count)
            {
                _achievementRows.Add(Instantiate(achievementTemplate, achievementList));
            }

            for (var i = 0; i < _achievementRows.Count; i++)
            {
                var row = _achievementRows[i];
                if (i >= definitions.Count)
                {
                    row.gameObject.SetActive(false);
                    continue;
                }

                var achievement = definitions[i];
                var unlocked = _achievements.IsUnlocked(achievement.id);
                var reward = achievement.reward.IsSomething ? (unlocked ? "Received" : RewardText(achievement.reward)) : string.Empty;
                row.Show(achievement.id, achievement.title, achievement.description, _achievements.ProgressOf(achievement), _achievements.TargetOf(achievement),
                    reward, null, claimable: false, done: unlocked, animate: IsOpen);
                row.SetLeadingIcon(IconFor(achievement), unlocked);
            }
        }

        private void RefreshScore()
        {
            SetText(bestText, _progress.HighScore.ToString());
            SetText(gamesText, _progress.GamesPlayed.ToString());
            SetText(mergesText, _progress.TotalMerges.ToString());
            SetText(streakText, $"{_progress.DisplayedStreak()}  (best {_progress.BestStreak})");

            var highest = HighestItem();
            SetText(highestItemText, highest != null ? highest.DisplayName : "None yet");
            if (highestItemImage != null)
            {
                highestItemImage.sprite = highest != null ? highest.Sprite : null;
                highestItemImage.enabled = highest != null && highest.Sprite != null;
            }

            if (rankText != null)
            {
                var current = _milestones?.Current;
                var next = _milestones?.Next;
                rankText.text = (current != null ? current.title : "New Player") + (next != null ? $"   •   next: {next.title} at {next.score}" : "   •   top rank");
            }

            var games = _progress.RecentGames;
            if (recentEmptyText != null)
            {
                recentEmptyText.gameObject.SetActive(games.Count == 0);
            }

            if (recentTemplate == null || recentList == null)
            {
                return;
            }

            while (_recentRows.Count < games.Count)
            {
                _recentRows.Add(Instantiate(recentTemplate, recentList));
            }

            var today = DayKey.Today(_clock);
            for (var i = 0; i < _recentRows.Count; i++)
            {
                if (i >= games.Count)
                {
                    _recentRows[i].gameObject.SetActive(false);
                    continue;
                }

                _recentRows[i].Show(i + 1, games[games.Count - 1 - i], today);
            }
        }

        private MergeItemDefinition HighestItem()
        {
            if (_content == null || string.IsNullOrEmpty(_progress.HighestItemId))
            {
                return null;
            }

            var index = _content.IndexOf(_progress.HighestItemId);
            return index >= 0 ? _content.Get(index) : null;
        }

        private Sprite IconFor(AchievementDefinition achievement)
        {
            switch (achievement.type)
            {
                case AchievementType.CompleteGames: return gamesIcon;
                case AchievementType.TotalMerges: return mergesIcon;
                case AchievementType.BestScore: return scoreIcon;
                case AchievementType.DayStreak: return streakIcon;
                case AchievementType.DiscoverItem:
                    var index = _content != null ? _content.IndexOf(achievement.itemId) : -1;
                    var sprite = index >= 0 ? _content.Get(index).Sprite : null;
                    return sprite != null ? sprite : discoverIcon;
                default: return scoreIcon;
            }
        }

        private static string RewardText(RewardDefinition reward)
        {
            if (reward.type != RewardType.PowerUpCharges || string.IsNullOrEmpty(reward.powerUpId))
            {
                return $"×{reward.amount}";
            }

            return $"{char.ToUpperInvariant(reward.powerUpId[0])}{reward.powerUpId.Substring(1)} ×{reward.amount}";
        }

        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private void Unbind()
        {
            if (_progress != null)
            {
                _progress.Changed -= Refresh;
            }

            if (_achievements != null)
            {
                _achievements.Unlocked -= OnUnlocked;
            }

            for (var i = 0; i < tabButtons.Length && i < _tabActions.Length; i++)
            {
                if (tabButtons[i] != null && _tabActions[i] != null)
                {
                    tabButtons[i].onClick.RemoveListener(_tabActions[i]);
                }
            }

            _achievements = null;
            _progress = null;
        }
    }
}
