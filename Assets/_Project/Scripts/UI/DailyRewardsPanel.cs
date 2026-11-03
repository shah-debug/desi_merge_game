using System;
using System.Collections.Generic;
using DesiMerge.Audio;
using DesiMerge.PowerUps;
using DesiMerge.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.UI
{
    /// <summary>
    /// The 7-day reward calendar. The service decides what can be claimed and applies the reward; this panel only
    /// shows the state and forwards Claim. Claim is disabled while a claim is being processed, so rapid taps
    /// can't claim twice (the service would refuse anyway).
    /// </summary>
    public sealed class DailyRewardsPanel : MenuPanel
    {
        [SerializeField] private DailyRewardTile[] tiles = Array.Empty<DailyRewardTile>();
        [SerializeField] private Button claimButton;
        [SerializeField] private Text claimLabel;
        [SerializeField] private Text streakText;
        [SerializeField] private Text statusText;

        private IDailyRewardService _rewards;
        private IPowerUpService _powerUps;
        private RewardPresenter _presenter;
        private DesiMerge.Core.IClock _clock;
        private float _tick;

        /// <summary>Text on the button while today's reward is claimed (e.g. "NEXT IN 13:42:05").</summary>
        public string ClaimLabel => claimLabel != null ? claimLabel.text : string.Empty;

        /// <summary>Clock for the countdown (rewards change at 00:00 UTC, see DayKey).</summary>
        public void SetClock(DesiMerge.Core.IClock clock) => _clock = clock;

        /// <summary>Time left until the next day's reward can be claimed.</summary>
        public System.TimeSpan TimeToNextReward()
        {
            var now = (_clock ?? new DesiMerge.Core.SystemClock()).UtcNow;
            return now.Date.AddDays(1) - now;
        }

        private void Update()
        {
            if (_rewards == null || _rewards.CanClaim || !IsOpen)
            {
                enabled = false;
                return;
            }

            _tick -= Time.unscaledDeltaTime;
            if (_tick > 0f)
            {
                return;
            }

            _tick = 1f;
            var left = TimeToNextReward();
            if (left.TotalSeconds <= 1)
            {
                _rewards.Refresh();
                Refresh();
                return;
            }

            if (claimLabel != null)
            {
                claimLabel.text = DesiMerge.Localization.Loc.F("NEXT IN {0}", $"{(int)left.TotalHours:00}:{left.Minutes:00}:{left.Seconds:00}");
            }
        }

        public IReadOnlyList<DailyRewardTile> Tiles => tiles;
        public bool ClaimInteractable => claimButton != null && claimButton.interactable;
        public string StatusText => statusText != null ? statusText.text : string.Empty;
        internal Button ClaimButton => claimButton;

        public void Bind(IDailyRewardService rewards, IPowerUpService powerUps, RewardPresenter presenter, IAudioService audio)
        {
            Unbind();
            _rewards = rewards ?? throw new ArgumentNullException(nameof(rewards));
            _powerUps = powerUps;
            _presenter = presenter;
            _rewards.Changed += Refresh;
            if (claimButton != null)
            {
                claimButton.onClick.AddListener(OnClaim);
            }

            BindAudio(audio);
            Refresh();
        }

        protected override void OnDestroy()
        {
            Unbind();
            base.OnDestroy();
        }

        protected override void OnOpening()
        {
            _rewards?.Refresh();
            Refresh();
        }

        public void Refresh()
        {
            if (_rewards == null)
            {
                return;
            }

            var days = _rewards.Days;
            for (var i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] == null)
                {
                    continue;
                }

                tiles[i].gameObject.SetActive(i < days.Count);
                if (i < days.Count)
                {
                    var rewards = days[i].rewards;
                    var main = Main(rewards);
                    tiles[i].Show(i, rewards.Length > 0 && _presenter != null ? _presenter.IconFor(main) : null, Amount(rewards), _rewards.StateOf(i));
                }
            }

            if (claimButton != null)
            {
                claimButton.interactable = _rewards.CanClaim;
            }

            if (claimLabel != null)
            {
                var left = TimeToNextReward();
                claimLabel.text = _rewards.CanClaim ? DesiMerge.Localization.Loc.T("CLAIM") : DesiMerge.Localization.Loc.F("NEXT IN {0}", $"{(int)left.TotalHours:00}:{left.Minutes:00}:{left.Seconds:00}");
            }

            _tick = 1f;
            enabled = !_rewards.CanClaim;

            if (streakText != null)
            {
                streakText.text = _rewards.Streak > 0 ? DesiMerge.Localization.Loc.F(_rewards.Streak == 1 ? "Streak: {0} day" : "Streak: {0} days", _rewards.Streak) : DesiMerge.Localization.Loc.T("Claim every day to build a streak");
            }

            SetStatus(_rewards.ClockChanged ? DesiMerge.Localization.Loc.T("The phone clock was changed. Daily rewards continue once a real day has passed.")
                : _rewards.ClockMovedBack ? DesiMerge.Localization.Loc.T("Your device date looks earlier than before. Rewards continue when that day comes again.") : string.Empty);
        }

        private void OnClaim()
        {
            if (_rewards == null || !_rewards.CanClaim)
            {
                return;
            }

            claimButton.interactable = false;
            var dayIndex = _rewards.TodayIndex;
            var day = _rewards.Days[dayIndex];
            var result = _rewards.TryClaim();
            Refresh();
            switch (result)
            {
                case ClaimResult.Claimed:
                    _presenter?.Show(DesiMerge.Localization.Loc.F("DAY {0} REWARD", dayIndex + 1), DesiMerge.Localization.Loc.T(day.title), day.rewards);
                    break;
                case ClaimResult.StorageFull:
                    SetStatus(StorageFullMessage(day.rewards));
                    break;
                case ClaimResult.Failed:
                    SetStatus(DesiMerge.Localization.Loc.T("The reward couldn't be saved. Please try again."));
                    break;
            }

        }

        private string StorageFullMessage(IReadOnlyList<RewardDefinition> rewards)
        {
            foreach (var reward in rewards)
            {
                var definition = _powerUps?.Find(reward.powerUpId);
                if (definition != null && !_powerUps.CanGrant(reward.powerUpId, reward.amount))
                {
                    return DesiMerge.Localization.Loc.F("{0} storage is full ({1}/{2}). Use one in a game, then claim.", DesiMerge.Localization.Loc.T(definition.title), _powerUps.SavedCharges(reward.powerUpId), definition.maxSavedCharges);
                }
            }

            return DesiMerge.Localization.Loc.T("Storage is full. Use a power-up in a game, then claim.");
        }

        /// <summary>The tile shows the diamonds first when a day has them.</summary>
        private static RewardDefinition Main(IReadOnlyList<RewardDefinition> rewards)
        {
            foreach (var reward in rewards)
            {
                if (reward.type == RewardType.Coins)
                {
                    return reward;
                }
            }

            return rewards.Count > 0 ? rewards[0] : default;
        }

        private string Amount(IReadOnlyList<RewardDefinition> rewards)
        {
            var main = Main(rewards);
            if (main.type == RewardType.Coins)
            {
                var extras = 0;
                foreach (var reward in rewards)
                {
                    extras += reward.type == RewardType.Coins ? 0 : reward.amount;
                }

                return extras > 0 ? $"{main.amount} +{extras}" : main.amount.ToString();
            }

            if (rewards.Count == 1)
            {
                return "×" + rewards[0].amount;
            }

            var parts = new List<string>();
            foreach (var reward in rewards)
            {
                parts.Add("×" + reward.amount);
            }

            return string.Join(" + ", parts);
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
            if (_rewards != null)
            {
                _rewards.Changed -= Refresh;
            }

            if (claimButton != null)
            {
                claimButton.onClick.RemoveListener(OnClaim);
            }

            _rewards = null;
        }
    }
}
