using System;
using UnityEngine;

namespace DesiMerge.UI
{
    /// <summary>
    /// Keeps layouts designed for 1080 x 1920 readable on shorter portrait screens (e.g. 3:4 tablets): scales the
    /// targets down uniformly when this rect is smaller than the reference, never up. Re-applies only when this
    /// rect's size changes (screen or safe-area change), so there is no per-frame work.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class ResponsiveScaler : MonoBehaviour
    {
        [SerializeField] private Vector2 referenceSize = new Vector2(1080f, 1920f);
        [SerializeField] private RectTransform[] targets = Array.Empty<RectTransform>();

        public float CurrentScale { get; private set; } = 1f;

        public static float ScaleFor(Vector2 available, Vector2 reference)
        {
            if (available.x <= 0f || available.y <= 0f || reference.x <= 0f || reference.y <= 0f)
            {
                return 1f;
            }

            return Mathf.Min(1f, available.x / reference.x, available.y / reference.y);
        }

        private void OnEnable() => Apply();

        private void OnRectTransformDimensionsChange() => Apply();

        public void Apply()
        {
            CurrentScale = ScaleFor(((RectTransform)transform).rect.size, referenceSize);
            foreach (var target in targets)
            {
                if (target != null)
                {
                    target.localScale = new Vector3(CurrentScale, CurrentScale, 1f);
                }
            }
        }
    }
}
