using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.UI
{
    /// <summary>One achievement line: title, description, progress text and bar, unlocked mark. Presentation only.</summary>
    public sealed class AchievementRow : MonoBehaviour
    {
        [SerializeField] private Text title;
        [SerializeField] private Text description;
        [SerializeField] private Text progress;
        [SerializeField] private Image progressFill;
        [SerializeField] private GameObject unlockedMark;
        [SerializeField] private Image background;
        [SerializeField] private Color lockedColor = new Color(0f, 0f, 0f, 0.25f);
        [SerializeField] private Color unlockedColor = new Color(1f, 0.7f, 0f, 0.35f);

        public bool ShowsUnlocked { get; private set; }
        public string ProgressText => progress != null ? progress.text : string.Empty;

        public void Show(string titleText, string descriptionText, long current, long target, bool unlocked)
        {
            ShowsUnlocked = unlocked;
            if (title != null)
            {
                title.text = titleText;
            }

            if (description != null)
            {
                description.text = descriptionText;
            }

            if (progress != null)
            {
                progress.text = unlocked ? "Unlocked" : $"{current} / {target}";
            }

            if (progressFill != null)
            {
                progressFill.fillAmount = target > 0 ? Mathf.Clamp01((float)current / target) : 0f;
            }

            if (unlockedMark != null)
            {
                unlockedMark.SetActive(unlocked);
            }

            if (background != null)
            {
                background.color = unlocked ? unlockedColor : lockedColor;
            }
        }

        internal void SetReferencesForTests(Text titleText, Text progressText, GameObject mark)
        {
            title = titleText;
            progress = progressText;
            unlockedMark = mark;
        }
    }
}
