using DesiMerge.PowerUps;
using DesiMerge.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.UI
{
    /// <summary>
    /// Today's challenge on the main menu: objective, progress, status and day streak. Refreshes from the
    /// service's Changed event (no polling). The Claim button only appears for challenges that actually have a reward.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DailyChallengeCard : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text progressText;
        [SerializeField] private Image progressFill;
        [SerializeField] private Text statusText;
        [SerializeField] private Text streakText;
        [SerializeField] private GameObject completedMark;
        [SerializeField] private Button claimButton;

        private IDailyChallengeService _daily;
        private IPlayerProgressService _progress;
        private IPowerUpService _powerUps;
        private bool _claimFailed;

        public string StatusText => statusText != null ? statusText.text : string.Empty;
        public string ProgressText => progressText != null ? progressText.text : string.Empty;
        public bool ClaimVisible => claimButton != null && claimButton.gameObject.activeSelf;

        /// <param name="powerUps">Names power-up rewards ("+1 Hammer"). Optional.</param>
        public void Bind(IDailyChallengeService daily, IPlayerProgressService progress, IPowerUpService powerUps = null)
        {
            Unbind();
            _daily = daily;
            _progress = progress;
            _powerUps = powerUps;
            _daily.Changed += Refresh;
            if (claimButton != null)
            {
                claimButton.onClick.AddListener(OnClaimClicked);
            }

            Refresh();
        }

        private void OnDestroy() => Unbind();

        public void Refresh()
        {
            if (_daily == null)
            {
                return;
            }

            var challenge = _daily.Current;
            if (challenge == null)
            {
                SetText(titleText, "No challenge today");
                SetText(descriptionText, string.Empty);
                SetText(progressText, string.Empty);
                SetText(statusText, string.Empty);
                SetFill(0f);
                SetActive(completedMark, false);
                SetActive(claimButton != null ? claimButton.gameObject : null, false);
            }
            else
            {
                SetText(titleText, challenge.title);
                SetText(descriptionText, challenge.description);
                SetText(progressText, $"{_daily.Progress} / {_daily.Target}");
                SetFill(_daily.Target > 0 ? (float)_daily.Progress / _daily.Target : 0f);
                SetText(statusText, StatusFor(challenge));
                SetActive(completedMark, _daily.IsCompleted);
                SetActive(claimButton != null ? claimButton.gameObject : null, _daily.CanClaim);
            }

            if (_progress != null)
            {
                var streak = _progress.DisplayedStreak();
                SetText(streakText, streak > 0 ? $"Day streak: {streak}" : "Finish a game to start a streak");
            }
        }

        private string StatusFor(DailyChallengeDefinition challenge)
        {
            var reward = DescribeReward(challenge.reward, _powerUps);
            if (_daily.IsClaimed)
            {
                return "Reward claimed";
            }

            if (_daily.CanClaim)
            {
                return _claimFailed ? "Can't claim yet: use some first" : $"Claim {reward}!";
            }

            if (_daily.IsCompleted)
            {
                return "Completed!";
            }

            return string.IsNullOrEmpty(reward) ? "In progress" : $"Reward: {reward}";
        }

        /// <summary>"+1 Hammer", "+5 coins", or empty for no reward.</summary>
        internal static string DescribeReward(RewardDefinition reward, IPowerUpService powerUps)
        {
            if (!reward.IsSomething)
            {
                return string.Empty;
            }

            if (reward.type == RewardType.PowerUpCharges)
            {
                var definition = powerUps != null ? powerUps.Find(reward.powerUpId) : null;
                return $"+{reward.amount} {(definition != null ? definition.title : reward.powerUpId)}";
            }

            return $"+{reward.amount} coins";
        }

        private void OnClaimClicked()
        {
            if (_daily == null)
            {
                return;
            }

            // Refused claims (e.g. saved charges at their maximum) change nothing and can be retried later.
            _claimFailed = !_daily.TryClaim();
            Refresh();
        }

        private void Unbind()
        {
            if (_daily != null)
            {
                _daily.Changed -= Refresh;
                _daily = null;
            }

            if (claimButton != null)
            {
                claimButton.onClick.RemoveListener(OnClaimClicked);
            }
        }

        private void SetFill(float amount)
        {
            if (progressFill != null)
            {
                progressFill.fillAmount = Mathf.Clamp01(amount);
            }
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        internal void SetReferencesForTests(Text title, Text progress, Text status, Button claim)
        {
            titleText = title;
            progressText = progress;
            statusText = status;
            claimButton = claim;
        }
    }
}
