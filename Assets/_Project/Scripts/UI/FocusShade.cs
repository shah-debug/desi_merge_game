using DesiMerge.Gameplay;
using UnityEngine;

namespace DesiMerge.UI
{
    /// <summary>
    /// Hammer focus: two dark bands slide in from the top and bottom of the screen and stop at the jar's edges, so
    /// only the jar stays bright while the player picks an item. Follows the camera (it zooms while selecting).
    /// Visual only; input is handled by the drop controller and the cancel button.
    /// </summary>
    public sealed class FocusShade : MonoBehaviour
    {
        [SerializeField] private RectTransform area;
        [SerializeField] private RectTransform topBand;
        [SerializeField] private RectTransform bottomBand;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private MergeBoard board;
        [Tooltip("Jar top / bottom in board-local units (the rim above the container and the floor below it).")]
        [SerializeField] private float jarTop = 9.6f;
        [SerializeField] private float jarBottom = -0.7f;
        [SerializeField] private float slideSeconds = 0.28f;

        private float _shown;
        private bool _visible;

        /// <summary>0 = hidden, 1 = fully in place.</summary>
        public float Amount => _shown;
        public bool Visible => _visible;
        public float TopBandHeight => topBand != null ? topBand.sizeDelta.y : 0f;
        public float BottomBandHeight => bottomBand != null ? bottomBand.sizeDelta.y : 0f;

        public void SetVisible(bool visible)
        {
            _visible = visible;
            if (visible)
            {
                gameObject.SetActive(true);
                Layout();
            }
        }

        private void LateUpdate()
        {
            var target = _visible ? 1f : 0f;
            _shown = Mathf.MoveTowards(_shown, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, slideSeconds));
            Layout();
            if (!_visible && _shown <= 0f)
            {
                gameObject.SetActive(false);
            }
        }

        private void Layout()
        {
            if (area == null || topBand == null || bottomBand == null)
            {
                return;
            }

            var height = area.rect.height;
            var top = height * 0.3f;
            var bottom = height * 0.12f;
            if (worldCamera != null && board != null)
            {
                top = Mathf.Clamp(height * 0.5f - LocalY(board.LocalToWorld(new Vector2(0f, jarTop))), 0f, height);
                bottom = Mathf.Clamp(height * 0.5f + LocalY(board.LocalToWorld(new Vector2(0f, jarBottom))), 0f, height);
            }

            var eased = 1f - (1f - _shown) * (1f - _shown);
            topBand.sizeDelta = new Vector2(topBand.sizeDelta.x, top * eased);
            bottomBand.sizeDelta = new Vector2(bottomBand.sizeDelta.x, bottom * eased);
            if (group != null)
            {
                group.alpha = Mathf.Clamp01(_shown * 1.6f);
            }
        }

        /// <summary>Vertical position of a world point inside <see cref="area"/> (centre = 0).</summary>
        private float LocalY(Vector2 world)
        {
            var screen = worldCamera.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, null, out var local);
            return local.y + (area.pivot.y - 0.5f) * area.rect.height;
        }
    }
}
