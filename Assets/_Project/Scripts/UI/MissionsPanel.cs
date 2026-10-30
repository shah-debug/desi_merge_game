using System;
using System.Collections.Generic;
using DesiMerge.Audio;
using DesiMerge.Core;
using DesiMerge.PowerUps;
using DesiMerge.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.UI
{
    public enum MissionTab
    {
        Daily = 0,
        Weekly = 1,
        Completed = 2,
    }

    /// <summary>
    /// Daily, weekly and completed missions. The Daily tab also shows today's challenge (the existing daily challenge
    /// card). Cards are a fixed pool built with the scene; claims go through the mission service.
    /// </summary>
    public sealed class MissionsPanel : MenuPanel
    {
        [SerializeField] private Button[] tabButtons = Array.Empty<Button>();
        [SerializeField] private Image[] tabBackgrounds = Array.Empty<Image>();
        [SerializeField] private Text[] tabLabels = Array.Empty<Text>();
        [SerializeField] private Color tabSelectedColor = Color.white;
        [SerializeField] private Color tabNormalColor = Color.white;
        [SerializeField] private Color tabSelectedText = Color.white;
        [SerializeField] private Color tabNormalText = Color.white;
        [SerializeField] private DailyChallengeCard dailyChallengeCard;
        [SerializeField] private RectTransform list;
        [SerializeField] private ProgressCard[] cards = Array.Empty<ProgressCard>();
        [SerializeField] private Text emptyText;
        [SerializeField] private Text resetText;
        [SerializeField] private Text statusText;
        [SerializeField] private float listTopWithChallenge = -400f;
        [SerializeField] private float listTopWithoutChallenge = -20f;

        private readonly List<MissionState> _shown = new List<MissionState>();
        private readonly UnityEngine.Events.UnityAction[] _tabActions = new UnityEngine.Events.UnityAction[3];
        private IMissionService _missions;
        private IPowerUpService _powerUps;
        private IClock _clock;
        private RewardPresenter _presenter;

        public MissionTab Tab { get; private set; }
        public IReadOnlyList<ProgressCard> Cards => cards;
        public DailyChallengeCard DailyChallengeCard => dailyChallengeCard;
        public string EmptyText => emptyText != null && emptyText.gameObject.activeSelf ? emptyText.text : string.Empty;
        public string StatusText => statusText != null ? statusText.text : string.Empty;

        public void Bind(IMissionService missions, IDailyChallengeService daily, IPlayerProgressService progress, IPowerUpService powerUps, IClock clock,
            RewardPresenter presenter, IAudioService audio)
        {
            Unbind();
            _missions = missions ?? throw new ArgumentNullException(nameof(missions));
            _powerUps = powerUps;
            _clock = clock ?? new SystemClock();
            _presenter = presenter;
            _missions.Changed += Refresh;
            foreach (var card in cards)
            {
                if (card != null)
                {
                    card.ClaimClicked += OnClaim;
                }
            }

            for (var i = 0; i < tabButtons.Length && i < _tabActions.Length; i++)
            {
                var tab = (MissionTab)i;
                _tabActions[i] = () => SelectTab(tab);
                if (tabButtons[i] != null)
                {
                    tabButtons[i].onClick.AddListener(_tabActions[i]);
                }
            }

            if (dailyChallengeCard != null && daily != null)
            {
                dailyChallengeCard.Bind(daily, progress, powerUps);
            }

            BindAudio(audio);
            SelectTab(MissionTab.Daily);
        }

        protected override void OnDestroy()
        {
            Unbind();
            base.OnDestroy();
        }

        protected override void OnOpening()
        {
            _missions?.Refresh();
            SetStatus(string.Empty);
            SelectTab(MissionTab.Daily);
        }

        public void SelectTab(MissionTab tab)
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

            Refresh();
        }

        public void Refresh()
        {
            if (_missions == null)
            {
                return;
            }

            _shown.Clear();
            switch (Tab)
            {
                case MissionTab.Daily:
                    AddUnclaimed(_missions.Daily);
                    break;
                case MissionTab.Weekly:
                    AddUnclaimed(_missions.Weekly);
                    break;
                default:
                    AddClaimed(_missions.Daily);
                    AddClaimed(_missions.Weekly);
                    break;
            }

            var showChallenge = Tab == MissionTab.Daily && dailyChallengeCard != null;
            if (dailyChallengeCard != null)
            {
                dailyChallengeCard.gameObject.SetActive(showChallenge);
            }

            if (list != null)
            {
                list.anchoredPosition = new Vector2(list.anchoredPosition.x, showChallenge ? listTopWithChallenge : listTopWithoutChallenge);
            }

            for (var i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null)
                {
                    continue;
                }

                if (i >= _shown.Count)
                {
                    cards[i].gameObject.SetActive(false);
                    continue;
                }

                var state = _shown[i];
                var reward = state.Definition.reward;
                cards[i].Show(state.Definition.id, state.Definition.title, state.Definition.description, state.Progress, state.Target,
                    _presenter != null ? _presenter.Describe(reward) : string.Empty, _presenter != null ? _presenter.IconFor(reward) : null,
                    state.CanClaim, state.Claimed, animate: IsOpen);
            }

            if (emptyText != null)
            {
                emptyText.text = Tab == MissionTab.Completed ? "Claimed missions appear here." : "All done! New missions arrive soon.";
                emptyText.gameObject.SetActive(_shown.Count == 0);
            }

            if (resetText != null)
            {
                resetText.text = ResetLabel();
            }
        }

        private void AddUnclaimed(IReadOnlyList<MissionState> states)
        {
            foreach (var state in states)
            {
                if (!state.Claimed)
                {
                    _shown.Add(state);
                }
            }

            _shown.Sort((a, b) => b.CanClaim.CompareTo(a.CanClaim));
        }

        private void AddClaimed(IReadOnlyList<MissionState> states)
        {
            foreach (var state in states)
            {
                if (state.Claimed)
                {
                    _shown.Add(state);
                }
            }
        }

        private string ResetLabel()
        {
            var now = _clock.UtcNow;
            var nextDay = now.Date.AddDays(1);
            if (Tab == MissionTab.Weekly)
            {
                var daysToMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
                var nextWeek = now.Date.AddDays(daysToMonday == 0 ? 7 : daysToMonday);
                return "New weekly missions in " + Format(nextWeek - now);
            }

            return Tab == MissionTab.Daily ? "New daily missions in " + Format(nextDay - now) : string.Empty;
        }

        private static string Format(TimeSpan span) => span.TotalHours >= 24 ? $"{(int)span.TotalDays}d {span.Hours}h" : $"{(int)span.TotalHours}h {span.Minutes}m";

        private void OnClaim(string missionId)
        {
            MissionState state = null;
            foreach (var candidate in _shown)
            {
                if (candidate.Definition.id == missionId)
                {
                    state = candidate;
                }
            }

            if (state == null)
            {
                return;
            }

            var result = _missions.TryClaim(missionId);
            if (result == ClaimResult.Claimed)
            {
                SetStatus(string.Empty);
                _presenter?.Show("MISSION COMPLETE", state.Definition.title, new[] { state.Definition.reward });
            }
            else if (result == ClaimResult.StorageFull)
            {
                var definition = _powerUps?.Find(state.Definition.reward.powerUpId);
                SetStatus(definition != null ? $"{definition.title} storage is full. Use one in a game, then claim." : "Storage is full. Use a power-up first.");
            }
            else if (result == ClaimResult.Failed)
            {
                SetStatus("The reward couldn't be saved. Please try again.");
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.gameObject.SetActive(!string.IsNullOrEmpty(message));
            }
        }

        private void Unbind()
        {
            if (_missions != null)
            {
                _missions.Changed -= Refresh;
            }

            foreach (var card in cards)
            {
                if (card != null)
                {
                    card.ClaimClicked -= OnClaim;
                }
            }

            for (var i = 0; i < tabButtons.Length && i < _tabActions.Length; i++)
            {
                if (tabButtons[i] != null && _tabActions[i] != null)
                {
                    tabButtons[i].onClick.RemoveListener(_tabActions[i]);
                }
            }

            _missions = null;
        }
    }
}
