using DesiMerge.Core;
using UnityEngine;

namespace DesiMerge.Gameplay
{
    public static class DropRules
    {
        /// <summary>Clamps a board-local x so an item of <paramref name="radius"/> stays inside the walls.</summary>
        public static float ClampX(float x, float halfWidth, float radius)
        {
            var limit = Mathf.Max(0f, halfWidth - radius);
            return Mathf.Clamp(x, -limit, limit);
        }

        public static bool CanDrop(bool sessionOver, GameState state, float now, float nextDropAllowedAt)
        {
            return !sessionOver && state == GameState.Playing && now >= nextDropAllowedAt;
        }
    }
}
