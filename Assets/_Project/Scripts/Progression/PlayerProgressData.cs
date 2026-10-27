using System;
using System.Collections.Generic;

namespace DesiMerge.Progression
{
    /// <summary>
    /// Long-term player progress (file "player_progress.json"). Field names are the save format: never rename them.
    /// Content is referenced only by stable item IDs (MergeItemDefinition.Id), never by display name or level index.
    ///
    /// Schema history (see PlayerProgressService.Migrate):
    ///  1 — highScore, coins, gamesPlayed.
    ///  2 — adds totalMerges, discoveredItemIds, highestItemId, streak fields. gamesPlayed now means completed games
    ///      (it always did: it was only incremented on game over).
    /// To change the shape: bump <see cref="CurrentSchemaVersion"/>, add fields with safe defaults (JsonUtility keeps
    /// initializer values for fields missing from older files), and add a step to Migrate for anything non-trivial.
    /// </summary>
    [Serializable]
    public sealed class PlayerProgressData
    {
        public const int CurrentSchemaVersion = 2;

        public int schemaVersion = CurrentSchemaVersion;
        public long highScore;
        public long coins;
        public int gamesPlayed;

        public long totalMerges;
        public List<string> discoveredItemIds = new List<string>();
        public string highestItemId = string.Empty;

        public int currentStreak;
        public int bestStreak;
        /// <summary>UTC day number (see DayKey) of the last completed game; -1 = never.</summary>
        public int lastPlayedDay = -1;
    }
}
