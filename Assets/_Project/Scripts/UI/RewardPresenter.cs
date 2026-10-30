using System;
using System.Collections.Generic;
using DesiMerge.Audio;
using DesiMerge.PowerUps;
using DesiMerge.Progression;
using DesiMerge.UI.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.UI
{
    /// <summary>
    /// The one reward popup used by every screen: shows what was just added to the player's inventory (the reward is
    /// already applied by the service before this is shown), plays the claim sound and waits for Collect.
    /// Several rewards in a row are queued, so only one popup is ever open.
    /// </summary>
    public sealed class RewardPresenter : MonoBehaviour
    {
        private const int MaxQueued = 8;

        [SerializeField] private PanelTransition transition;
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private GameObject[] rewardSlots = Array.Empty<GameObject>();
        [SerializeField] private Image[] rewardIcons = Array.Empty<Image>();
        [SerializeField] private Text[] rewardLabels = Array.Empty<Text>();
        [SerializeField] private RectTransform rays;
        [SerializeField] private ScalePunch iconPunch;
        [SerializeField] private Button collectButton;
        [SerializeField] private Sprite hammerIcon;
        [SerializeField] private Sprite shuffleIcon;
        [SerializeField] private Sprite giftIcon;
        [SerializeField] private float raysDegreesPerSecond = 24f;

        private readonly Queue<(string Title, string Subtitle, RewardDefinition[] Rewards)> _queue = new Queue<(string, string, RewardDefinition[])>();
        private IAudioService _audio;
        private IPowerUpService _powerUps;

        public bool IsShowing => transition != null ? transition.IsVisible : gameObject.activeSelf;
        public string Title => titleText != null ? titleText.text : string.Empty;
        public int QueuedCount => _queue.Count;
        internal Button CollectButton => collectButton;

        private void Awake()
        {
            if (collectButton != null)
            {
                collectButton.onClick.AddListener(Collect);
            }

            if (transition != null)
            {
                transition.SetVisibleImmediate(false);
            }

            enabled = false;
        }

        private void OnDestroy()
        {
            if (collectButton != null)
            {
                collectButton.onClick.RemoveListener(Collect);
            }
        }

        public void Bind(IAudioService audio, IPowerUpService powerUps)
        {
            _audio = audio;
            _powerUps = powerUps;
            foreach (var feedback in GetComponentsInChildren<ButtonFeedback>(true))
            {
                feedback.Bind(audio);
            }
        }

        public Sprite IconFor(RewardDefinition reward)
        {
            if (reward.type == RewardType.PowerUpCharges)
            {
                var kind = _powerUps?.Find(reward.powerUpId)?.kind;
                if (kind == PowerUpKind.Hammer || reward.powerUpId == "hammer")
                {
                    return hammerIcon;
                }

                if (kind == PowerUpKind.Shuffle || reward.powerUpId == "shuffle")
                {
                    return shuffleIcon;
                }
            }

            return giftIcon;
        }

        public string Describe(RewardDefinition reward)
        {
            switch (reward.type)
            {
                case RewardType.PowerUpCharges:
                    var title = _powerUps?.Find(reward.powerUpId)?.title ?? reward.powerUpId;
                    return $"{title} ×{reward.amount}";
                case RewardType.Coins:
                    return $"{reward.amount} coins";
                default:
                    return string.Empty;
            }
        }

        public string Describe(IReadOnlyList<RewardDefinition> rewards)
        {
            var parts = new List<string>();
            foreach (var reward in rewards)
            {
                if (reward.IsSomething)
                {
                    parts.Add(Describe(reward));
                }
            }

            return string.Join(" + ", parts);
        }

        public void Show(string title, string subtitle, IReadOnlyList<RewardDefinition> rewards)
        {
            if (_queue.Count >= MaxQueued)
            {
                return;
            }

            var copy = new RewardDefinition[rewards?.Count ?? 0];
            for (var i = 0; i < copy.Length; i++)
            {
                copy[i] = rewards[i];
            }

            _queue.Enqueue((title, subtitle, copy));
            if (!IsShowing)
            {
                ShowNext();
            }
        }

        public void Collect()
        {
            if (_queue.Count > 0)
            {
                ShowNext();
                return;
            }

            enabled = false;
            if (transition != null)
            {
                transition.Hide();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void ShowNext()
        {
            var next = _queue.Dequeue();
            if (titleText != null)
            {
                titleText.text = next.Title;
            }

            if (subtitleText != null)
            {
                subtitleText.text = next.Subtitle ?? string.Empty;
            }

            for (var i = 0; i < rewardIcons.Length; i++)
            {
                var has = i < next.Rewards.Length && next.Rewards[i].IsSomething;
                if (i < rewardSlots.Length && rewardSlots[i] != null)
                {
                    rewardSlots[i].SetActive(has);
                }

                if (rewardIcons[i] != null)
                {
                    rewardIcons[i].sprite = has ? IconFor(next.Rewards[i]) : null;
                }

                if (i < rewardLabels.Length && rewardLabels[i] != null)
                {
                    rewardLabels[i].text = has ? Describe(next.Rewards[i]) : string.Empty;
                }
            }

            if (transition != null)
            {
                transition.Show();
            }
            else
            {
                gameObject.SetActive(true);
            }

            if (iconPunch != null)
            {
                iconPunch.Play();
            }

            _audio?.PlaySfx(SfxCue.RewardClaim);
            enabled = rays != null;
        }

        private void Update()
        {
            rays.Rotate(0f, 0f, -raysDegreesPerSecond * Time.unscaledDeltaTime);
        }
    }
}
