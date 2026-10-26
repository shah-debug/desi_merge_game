using UnityEngine;

namespace DesiMerge.UI
{
    /// <summary>
    /// Merge sparkles from a single ParticleSystem using Emit (no per-merge GameObjects or allocations).
    /// If no particle system is assigned, or effects are turned off, merges simply play without sparkles.
    /// </summary>
    public sealed class MergeEffects : MonoBehaviour
    {
        [SerializeField] private ParticleSystem sparkles;
        [SerializeField, Range(0, 64)] private int particlesPerMerge = 14;
        [SerializeField] private bool effectsEnabled = true;

        public bool EffectsEnabled
        {
            get => effectsEnabled && sparkles != null;
            set => effectsEnabled = value;
        }

        public void PlayMerge(Vector3 worldPosition, Color color, float radius)
        {
            if (!EffectsEnabled)
            {
                return;
            }

            var emit = new ParticleSystem.EmitParams
            {
                position = worldPosition,
                applyShapeToPosition = true,
                startColor = color,
                startSize = Mathf.Lerp(0.15f, 0.35f, Mathf.InverseLerp(0.3f, 1.5f, radius)),
            };
            sparkles.Emit(emit, particlesPerMerge);

            // A few white glints on top of the tinted burst.
            emit.startColor = Color.white;
            emit.startSize *= 0.6f;
            sparkles.Emit(emit, particlesPerMerge / 3);
        }
    }
}
