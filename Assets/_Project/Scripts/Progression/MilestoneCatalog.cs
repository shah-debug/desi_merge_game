using System;
using System.Collections.Generic;
using UnityEngine;

namespace DesiMerge.Progression
{
    /// <summary>A player rank reached through the best score.</summary>
    [Serializable]
    public sealed class MilestoneDefinition
    {
        [Tooltip("Stable save identifier. Never change or reuse it once shipped.")]
        public string id;
        public string title;
        public string description;
        public long score;
        public RewardDefinition reward;
    }

    [CreateAssetMenu(fileName = "Milestones", menuName = "Desi Merge/Score Milestones")]
    public sealed class MilestoneCatalog : ScriptableObject
    {
        [SerializeField] private MilestoneDefinition[] milestones = Array.Empty<MilestoneDefinition>();

        public IReadOnlyList<MilestoneDefinition> All => milestones;

        public static bool IsValid(MilestoneDefinition milestone) =>
            milestone != null && !string.IsNullOrWhiteSpace(milestone.id) && !string.IsNullOrWhiteSpace(milestone.title) && milestone.score >= 0;

        internal void Set(MilestoneDefinition[] definitions) => milestones = definitions;
    }
}
