using System;
using System.Collections.Generic;
using DesiMerge.Configuration;
using DesiMerge.Core;
using DesiMerge.Persistence;

namespace DesiMerge.Progression
{
    public sealed class PlayerProgressService : IPlayerProgressService
    {
        private readonly JsonRepository<PlayerProgressData> _repository;
        private readonly PlayerProgressData _data;
        private readonly HashSet<string> _discovered;
        private readonly MergeProgression _content;
        private readonly IClock _clock;
        private bool _dirty;

        /// <param name="content">Current item definitions, used to rank discoveries. Optional (tests, tooling).</param>
        /// <param name="clock">Used for the daily streak. Defaults to the system clock.</param>
        public PlayerProgressService(ISaveStore store, MergeProgression content = null, IClock clock = null)
        {
            _repository = new JsonRepository<PlayerProgressData>(store, SaveKey.PlayerProgress);
            _content = content;
            _clock = clock ?? new SystemClock();
            _data = _repository.Load(out var outcome);
            LoadOutcome = outcome;
            Migrate(_data);
            _discovered = new HashSet<string>(_data.discoveredItemIds);
            ReconcileHighestItem();
        }

        public long HighScore => _data.highScore;
        public long Coins => _data.coins;
        public int GamesPlayed => _data.gamesPlayed;
        public long TotalMerges => _data.totalMerges;
        public string HighestItemId => _data.highestItemId;
        public int CurrentStreak => _data.currentStreak;
        public int BestStreak => _data.bestStreak;
        public int LastPlayedDay => _data.lastPlayedDay;
        public IReadOnlyCollection<string> DiscoveredItemIds => _discovered;
        public LoadOutcome LoadOutcome { get; }

        public event Action Changed;
        public event Action<MergeItemDefinition> ItemDiscovered;
        public event Action<MergeItemDefinition> MergeRecorded;
        public event Action<long> GameCompleted;

        public bool IsDiscovered(string itemId) => !string.IsNullOrEmpty(itemId) && _discovered.Contains(itemId);

        public int DisplayedStreak() => StreakRules.Displayed(_data.currentStreak, _data.lastPlayedDay, DayKey.Today(_clock));

        public bool SubmitFinalScore(long score)
        {
            if (score < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(score), "Score cannot be negative.");
            }

            _data.gamesPlayed++;
            var isHighScore = score > _data.highScore;
            if (isHighScore)
            {
                _data.highScore = score;
            }

            StreakRules.RecordCompletedGame(ref _data.currentStreak, ref _data.bestStreak, ref _data.lastPlayedDay, DayKey.Today(_clock));
            MarkChanged();
            GameCompleted?.Invoke(score);
            return isHighScore;
        }

        public void RecordMerge(MergeItemDefinition createdItem)
        {
            _data.totalMerges++;
            TryDiscover(createdItem);
            MarkChanged();
            MergeRecorded?.Invoke(createdItem);
        }

        public void RecordSeen(MergeItemDefinition item)
        {
            if (TryDiscover(item))
            {
                MarkChanged();
            }
        }

        public void AddCoins(long amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
            }

            _data.coins = amount > long.MaxValue - _data.coins ? long.MaxValue : _data.coins + amount;
            MarkChanged();
        }

        public bool TrySpendCoins(long amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
            }

            if (_data.coins < amount)
            {
                return false;
            }

            _data.coins -= amount;
            MarkChanged();
            return true;
        }

        public bool Flush()
        {
            if (!_dirty)
            {
                return true;
            }

            _data.discoveredItemIds.Clear();
            _data.discoveredItemIds.AddRange(_discovered);
            if (!_repository.Save(_data))
            {
                return false;
            }

            _dirty = false;
            return true;
        }

        private bool TryDiscover(MergeItemDefinition item)
        {
            if (item == null || string.IsNullOrEmpty(item.Id) || !_discovered.Add(item.Id))
            {
                return false;
            }

            if (_content != null && _content.IndexOf(item.Id) > _content.IndexOf(_data.highestItemId))
            {
                _data.highestItemId = item.Id;
            }

            ItemDiscovered?.Invoke(item);
            return true;
        }

        private void MarkChanged()
        {
            _dirty = true;
            Changed?.Invoke();
        }

        /// <summary>The highest item is derived from discoveries; repair it if its content was removed or reordered.</summary>
        private void ReconcileHighestItem()
        {
            if (_content == null)
            {
                return;
            }

            var best = -1;
            foreach (var id in _discovered)
            {
                best = Math.Max(best, _content.IndexOf(id));
            }

            _data.highestItemId = best >= 0 ? _content.Get(best).Id : string.Empty;
        }

        /// <summary>Upgrades older saves in place and clamps tampered or invalid values. Nothing is written here.</summary>
        private static void Migrate(PlayerProgressData data)
        {
            // v1 → v2: new fields arrive with their defaults (JsonUtility keeps initializers). Nothing else to convert.
            data.schemaVersion = PlayerProgressData.CurrentSchemaVersion;
            data.highScore = Math.Max(0, data.highScore);
            data.coins = Math.Max(0, data.coins);
            data.gamesPlayed = Math.Max(0, data.gamesPlayed);
            data.totalMerges = Math.Max(0, data.totalMerges);
            data.currentStreak = Math.Max(0, data.currentStreak);
            data.bestStreak = Math.Max(data.bestStreak, data.currentStreak);
            if (data.lastPlayedDay < -1)
            {
                data.lastPlayedDay = -1;
            }

            if (data.discoveredItemIds == null)
            {
                data.discoveredItemIds = new List<string>();
            }

            data.discoveredItemIds.RemoveAll(string.IsNullOrWhiteSpace);
            if (data.highestItemId == null)
            {
                data.highestItemId = string.Empty;
            }
        }
    }
}
