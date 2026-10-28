using System;
using System.Collections.Generic;
using DesiMerge.Configuration;
using DesiMerge.Persistence;
using UnityEngine;

namespace DesiMerge.Progression
{

    /// <summary>Save file "achievements.json". Progress itself is derived from player statistics, so only unlocks are stored.</summary>
    [Serializable]
    public sealed class AchievementSaveData
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public List<string> unlockedIds = new List<string>();
        public List<string> rewardedIds = new List<string>();
    }

    public interface IAchievementService
    {
        IReadOnlyList<AchievementDefinition> Definitions { get; }
        int UnlockedCount { get; }

        /// <summary>Raised once per achievement, when it is unlocked during play.</summary>
        event Action<AchievementDefinition> Unlocked;

        bool IsUnlocked(string achievementId);

        /// <summary>Current progress towards the target (from real statistics), clamped to the target.</summary>
        long ProgressOf(AchievementDefinition achievement);

        long TargetOf(AchievementDefinition achievement);

        bool Flush();
    }

    /// <summary>
    /// Unlocks achievements from <see cref="IPlayerProgressService"/> changes. Each unlock (and its reward, if any) happens
    /// exactly once and is persisted by ID. Achievements already earned by existing statistics (e.g. added in an update)
    /// unlock silently at startup without a notification. Invalid definitions are skipped.
    /// </summary>
    public sealed class AchievementService : IAchievementService, IDisposable
    {
        private readonly JsonRepository<AchievementSaveData> _repository;
        private readonly AchievementSaveData _data;
        private readonly HashSet<string> _unlocked;
        private readonly HashSet<string> _rewarded;
        private readonly List<AchievementDefinition> _definitions = new List<AchievementDefinition>();
        private readonly IPlayerProgressService _progress;
        private readonly IRewardGranter _rewards;
        private bool _dirty;
        private bool _evaluating;

        public AchievementService(ISaveStore store, AchievementCatalog catalog, IPlayerProgressService progress, MergeProgression content, IRewardGranter rewards)
        {
            _repository = new JsonRepository<AchievementSaveData>(store, SaveKey.Achievements);
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _rewards = rewards;
            _data = _repository.Load();
            _data.schemaVersion = AchievementSaveData.CurrentSchemaVersion;
            _unlocked = new HashSet<string>(_data.unlockedIds ?? new List<string>());
            _rewarded = new HashSet<string>(_data.rewardedIds ?? new List<string>());

            if (catalog != null)
            {
                foreach (var error in catalog.Validate(content))
                {
                    Debug.LogWarning($"[Achievements] {error} It will be skipped.");
                }

                var seen = new HashSet<string>();
                foreach (var definition in catalog.All)
                {
                    var valid = definition != null && !string.IsNullOrWhiteSpace(definition.id) && seen.Add(definition.id)
                                && (definition.type == AchievementType.DiscoverItem
                                    ? content == null || content.IndexOf(definition.itemId) >= 0
                                    : definition.target > 0);
                    if (valid)
                    {
                        _definitions.Add(definition);
                    }
                }
            }

            Evaluate(notify: false);
            _progress.Changed += OnProgressChanged;
        }

        public IReadOnlyList<AchievementDefinition> Definitions => _definitions;
        public int UnlockedCount => CountUnlockedDefinitions();

        public event Action<AchievementDefinition> Unlocked;

        public bool IsUnlocked(string achievementId) => !string.IsNullOrEmpty(achievementId) && _unlocked.Contains(achievementId);

        public long TargetOf(AchievementDefinition achievement) => achievement.type == AchievementType.DiscoverItem ? 1 : achievement.target;

        public long ProgressOf(AchievementDefinition achievement)
        {
            long value;
            switch (achievement.type)
            {
                case AchievementType.CompleteGames:
                    value = _progress.GamesPlayed;
                    break;
                case AchievementType.TotalMerges:
                    value = _progress.TotalMerges;
                    break;
                case AchievementType.BestScore:
                    value = _progress.HighScore;
                    break;
                case AchievementType.DiscoverItem:
                    value = _progress.IsDiscovered(achievement.itemId) ? 1 : 0;
                    break;
                case AchievementType.DayStreak:
                    value = _progress.BestStreak;
                    break;
                default:
                    value = 0;
                    break;
            }

            return IsUnlocked(achievement.id) ? TargetOf(achievement) : Math.Min(value, TargetOf(achievement));
        }

        public bool Flush()
        {
            if (!_dirty)
            {
                return true;
            }

            _data.unlockedIds = new List<string>(_unlocked);
            _data.rewardedIds = new List<string>(_rewarded);
            if (!_repository.Save(_data))
            {
                return false;
            }

            _dirty = false;
            return true;
        }

        public void Dispose()
        {
            _progress.Changed -= OnProgressChanged;
        }

        private void OnProgressChanged() => Evaluate(notify: true);

        private void Evaluate(bool notify)
        {
            // Granting a reward changes progress, which re-enters here; the outer pass handles everything.
            if (_evaluating)
            {
                return;
            }

            _evaluating = true;
            try
            {
                foreach (var achievement in _definitions)
                {
                    if (_unlocked.Contains(achievement.id))
                    {
                        // A reward that could not be applied when unlocked (e.g. charges at their maximum) is retried.
                        TryGrantReward(achievement);
                        continue;
                    }

                    if (ProgressOf(achievement) < TargetOf(achievement))
                    {
                        continue;
                    }

                    _unlocked.Add(achievement.id);
                    _dirty = true;
                    TryGrantReward(achievement);

                    if (notify)
                    {
                        Unlocked?.Invoke(achievement);
                    }
                }
            }
            finally
            {
                _evaluating = false;
            }
        }

        /// <summary>Applies an unlocked achievement's reward once; it is recorded as rewarded only if the grant succeeded.</summary>
        private void TryGrantReward(AchievementDefinition achievement)
        {
            if (!achievement.reward.IsSomething || _rewarded.Contains(achievement.id) || _rewards == null)
            {
                return;
            }

            if (_rewards.Grant(GrantIdFor(achievement), achievement.reward))
            {
                _rewarded.Add(achievement.id);
                _dirty = true;
            }
        }

        internal static string GrantIdFor(AchievementDefinition achievement) => "achievement:" + achievement.id;

        private int CountUnlockedDefinitions()
        {
            var count = 0;
            foreach (var achievement in _definitions)
            {
                if (_unlocked.Contains(achievement.id))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
