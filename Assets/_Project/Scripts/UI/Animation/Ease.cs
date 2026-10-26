using UnityEngine;

namespace DesiMerge.UI.Animation
{
    /// <summary>Easing curves on t in [0, 1].</summary>
    public static class Ease
    {
        public static float OutCubic(float t)
        {
            t = Mathf.Clamp01(t) - 1f;
            return t * t * t + 1f;
        }

        public static float InCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t;
        }

        /// <summary>Overshoots slightly past 1 before settling (playful "pop").</summary>
        public static float OutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            t = Mathf.Clamp01(t) - 1f;
            return 1f + c3 * t * t * t + c1 * t * t;
        }

        /// <summary>0 → peak → 0 with a small rebound; used for scale punches.</summary>
        public static float Punch(float t)
        {
            t = Mathf.Clamp01(t);
            return Mathf.Sin(t * Mathf.PI * 2f) * (1f - t) * 0.5f + Mathf.Sin(t * Mathf.PI) * (1f - t);
        }
    }
}
