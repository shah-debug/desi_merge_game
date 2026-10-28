using System;
using UnityEngine;

namespace DesiMerge.Cosmetics
{
    /// <summary>What a cosmetic changes. One item of each kind can be selected at a time.</summary>
    public enum CosmeticKind
    {
        ContainerTheme = 0,
        ItemSkin = 1,
    }

    public enum CosmeticUnlock
    {
        /// <summary>Owned from the start. Each kind needs one; it is the fallback selection.</summary>
        Default = 0,
        /// <summary>Owned once the achievement <c>achievementId</c> is unlocked.</summary>
        Achievement = 1,
    }

    /// <summary>
    /// Foundation only (Phase 5): ownership, selection and saving are implemented and tested; nothing in the game
    /// reads the selection yet and there is no shop screen. Real-money purchases and paid currency are out of scope.
    /// </summary>
    [Serializable]
    public sealed class CosmeticDefinition
    {
        [Tooltip("Stable save identifier. Never change or reuse it once shipped.")]
        public string id;
        public string title;
        public CosmeticKind kind;
        public CosmeticUnlock unlock;
        public string achievementId;
        public Color tint = Color.white;
    }
}
