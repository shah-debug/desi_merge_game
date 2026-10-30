using System;
using System.Collections.Generic;
using DesiMerge.Configuration;
using DesiMerge.Core;
using DesiMerge.Persistence;
using DesiMerge.PowerUps;
using UnityEngine;

namespace DesiMerge.Progression
{
    [Serializable]
    public sealed class MissionProgressEntry
    {
        public string id;
        public int period;
        public int periodKey;
        public long progress;
        public bool completed;
        public bool claimed;
    }

    [Serializable]
    public sealed class MissionSaveData
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public int latestDay = -1;
        public List<MissionProgressEntry> entries = new List<MissionProgressEntry>();
    }

    public sealed class MissionState
    {
        internal MissionState(MissionDefinition definition, MissionProgressEntry entry)
        {
            Definition = definition;
            Entry = entry;
        }

        public MissionDefinition Definition { get; }
        internal MissionProgressEntry Entry { get; }
        public long Target => Definition.type == MissionType.CreateItem ? 1 : Definition.target;
        public long Progress => Math.Min(Entry.progress, Target);
        public bool Completed => Entry.completed;
        public bool Claimed => Entry.claimed;
        public bool CanClaim => Entry.completed && !Entry.claimed;
    }

    public interface IMissionService
    {
        IReadOnlyList<MissionState> Daily { get; }
        IReadOnlyList<MissionState> Weekly { get; }
        int ClaimableCount { get; }
        int DayKey { get; }
        int WeekKey { get; }

        event Action Changed;

        /// <summary>Raised once per mission and period, when it is completed.</summary>
        event Action<MissionDefinition> Completed;

        /// <summary>Rolls over daily/weekly missions if the date changed. Called by every event and when screens open.</summary>
        void Refresh();

        ClaimResult TryClaim(string missionId);

        bool Flush();
    }

    /// <summary>
    /// Daily missions change at 00:00 UTC; weekly missions on Monday 00:00 UTC (<see cref="Core.DayKey.WeekOf"/>).
    /// Each period shows a fixed number of missions picked deterministically from the catalog by the period number,
    /// so the same day always offers the same missions. Unclaimed rewards of an ended period expire with it.
    /// Progress comes only from real events (progress service and power-up service), never from polling.
    /// </summary>
    public sealed class MissionService : IMissionService, IDisposable
    {
        private readonly JsonRepository<MissionSaveData> _repository;
        private readonly MissionSaveData _data;
        private readonly List<MissionDefinition> _dailyPool = new List<MissionDefinition>();
        private readonly List<MissionDefinition> _weeklyPool = new List<MissionDefinition>();
        private readonly List<MissionState> _daily = new List<MissionState>();
        private readonly List<MissionState> _weekly = new List<MissionState>();
        private readonly IPlayerProgressService _progress;
        private readonly IPowerUpService _powerUps;
        private readonly MergeProgression _content;
        private readonly IClock _clock;
        private readonly IRewardGranter _rewards;
        private readonly int _dailySlots;
        private readonly int _weeklySlots;
        private bool _powerUpUsedThisGame;
        private bool _dirty;

        public MissionService(ISaveStore store, MissionCatalog catalog, IPlayerProgressService progress, IPowerUpService powerUps,
            MergeProgression content, IClock clock, IRewardGranter rewards)
        {
            _repository = new JsonRepository<MissionSaveData>(store, SaveKey.Missions);
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _powerUps = powerUps;
            _content = content;
            _clock = clock ?? new SystemClock();
            _rewards = rewards;
            _data = _repository.Load();
            _data.schemaVersion = MissionSaveData.CurrentSchemaVersion;
            _data.entries = _data.entries ?? new List<MissionProgressEntry>();
            _data.entries.RemoveAll(entry => entry == null || string.IsNullOrWhiteSpace(entry.id));

            _dailySlots = catalog != null ? catalog.DailySlots : 0;
            _weeklySlots = catalog != null ? catalog.WeeklySlots : 0;
            if (catalog != null)
            {
                var ids = new HashSet<string>();
                foreach (var mission in catalog.All)
                {
                    if (!MissionCatalog.IsValid(mission, content) || !ids.Add(mission.id))
                    {
                        Debug.LogWarning("[Missions] Skipping an invalid or duplicate mission definition.");
                        continue;
                    }

                    (mission.period == MissionPeriod.Weekly ? _weeklyPool : _dailyPool).Add(mission);
                }
            }

            Refresh();
            _progress.MergeRecorded += OnMergeRecorded;
            _progress.GameCompleted += OnGameCompleted;
            _progress.FinalScoreRecorded += OnFinalScoreRecorded;
            if (_powerUps != null)
            {
                _powerUps.Used += OnPowerUpUsed;
                _powerUps.GameBegan += OnGameBegan;
            }
        }

        public IReadOnlyList<MissionState> Daily => _daily;
        public IReadOnlyList<MissionState> Weekly => _weekly;
        public int DayKey { get; private set; }
        public int WeekKey { get; private set; }

        public int ClaimableCount
        {
            get
            {
                var count = 0;
                foreach (var state in _daily)
                {
                    count += state.CanClaim ? 1 : 0;
                }

                foreach (var state in _weekly)
                {
                    count += state.CanClaim ? 1 : 0;
                }

                return count;
            }
        }

        public event Action Changed;
        public event Action<MissionDefinition> Completed;

        internal static string GrantPrefixFor(MissionPeriod period, int key, string id) => $"mission:{(period == MissionPeriod.Daily ? "d" : "w")}{key}:{id}";

        public static IReadOnlyList<MissionDefinition> SelectFor(int key, IReadOnlyList<MissionDefinition> pool, int slots)
        {
            var selected = new List<MissionDefinition>();
            var count = Math.Min(slots, pool.Count);
            if (count <= 0)
            {
                return selected;
            }

            var start = (int)(((long)key * count) % pool.Count);
            if (start < 0)
            {
                start += pool.Count;
            }

            for (var i = 0; i < count; i++)
            {
                selected.Add(pool[(start + i) % pool.Count]);
            }

            return selected;
        }

        public void Refresh()
        {
            var day = Core.DayKey.Effective(Core.DayKey.Today(_clock), _data.latestDay);
            var changed = day != _data.latestDay;
            _data.latestDay = day;
            DayKey = day;
            WeekKey = Core.DayKey.WeekOf(day);
            changed |= Roll(MissionPeriod.Daily, DayKey, _dailyPool, _dailySlots, _daily);
            changed |= Roll(MissionPeriod.Weekly, WeekKey, _weeklyPool, _weeklySlots, _weekly);
            if (changed)
            {
                _dirty = true;
                Changed?.Invoke();
            }
        }

        public ClaimResult TryClaim(string missionId)
        {
            Refresh();
            var state = Find(missionId);
            if (state == null || !state.CanClaim)
            {
                return ClaimResult.NotAvailable;
            }

            var period = state.Definition.period;
            var key = period == MissionPeriod.Daily ? DayKey : WeekKey;
            var result = RewardClaims.GrantAll(_rewards, GrantPrefixFor(period, key, missionId), new[] { state.Definition.reward });
            if (result != ClaimResult.Claimed)
            {
                return result;
            }

            state.Entry.claimed = true;
            _dirty = true;
            Flush();
            Changed?.Invoke();
            return ClaimResult.Claimed;
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
            if (_powerUps != null)
            {
                _powerUps.Used -= OnPowerUpUsed;
                _powerUps.GameBegan -= OnGameBegan;
            }
        }

        private bool Roll(MissionPeriod period, int key, IReadOnlyList<MissionDefinition> pool, int slots, List<MissionState> states)
        {
            var selection = SelectFor(key, pool, slots);
            var changed = _data.entries.RemoveAll(entry => entry.period == (int)period && (entry.periodKey != key || !Contains(selection, entry.id))) > 0;
            states.Clear();
            foreach (var definition in selection)
            {
                var entry = _data.entries.Find(candidate => candidate.period == (int)period && candidate.id == definition.id);
                if (entry == null)
                {
                    entry = new MissionProgressEntry { id = definition.id, period = (int)period, periodKey = key };
                    _data.entries.Add(entry);
                    changed = true;
                }

                entry.progress = Math.Max(0, entry.progress);
                states.Add(new MissionState(definition, entry));
            }

            return changed;
        }

        private static bool Contains(IReadOnlyList<MissionDefinition> selection, string id)
        {
            foreach (var definition in selection)
            {
                if (definition.id == id)
                {
                    return true;
                }
            }

            return false;
        }

        private MissionState Find(string id)
        {
            foreach (var state in _daily)
            {
                if (state.Definition.id == id)
                {
                    return state;
                }
            }

            foreach (var state in _weekly)
            {
                if (state.Definition.id == id)
                {
                    return state;
                }
            }

            return null;
        }

        private void OnGameBegan() => _powerUpUsedThisGame = false;

        private void OnMergeRecorded(MergeItemDefinition created)
        {
            Refresh();
            Advance(MissionType.Merges, state => state.Entry.progress + 1);
            if (created == null)
            {
                return;
            }

            Advance(MissionType.CreateItem, state => _content == null
                ? (created.Id == state.Definition.itemId ? 1 : state.Entry.progress)
                : (_content.IndexOf(created.Id) >= _content.IndexOf(state.Definition.itemId) ? 1 : state.Entry.progress));
        }

        private void OnGameCompleted(long score)
        {
            Refresh();
            Advance(MissionType.CompleteGames, state => state.Entry.progress + 1);
            if (!_powerUpUsedThisGame)
            {
                Advance(MissionType.GamesWithoutPowerUps, state => state.Entry.progress + 1);
            }
        }

        private void OnFinalScoreRecorded(long score)
        {
            Refresh();
            Advance(MissionType.ScoreInOneGame, state => Math.Max(state.Entry.progress, score));
        }

        private void OnPowerUpUsed(string powerUpId)
        {
            _powerUpUsedThisGame = true;
            Refresh();
            Advance(MissionType.UsePowerUps, state =>
                string.IsNullOrEmpty(state.Definition.powerUpId) || state.Definition.powerUpId == powerUpId ? state.Entry.progress + 1 : state.Entry.progress);
        }

        private void Advance(MissionType type, Func<MissionState, long> next)
        {
            List<MissionDefinition> completed = null;
            var changed = false;
            foreach (var list in new[] { _daily, _weekly })
            {
                foreach (var state in list)
                {
                    if (state.Definition.type != type || state.Entry.completed)
                    {
                        continue;
                    }

                    var value = next(state);
                    if (value == state.Entry.progress)
                    {
                        continue;
                    }

                    state.Entry.progress = value;
                    changed = true;
                    if (value >= state.Target)
                    {
                        state.Entry.completed = true;
                        (completed ??= new List<MissionDefinition>()).Add(state.Definition);
                    }
                }
            }

            if (!changed)
            {
                return;
            }

            _dirty = true;
            Changed?.Invoke();
            if (completed == null)
            {
                return;
            }

            foreach (var definition in completed)
            {
                Completed?.Invoke(definition);
            }
        }
    }
}
