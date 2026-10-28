using System;
using System.Collections.Generic;
using DesiMerge.Configuration;
using DesiMerge.Persistence;

namespace DesiMerge.Progression
{
    /// <summary>
    /// Long-term player progress and statistics. Fed by real gameplay events from the gameplay loop; UI reads it
    /// and listens to its events. It never knows about the current session's internals.
    /// </summary>
    public interface IPlayerProgressService
    {
        long HighScore { get; }
        long Coins { get; }

        /// <summary>Completed games (game over reached). Opening or abandoning a game doesn't count.</summary>
        int GamesPlayed { get; }

        long TotalMerges { get; }

        /// <summary>Stable ID of the highest-level item ever discovered (empty if none, or if its content was removed).</summary>
        string HighestItemId { get; }

        /// <summary>Stored streak and best streak; see <see cref="StreakRules"/> for the rules.</summary>
        int CurrentStreak { get; }
        int BestStreak { get; }
        int LastPlayedDay { get; }

        /// <summary>Discovered item IDs, including IDs of content that no longer exists (kept in case it returns).</summary>
        IReadOnlyCollection<string> DiscoveredItemIds { get; }

        /// <summary>How the save was loaded at startup (for first-launch / recovery / error messaging).</summary>
        LoadOutcome LoadOutcome { get; }

        event Action Changed;

        /// <summary>Raised once per item ID, the first time it is discovered.</summary>
        event Action<MergeItemDefinition> ItemDiscovered;

        /// <summary>Raised once per recorded merge (created item is null for a final-level clear).</summary>
        event Action<MergeItemDefinition> MergeRecorded;

        /// <summary>Raised once per recorded completed game, with its final score. Not raised again after a revive.</summary>
        event Action<long> GameCompleted;

        /// <summary>Raised for every final score: the first game over and, after a revive, the game's later end.</summary>
        event Action<long> FinalScoreRecorded;

        bool IsDiscovered(string itemId);

        /// <summary>Displayed streak for today (0 if the streak is broken). See <see cref="StreakRules"/>.</summary>
        int DisplayedStreak();

        /// <summary>
        /// Records one completed game: counts it, updates the best score if beaten and the daily streak.
        /// Call exactly once per finished game. Returns true if the score is a new best.
        /// </summary>
        bool SubmitFinalScore(long score);

        /// <summary>
        /// A game already recorded with <see cref="SubmitFinalScore"/> was revived and has ended again: updates the
        /// best score only. It is not counted as another game and doesn't change the streak. Returns true on a new best.
        /// </summary>
        bool SubmitRevivedFinalScore(long score);

        /// <summary>Records one merge. <paramref name="createdItem"/> is discovered if new; null for a final-level clear.</summary>
        void RecordMerge(MergeItemDefinition createdItem);

        /// <summary>Marks an item the player has seen in play (e.g. dropped) as discovered.</summary>
        void RecordSeen(MergeItemDefinition item);

        void AddCoins(long amount);

        /// <summary>Spends coins if the balance allows it. Returns false and changes nothing otherwise.</summary>
        bool TrySpendCoins(long amount);

        /// <summary>Writes pending changes. Cheap when nothing changed. Returns false if the write failed (changes are kept).</summary>
        bool Flush();
    }
}
