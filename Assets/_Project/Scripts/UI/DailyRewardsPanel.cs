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
                    tiles[i].Show(i, rewards.Length > 0 && _presenter != null ? _presenter.IconFor(rewards[0]) : null, Amount(rewards), _rewards.StateOf(i));
                }
            }

            if (claimButton != null)
            {
                claimButton.interactable = _rewards.CanClaim;
            }

            if (claimLabel != null)
            {
                claimLabel.text = _rewards.CanClaim ? "CLAIM" : "SEE YOU TOMORROW";
            }

            if (streakText != null)
            {
                streakText.text = _rewards.Streak > 0 ? $"Streak: {_rewards.Streak} day{(_rewards.Streak == 1 ? string.Empty : "s")}" : "Claim every day to build a streak";
            }

            SetStatus(_rewards.ClockMovedBack ? "Your device date looks earlier than before. Rewards continue when that day comes again." : string.Empty);
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
                    _presenter?.Show($"DAY {dayIndex + 1} REWARD", day.title, day.rewards);
                    break;
                case ClaimResult.StorageFull:
                    SetStatus(StorageFullMessage(day.rewards));
                    break;
                case ClaimResult.Failed:
                    SetStatus("The reward couldn't be saved. Please try again.");
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
                    return $"{definition.title} storage is full ({_powerUps.SavedCharges(reward.powerUpId)}/{definition.maxSavedCharges}). Use one in a game, then claim.";
                }
            }

            return "Storage is full. Use a power-up in a game, then claim.";
        }

        private string Amount(IReadOnlyList<RewardDefinition> rewards)
        {
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
