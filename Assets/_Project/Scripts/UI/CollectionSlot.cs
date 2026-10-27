using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.UI
{
    /// <summary>One cell of the collection grid. Presentation only.</summary>
    public sealed class CollectionSlot : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Text label;
        [SerializeField] private GameObject lockedMark;

        public bool ShowsDiscovered { get; private set; }
        public string LabelText => label != null ? label.text : string.Empty;

        public void Show(Sprite sprite, Color tint, string text, bool discovered)
        {
            ShowsDiscovered = discovered;
            if (icon != null)
            {
                icon.sprite = sprite;
                icon.color = tint;
                icon.enabled = sprite != null;
            }

            if (label != null)
            {
                label.text = text;
            }

            if (lockedMark != null)
            {
                lockedMark.SetActive(!discovered);
            }
        }

        internal void SetReferencesForTests(Image iconImage, Text labelText, GameObject mark)
        {
            icon = iconImage;
            label = labelText;
            lockedMark = mark;
        }
    }
}
