using System;
using System.Collections.Generic;
using DesiMerge.Configuration;
using DesiMerge.Core;
using DesiMerge.Persistence;
using UnityEngine;

namespace DesiMerge.Progression
{
    /// <summary>Save file "daily_challenge.json": progress for one day's challenge.</summary>
    [Serializable]
    public sealed class DailyChallengeSaveData
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        /// <summary>UTC day number the progress belongs to; -1 = none yet.</summary>
        public int dayNumber = -1;
        public string challengeId = string.Empty;
        public long progress;
        public bool completed;
        public bool claimed;
        /// <summary>Latest day ever seen. A clock moved backwards never re-opens an earlier day.</summary>
        public int latestDay = -1;
    }

    public interface IDailyChallengeService
    {
        /// <summary>Today's challenge, or null if the catalog has no valid challenges.</summary>
        DailyChallengeDefinition Current { get; }
        int DayNumber { get; }
        long Progress { get; }
        long Target { get; }
        bool IsCompleted { get; }
        bool IsClaimed { get; }

        /// <summary>True when completed, not yet claimed, and the challenge has a reward.</summary>
        bool CanClaim { get; }

        event Action Changed;

        /// <summary>Raised once per day, when today's challenge is completed.</summary>
        event Action<DailyChallengeDefinition> Completed;

        /// <summary>Rolls over to a new day's challenge if the date changed. Call when a screen opens; there is no timer.</summary>
        void Refresh();

        /// <summary>Grants the reward exactly once. Returns false if not claimable (or already claimed).</summary>
        bool TryClaim();

        bool Flush();
    }

    /// <summary>
    /// Date policy: days are UTC day numbers (<see cref="DayKey"/>). The effective day is max(today, latest day seen),
    /// so moving the device clock back can't reset or repeat a challenge; moving it forward does start the next
    /// day's challenge (local clocks can't be trusted against manipulation). Progress only changes from real events
    /// reported by <see cref="IPlayerProgressService"/> (merges, completed games).
    /// </summary>
    public sealed class DailyChallengeService : IDailyChallengeService, IDisposable
    {
        private readonly JsonRepository<DailyChallengeSaveData> _repository;
        private readonly DailyChallengeSaveData _data;
        private readonly List<DailyChallengeDefinition> _pool = new List<DailyChallengeDefinition>();
        private readonly IPlayerProgressService _progress;
        private readonly MergeProgression _content;
        private readonly IClock _clock;
        private readonly IRewardGranter _rewards;
        private bool _dirty;

        public DailyChallengeService(ISaveStore store, DailyChallengeCatalog catalog, IPlayerProgressService progress, MergeProgression content, IClock clock, IRewardGranter rewards)
        {
            _repository = new JsonRepository<DailyChallengeSaveData>(store, SaveKey.DailyChallenge);
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _content = content;
            _clock = clock ?? new SystemClock();
            _rewards = rewards;
            _data = _repository.Load();
            _data.schemaVersion = DailyChallengeSaveData.CurrentSchemaVersion;
            _data.challengeId = _data.challengeId ?? string.Empty;
            _data.progress = Math.Max(0, _data.progress);

            if (catalog != null)
            {
                foreach (var error in catalog.Validate(content))
                {
                    Debug.LogWarning($"[Daily] {error} It will be skipped.");
                }

                var seen = new HashSet<string>();
                foreach (var challenge in catalog.All)
                {
                    if (DailyChallengeCatalog.IsValid(challenge, content) && seen.Add(challenge.id))
                    {
                        _pool.Add(challenge);
                    }
                }
            }

            Refresh();
            _progress.MergeRecorded += OnMergeRecorded;
            _progress.GameCompleted += OnGameCompleted;
            _progress.FinalScoreRecorded += OnFinalScoreRecorded;
        }

        public DailyChallengeDefinition Current { get; private set; }
        public int DayNumber => _data.dayNumber;
        public long Progress => Math.Min(_data.progress, Target);
        public long Target => Current == null ? 0 : Current.type == DailyChallengeType.CreateItem ? 1 : Current.target;
        public bool IsCompleted => Current != null && _data.completed;
        public bool IsClaimed => _data.claimed;
        public bool CanClaim => IsCompleted && !_data.claimed && Current.reward.IsSomething;

        public event Action Changed;
        public event Action<DailyChallengeDefinition> Completed;

        /// <summary>Stable reward ID for one day's challenge, so its reward is applied at most once.</summary>
        internal static string GrantIdFor(int dayNumber, DailyChallengeDefinition challenge) => $"daily:{dayNumber}:{challenge.id}";

        public static DailyChallengeDefinition SelectFor(int dayNumber, IReadOnlyList<DailyChallengeDefinition> pool)
        {
            if (pool.Count == 0)
            {
                return null;
            }

            var index = dayNumber % pool.Count;
            return pool[index < 0 ? index + pool.Count : index];
        }

        public void Refresh()
        {
            var effectiveDay = DayKey.Effective(DayKey.Today(_clock), _data.latestDay);
            var expected = SelectFor(effectiveDay, _pool);
            var changed = false;

            if (effectiveDay != _data.latestDay)
            {
                _data.latestDay = effectiveDay;
                changed = true;
            }

            // New day, or the stored challenge no longer exists in the catalog: start today's challenge fresh.
            var expectedId = expected != null ? expected.id : string.Empty;
            if (_data.dayNumber != effectiveDay || _data.challengeId != expectedId)
            {
                _data.dayNumber = effectiveDay;
                _data.challengeId = expectedId;
                _data.progress = 0;
                _data.completed = false;
                _data.claimed = false;
                changed = true;
            }

            Current = expected;
            if (changed)
            {
                _dirty = true;
                Changed?.Invoke();
            }
        }

        public bool TryClaim()
        {
            Refresh();
            if (!CanClaim)
            {
                return false;
            }

            // Grant first (the granter saves it under a stable ID), then record the claim. If the app dies in between,
            // the claim reappears and claiming again finds the grant already applied: no loss, no duplicate.
            if (_rewards == null || !_rewards.Grant(GrantIdFor(_data.dayNumber, Current), Current.reward))
            {
                return false;
            }

            _data.claimed = true;
            _dirty = true;
            Flush();
            Changed?.Invoke();
            return true;
        }

        public bool Flush()
        {
            if (!_dirty)
            {
                return true;
            }

            if (!_repository.Save(_data))
            {
                return false;
            }

            _dirty = false;
            return true;
        }

        public void Dispose()
        {
            _progress.MergeRecorded -= OnMergeRecorded;
            _progress.GameCompleted -= OnGameCompleted;
            _progress.FinalScoreRecorded -= OnFinalScoreRecorded;
        }

        private void OnMergeRecorded(MergeItemDefinition created)
        {
            Refresh();
            if (Current == null || _data.completed)
            {
                return;
            }

            switch (Current.type)
            {
                case DailyChallengeType.MergesInDay:
                    Advance(_data.progress + 1);
                    break;
                case DailyChallengeType.CreateItem:
                    if (created != null && _content != null && _content.IndexOf(created.Id) >= _content.IndexOf(Current.itemId))
                    {
                        Advance(1);
                    }
                    else if (created != null && _content == null && created.Id == Current.itemId)
                    {
                        Advance(1);
                    }

                    break;
            }
        }

        /// <summary>Counts each game once; a revived game ending again is not a new game.</summary>
        private void OnGameCompleted(long score)
        {
            Refresh();
            if (Current != null && !_data.completed && Current.type == DailyChallengeType.CompleteGames)
            {
                Advance(_data.progress + 1);
            }
        }

        /// <summary>Every final score, including the higher one a revived game ends with.</summary>
        private void OnFinalScoreRecorded(long score)
        {
            Refresh();
            if (Current != null && !_data.completed && Current.type == DailyChallengeType.ScoreInOneGame)
            {
                Advance(Math.Max(_data.progress, score));
            }
        }

        private void Advance(long progress)
        {
            if (progress == _data.progress)
            {
                return;
            }

            _data.progress = progress;
            _dirty = true;
            var justCompleted = !_data.completed && progress >= Target;
            if (justCompleted)
            {
                _data.completed = true;
            }

            Changed?.Invoke();
            if (justCompleted)
            {
                Completed?.Invoke(Current);
            }
        }
    }
}
