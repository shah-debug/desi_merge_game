using System;
using UnityEngine;

namespace DesiMerge.PowerUps
{
    /// <summary>The effect a power-up has. Undo is intentionally absent (deferred; see Documentation/PhaseStatus.md).</summary>
    public enum PowerUpKind
    {
        /// <summary>Removes exactly one item the player taps.</summary>
        Hammer = 0,
        /// <summary>Re-stacks every item into tidy, non-overlapping rows inside the container.</summary>
        Shuffle = 1,
    }

    [Serializable]
    public sealed class PowerUpDefinition
    {
        [Tooltip("Stable save identifier. Never change or reuse it once shipped.")]
        public string id;
        public string title;
        public PowerUpKind kind;

        [Tooltip("Free uses at the start of every game (new game or restart). Not saved; a revive keeps what is left.")]
        [Min(0)] public int freeUsesPerGame = 1;

        [Tooltip("Most saved bonus charges (earned from rewards) the player can hold. Further grants are refused.")]
        [Min(0)] public int maxSavedCharges = 9;
    }
}
