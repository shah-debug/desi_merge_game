using System;
using UnityEngine;

namespace DesiMerge.Progression
{
    public enum DailyChallengeType
    {
        /// <summary>Perform <c>target</c> merges during the day (across games, including abandoned ones).</summary>
        MergesInDay = 0,
        /// <summary>Finish a single game with a score of at least <c>target</c>.</summary>
        ScoreInOneGame = 1,
        /// <summary>Create the item <c>itemId</c> (or any higher level) by merging.</summary>
        CreateItem = 2,
        /// <summary>Complete <c>target</c> games during the day.</summary>
        CompleteGames = 3,
    }

    [Serializable]
    public sealed class DailyChallengeDefinition
    {
        [Tooltip("Stable save identifier. Never change or reuse it once shipped.")]
        public string id;
        public string title;
        public string description;
        public DailyChallengeType type;
        public long target = 1;
        [Tooltip("Item ID for CreateItem.")]
        public string itemId;
        [Tooltip("Optional. None = completion only (no economy is defined yet).")]
        public RewardDefinition reward;
    }
}
