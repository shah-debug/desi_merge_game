using System;
using System.Collections.Generic;
using DesiMerge.Configuration;
using UnityEngine;

namespace DesiMerge.Progression
{
    public enum MissionPeriod
    {
        Daily = 0,
        Weekly = 1,
    }

    public enum MissionType
    {
        /// <summary>Finish <c>target</c> games.</summary>
        CompleteGames = 0,
        /// <summary>Merge <c>target</c> times.</summary>
        Merges = 1,
        /// <summary>Finish one game with at least <c>target</c> points.</summary>
        ScoreInOneGame = 2,
        /// <summary>Create the item <c>itemId</c> (or any higher level) by merging.</summary>
        CreateItem = 3,
        /// <summary>Use power-ups <c>target</c> times (<c>powerUpId</c> empty = any).</summary>
        UsePowerUps = 4,
        /// <summary>Finish <c>target</c> games without using a power-up in them.</summary>
        GamesWithoutPowerUps = 5,
    }

    [Serializable]
    public sealed class MissionDefinition
    {
        [Tooltip("Stable save identifier. Never change or reuse it once shipped.")]
        public string id;
        public string title;
        public string description;
        public MissionPeriod period;
        public MissionType type;
        public long target = 1;
        public string itemId;
        public string powerUpId;
        public RewardDefinition reward;
    }

    [CreateAssetMenu(fileName = "Missions", menuName = "Desi Merge/Mission Catalog")]
    public sealed class MissionCatalog : ScriptableObject
    {
        [SerializeField] private MissionDefinition[] missions = Array.Empty<MissionDefinition>();
        [SerializeField, Range(1, 6)] private int dailySlots = 3;
        [SerializeField, Range(1, 6)] private int weeklySlots = 3;

        public IReadOnlyList<MissionDefinition> All => missions;
        public int DailySlots => dailySlots;
        public int WeeklySlots => weeklySlots;

        public static bool IsValid(MissionDefinition mission, MergeProgression content)
        {
            if (mission == null || string.IsNullOrWhiteSpace(mission.id) || mission.target < 1)
            {
                return false;
            }

            if (mission.type == MissionType.CreateItem)
            {
                return !string.IsNullOrEmpty(mission.itemId) && (content == null || content.IndexOf(mission.itemId) >= 0);
            }

            return true;
        }

        internal void Set(MissionDefinition[] definitions, int daily = 3, int weekly = 3)
        {
            missions = definitions;
            dailySlots = daily;
            weeklySlots = weekly;
        }
    }
}
