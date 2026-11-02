using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.UI
{
    /// <summary>
    /// Keeps a button's icon + label centred as one group, also when the label changes at runtime (e.g. NEXT →
    /// LET'S PLAY). Labels that are too long shrink to fit instead of running off-centre. Runs only when the text or
    /// the button size changes.
    /// </summary>
    [ExecuteAlways]
    public sealed class IconLabelLayout : MonoBehaviour
    {
        [SerializeField] private RectTransform icon;
        [SerializeField] private Text label;
        [SerializeField] private float iconSize = 64f;
        [SerializeField] private float gap = 18f;
        [SerializeField] private float padding = 24f;
        [SerializeField] private float lift;
        [SerializeField] private int maxFontSize = 48;

        private bool _applying;

        public float LabelCentreOffset { get; private set; }

        private void OnEnable()
        {
            if (label != null)
            {
                label.RegisterDirtyVerticesCallback(Apply);
            }

            Apply();
        }

        private void OnDisable()
        {
            if (label != null)
            {
                label.UnregisterDirtyVerticesCallback(Apply);
            }
        }

        private void OnRectTransformDimensionsChange() => Apply();

        public void Apply()
        {
            if (_applying || icon == null || label == null)
            {
                return;
            }

            _applying = true;
            var width = ((RectTransform)transform).rect.width;
            var available = Mathf.Max(10f, width - padding * 2f - iconSize - gap);
            label.fontSize = maxFontSize;
            var textWidth = label.preferredWidth;
            if (textWidth > available)
            {
                label.fontSize = Mathf.Max(12, Mathf.FloorToInt(maxFontSize * available / textWidth));
                textWidth = Mathf.Min(label.preferredWidth, available);
            }

            var total = iconSize + gap + textWidth;
            var left = -total * 0.5f;
            Set(icon, new Vector2(left + iconSize * 0.5f, lift), new Vector2(iconSize, iconSize));
            Set(label.rectTransform, new Vector2(left + iconSize + gap + textWidth * 0.5f, lift - 2f), new Vector2(textWidth + 4f, ((RectTransform)transform).rect.height));
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            LabelCentreOffset = left + total * 0.5f;
            _applying = false;
        }

        private static void Set(RectTransform rect, Vector2 position, Vector2 size)
        {
            var centre = new Vector2(0.5f, 0.5f);
            if (rect.anchorMin != centre || rect.anchorMax != centre)
            {
                rect.anchorMin = centre;
                rect.anchorMax = centre;
            }

            rect.pivot = centre;
            if (rect.anchoredPosition != position)
            {
                rect.anchoredPosition = position;
            }

            if (rect.sizeDelta != size)
            {
                rect.sizeDelta = size;
            }
        }

        internal void Configure(RectTransform iconRect, Text labelText, float size, float spacing, float sidePadding, float verticalLift)
        {
            icon = iconRect;
            label = labelText;
            iconSize = size;
            gap = spacing;
            padding = sidePadding;
            lift = verticalLift;
            maxFontSize = labelText.fontSize;
        }
    }
}
