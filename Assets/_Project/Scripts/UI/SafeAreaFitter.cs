using UnityEngine;

namespace DesiMerge.UI
{
    public static class SafeAreaMath
    {
        /// <summary>Converts a safe area in pixels to normalized anchors for a full-screen RectTransform.</summary>
        public static void ToAnchors(Rect safeArea, float screenWidth, float screenHeight, out Vector2 anchorMin, out Vector2 anchorMax)
        {
            if (screenWidth <= 0f || screenHeight <= 0f)
            {
                anchorMin = Vector2.zero;
                anchorMax = Vector2.one;
                return;
            }

            anchorMin = new Vector2(Mathf.Clamp01(safeArea.xMin / screenWidth), Mathf.Clamp01(safeArea.yMin / screenHeight));
            anchorMax = new Vector2(Mathf.Clamp01(safeArea.xMax / screenWidth), Mathf.Clamp01(safeArea.yMax / screenHeight));
        }
    }

    /// <summary>
    /// Shrinks this RectTransform (a full-screen child of the canvas) to <see cref="Screen.safeArea"/>, keeping UI
    /// clear of notches and rounded corners. Re-applies only when the canvas size changes (no per-frame polling).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        /// <summary>Test/preview hook: when set, used instead of the device safe area.</summary>
        internal static Rect? SimulatedSafeArea;

        private Rect _lastSafeArea;
        private Vector2 _lastScreen;

        private void OnEnable() => Apply(true);

        private void OnRectTransformDimensionsChange() => Apply(false);

        public void Apply(bool force)
        {
            var safeArea = SimulatedSafeArea ?? Screen.safeArea;
            var screen = new Vector2(Screen.width, Screen.height);
            if (!force && safeArea == _lastSafeArea && screen == _lastScreen)
            {
                return;
            }

            _lastSafeArea = safeArea;
            _lastScreen = screen;
            SafeAreaMath.ToAnchors(safeArea, screen.x, screen.y, out var min, out var max);
            var rect = (RectTransform)transform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
