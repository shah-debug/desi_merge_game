using System;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.UI
{
    /// <summary>
    /// One mission or rank row: title, description, animated progress bar, reward preview, and a Claim button that
    /// only appears when the reward can be claimed. A completed and claimed row shows a check mark instead.
    /// </summary>
    public sealed class ProgressCard : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Image progressFill;
        [SerializeField] private Text progressText;
        [SerializeField] private Image rewardIcon;
        [SerializeField] private Text rewardText;
        [SerializeField] private Button claimButton;
        [SerializeField] private GameObject doneMark;
        [SerializeField] private Image background;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color claimableColor = Color.white;
        [SerializeField] private Color doneColor = Color.white;
        [SerializeField] private float fillSpeed = 2.5f;

        private float _targetFill;

        public event Action<string> ClaimClicked;

        public string Id { get; private set; }
        public bool ClaimVisible => claimButton != null && claimButton.gameObject.activeSelf;
        public bool ShowsDone => doneMark != null && doneMark.activeSelf;
        public string ProgressLabel => progressText != null ? progressText.text : string.Empty;
        public string Title => titleText != null ? titleText.text : string.Empty;
        public string RewardLabel => rewardText != null ? rewardText.text : string.Empty;
        internal Button ClaimButton => claimButton;

        private void Awake()
        {
            if (claimButton != null)
            {
                claimButton.onClick.AddListener(OnClaim);
            }

            enabled = false;
        }

        private void OnDestroy()
        {
            if (claimButton != null)
            {
                claimButton.onClick.RemoveListener(OnClaim);
            }
        }

        public void Show(string id, string title, string description, long current, long target, string rewardLabel, Sprite icon, bool claimable, bool done, bool animate)
        {
            Id = id;
            SetText(titleText, title);
            SetText(descriptionText, description);
            SetText(progressText, done ? "Done" : $"{Math.Min(current, target)} / {target}");
            SetText(rewardText, rewardLabel);
            if (rewardIcon != null)
            {
                rewardIcon.sprite = icon;
                rewardIcon.enabled = icon != null;
            }

            if (claimButton != null)
            {
                claimButton.gameObject.SetActive(claimable);
            }

            if (doneMark != null)
            {
                doneMark.SetActive(done);
            }

            if (background != null)
            {
                background.color = done ? doneColor : claimable ? claimableColor : normalColor;
            }

            _targetFill = target <= 0 ? 1f : Mathf.Clamp01((float)current / target);
            if (progressFill != null)
            {
                if (animate && isActiveAndEnabled)
                {
                    enabled = true;
                }
                else
                {
                    progressFill.fillAmount = _targetFill;
                }
            }

            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (progressFill == null)
            {
                enabled = false;
                return;
            }

            progressFill.fillAmount = Mathf.MoveTowards(progressFill.fillAmount, _targetFill, fillSpeed * Time.unscaledDeltaTime);
            if (Mathf.Approximately(progressFill.fillAmount, _targetFill))
            {
                enabled = false;
            }
        }

        private void OnClaim() => ClaimClicked?.Invoke(Id);

        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }
    }
}
