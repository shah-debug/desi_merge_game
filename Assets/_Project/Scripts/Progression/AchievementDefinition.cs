using System;
using System.Collections.Generic;
using DesiMerge.Configuration;
using DesiMerge.Persistence;
using UnityEngine;

namespace DesiMerge.Progression
{
    public enum AchievementType
    {
        /// <summary>Complete <c>target</c> games.</summary>
        CompleteGames = 0,
        /// <summary>Perform <c>target</c> merges in total.</summary>
        TotalMerges = 1,
        /// <summary>Reach a best score of at least <c>target</c>.</summary>
        BestScore = 2,
        /// <summary>Discover the item with ID <c>itemId</c>.</summary>
        DiscoverItem = 3,
        /// <summary>Reach a daily streak of <c>target</c> days.</summary>
        DayStreak = 4,
    }

    [Serializable]
    public sealed class AchievementDefinition
    {
        [Tooltip("Stable save identifier. Never change or reuse it once shipped.")]
        public string id;
        public string title;
        public string description;
        public AchievementType type;
        public long target = 1;
        [Tooltip("Item ID for DiscoverItem.")]
        public string itemId;
        public RewardDefinition reward;
    }
}
