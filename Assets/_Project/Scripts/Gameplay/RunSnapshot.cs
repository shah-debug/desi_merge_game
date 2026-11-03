using System;
using System.Collections.Generic;
using DesiMerge.Configuration;
using DesiMerge.Persistence;
using UnityEngine;

namespace DesiMerge.Gameplay
{
    /// <summary>One item on the board, in board-local units (x = 0 centre, y = 0 floor).</summary>
    [Serializable]
    public sealed class RunSnapshotItem
    {
        public int level;
        public float x;
        public float y;
        public float rotation;
    }

    /// <summary>Free uses left for one power-up in the saved game.</summary>
    [Serializable]
    public sealed class RunSnapshotFreeUses
    {
        public string id;
        public int left;
    }

    /// <summary>
    /// A game in progress, written when the app goes to the background so that the game survives Android closing the
    /// app. Removed when the game ends, restarts or is left for the menu.
    /// </summary>
    [Serializable]
    public sealed class RunSnapshotData
    {
        public const int CurrentSchemaVersion = 1;
        /// <summary>Sanity bound: a real board holds a few dozen items.</summary>
        public const int MaxItems = 200;

        public int schemaVersion = CurrentSchemaVersion;
        public bool active;
        public string sessionId = string.Empty;
        public long score;
        public int mergeCount;
        public int revivesUsed;
        public int queueCurrent;
        public int queueNext;
        public int sessionDiamonds;
        public long bestBeforeSession;
        public bool hasDropped;
        public bool powerUpUsed;
        public List<string> pendingPowerUpUses = new List<string>();
        public List<RunSnapshotFreeUses> freeUses = new List<RunSnapshotFreeUses>();
        public List<RunSnapshotItem> items = new List<RunSnapshotItem>();

        /// <summary>
        /// True if the snapshot can be put back on a board described by <paramref name="config"/>: known levels, finite
        /// positions inside the container, non-negative counters. Anything else is discarded rather than half-restored.
        /// </summary>
        public bool IsValidFor(GameplayConfig config)
        {
            if (!active || config == null || config.Progression == null || schemaVersion != CurrentSchemaVersion)
            {
                return false;
            }

            var progression = config.Progression;
            if (score < 0 || mergeCount < 0 || revivesUsed < 0 || sessionDiamonds < 0 || string.IsNullOrEmpty(sessionId) ||
                items == null || items.Count > MaxItems ||
                queueCurrent < 0 || queueCurrent >= progression.SpawnableLevelCount || queueNext < 0 || queueNext >= progression.SpawnableLevelCount)
            {
                return false;
            }

            foreach (var item in items)
            {
                if (item == null || item.level < 0 || item.level >= progression.Count ||
                    !IsFinite(item.x) || !IsFinite(item.y) || !IsFinite(item.rotation) ||
                    Mathf.Abs(item.x) > config.HalfWidth + 0.01f || item.y < -0.5f || item.y > config.DropHeight + 2f)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    /// <summary>Loads, saves and clears the in-progress game. Never throws for storage problems (see <see cref="JsonRepository{T}"/>).</summary>
    public sealed class RunSnapshotStore
    {
        private readonly JsonRepository<RunSnapshotData> _repository;

        public RunSnapshotStore(ISaveStore store)
        {
            _repository = new JsonRepository<RunSnapshotData>(store, SaveKey.RunSnapshot);
        }

        public RunSnapshotData Load() => _repository.Load();

        public bool Save(RunSnapshotData data) => data != null && _repository.Save(data);

        /// <summary>Marks the stored game as finished (an inactive snapshot), keeping the write path the same as saves.</summary>
        public bool Clear() => _repository.Save(new RunSnapshotData { active = false });
    }
}
