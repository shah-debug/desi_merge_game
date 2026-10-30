using DesiMerge.Progression;
using DesiMerge.UI.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.UI
{
    /// <summary>One day of the reward calendar: claimed, today's (available) or upcoming.</summary>
    public sealed class DailyRewardTile : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Text dayText;
        [SerializeField] private Image icon;
        [SerializeField] private Text amountText;
        [SerializeField] private GameObject claimedMark;
        [SerializeField] private GameObject todayGlow;
        [SerializeField] private ScalePunch punch;
        [SerializeField] private Color claimedColor = Color.white;
        [SerializeField] private Color availableColor = Color.white;
        [SerializeField] private Color upcomingColor = Color.white;

        public DailyRewardDayState State { get; private set; }

        public void Show(int index, Sprite iconSprite, string amount, DailyRewardDayState state)
        {
            var changed = State != state;
            State = state;
            if (dayText != null)
            {
                dayText.text = $"DAY {index + 1}";
            }

            if (icon != null)
            {
                icon.sprite = iconSprite;
                icon.color = state == DailyRewardDayState.Claimed ? new Color(1f, 1f, 1f, 0.45f) : Color.white;
            }

            if (amountText != null)
            {
                amountText.text = amount;
            }

            if (claimedMark != null)
            {
                claimedMark.SetActive(state == DailyRewardDayState.Claimed);
            }

            if (todayGlow != null)
            {
                todayGlow.SetActive(state == DailyRewardDayState.Available);
            }

            if (background != null)
            {
                background.color = state == DailyRewardDayState.Claimed ? claimedColor : state == DailyRewardDayState.Available ? availableColor : upcomingColor;
            }

            if (changed && punch != null && isActiveAndEnabled)
            {
                punch.Play();
            }
        }
    }
}
