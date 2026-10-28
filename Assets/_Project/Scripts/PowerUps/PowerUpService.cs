using System;
using System.Collections.Generic;
using DesiMerge.Persistence;
using UnityEngine;

namespace DesiMerge.PowerUps
{
    /// <summary>
    /// Save file "power_ups.json": saved bonus charges and the IDs of rewards already applied.
    /// Free per-game uses are not saved (they reset with every new game).
    /// </summary>
    [Serializable]
    public sealed class PowerUpSaveData
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public List<PowerUpCharges> charges = new List<PowerUpCharges>();
        /// <summary>Rewards already applied, newest last (bounded). Makes every grant idempotent.</summary>
        public List<string> appliedGrantIds = new List<string>();
    }

    [Serializable]
    public sealed class PowerUpCharges
    {
        public string id;
        public int count;
    }

    public interface IPowerUpService
    {
        /// <summary>Valid definitions, in display order.</summary>
        IReadOnlyList<PowerUpDefinition> Definitions { get; }

        LoadOutcome LoadOutcome { get; }

        event Action Changed;

        PowerUpDefinition Find(string id);

        int FreeUsesLeft(string id);

        int SavedCharges(string id);

        /// <summary>Free uses left this game plus saved charges.</summary>
        int Available(string id);

        /// <summary>Starts a new game: free uses go back to each definition's allowance. Saved charges are untouched.</summary>
        void BeginGame();

        /// <summary>
        /// Uses one charge (free uses first, then saved charges). Call only after the effect has succeeded.
        /// Returns false and changes nothing if none are available.
        /// </summary>
        bool TryConsume(string id);

        /// <summary>
        /// Adds saved charges for a reward identified by <paramref name="grantId"/>. A grant ID is applied at most
        /// once, ever (repeats return true without adding anything), and the result is written immediately, so a
        /// reward survives an interrupted claim without being duplicated. Returns false (nothing changes) for an
        /// unknown power-up, a non-positive amount, or when the charges would exceed the definition's maximum.
        /// </summary>
        bool TryGrant(string grantId, string powerUpId, int amount);

        bool HasApplied(string grantId);

        bool Flush();
    }

    public sealed class PowerUpService : IPowerUpService
    {
        /// <summary>Old grant IDs beyond this many are forgotten (rewards are daily or one-off, so only recent IDs matter).</summary>
        internal const int MaxRememberedGrants = 200;

        private readonly JsonRepository<PowerUpSaveData> _repository;
        private readonly PowerUpSaveData _data;
        private readonly List<PowerUpDefinition> _definitions = new List<PowerUpDefinition>();
        private readonly Dictionary<string, int> _freeUsesLeft = new Dictionary<string, int>();
        private bool _dirty;

        public PowerUpService(ISaveStore store, PowerUpCatalog catalog)
        {
            _repository = new JsonRepository<PowerUpSaveData>(store, SaveKey.PowerUps);
            _data = _repository.Load(out var outcome);
            LoadOutcome = outcome;

            if (catalog != null)
            {
                var ids = new HashSet<string>();
                foreach (var definition in catalog.PowerUps)
                {
                    if (PowerUpCatalog.IsValid(definition) && ids.Add(definition.id))
                    {
                        _definitions.Add(definition);
                    }
                    else
                    {
                        Debug.LogWarning("[PowerUps] Skipping an invalid or duplicate power-up definition.");
                    }
                }
            }

            Migrate(_data);
            BeginGame();
        }

        public IReadOnlyList<PowerUpDefinition> Definitions => _definitions;
        public LoadOutcome LoadOutcome { get; }

        public event Action Changed;

        public PowerUpDefinition Find(string id)
        {
            foreach (var definition in _definitions)
            {
                if (definition.id == id)
                {
                    return definition;
                }
            }

            return null;
        }

        public int FreeUsesLeft(string id) => id != null && _freeUsesLeft.TryGetValue(id, out var left) ? left : 0;

        public int SavedCharges(string id)
        {
            var entry = FindCharges(id);
            return entry != null ? entry.count : 0;
        }

        public int Available(string id) => Find(id) == null ? 0 : FreeUsesLeft(id) + SavedCharges(id);

        public void BeginGame()
        {
            _freeUsesLeft.Clear();
            foreach (var definition in _definitions)
            {
                _freeUsesLeft[definition.id] = definition.freeUsesPerGame;
            }

            Changed?.Invoke();
        }

        public bool TryConsume(string id)
        {
            if (Find(id) == null)
            {
                return false;
            }

            if (FreeUsesLeft(id) > 0)
            {
                _freeUsesLeft[id]--;
                Changed?.Invoke();
                return true;
            }

            var saved = FindCharges(id);
            if (saved == null || saved.count <= 0)
            {
                return false;
            }

            saved.count--;
            _dirty = true;
            Changed?.Invoke();
            return true;
        }

        public bool HasApplied(string grantId) => !string.IsNullOrEmpty(grantId) && _data.appliedGrantIds.Contains(grantId);

        public bool TryGrant(string grantId, string powerUpId, int amount)
        {
            if (string.IsNullOrEmpty(grantId))
            {
                throw new ArgumentException("A grant needs a stable ID.", nameof(grantId));
            }

            if (HasApplied(grantId))
            {
                return true;
            }

            var definition = Find(powerUpId);
            if (definition == null || amount <= 0 || SavedCharges(powerUpId) + amount > definition.maxSavedCharges)
            {
                return false;
            }

            var entry = FindCharges(powerUpId);
            if (entry == null)
            {
                entry = new PowerUpCharges { id = powerUpId };
                _data.charges.Add(entry);
            }

            entry.count += amount;
            _data.appliedGrantIds.Add(grantId);
            if (_data.appliedGrantIds.Count > MaxRememberedGrants)
            {
                _data.appliedGrantIds.RemoveRange(0, _data.appliedGrantIds.Count - MaxRememberedGrants);
            }

            _dirty = true;
            // Rewards are rare: write now, before the caller records the claim (see DailyChallengeService.TryClaim).
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

        private PowerUpCharges FindCharges(string id)
        {
            foreach (var entry in _data.charges)
            {
                if (entry.id == id)
                {
                    return entry;
                }
            }

            return null;
        }

        /// <summary>Clamps tampered values. Charges for power-ups no longer in the catalog are kept (they may return).</summary>
        private static void Migrate(PowerUpSaveData data)
        {
            data.schemaVersion = PowerUpSaveData.CurrentSchemaVersion;
            if (data.charges == null)
            {
                data.charges = new List<PowerUpCharges>();
            }

            if (data.appliedGrantIds == null)
            {
                data.appliedGrantIds = new List<string>();
            }

            data.charges.RemoveAll(entry => entry == null || string.IsNullOrWhiteSpace(entry.id));
            foreach (var entry in data.charges)
            {
                entry.count = Math.Max(0, entry.count);
            }

            data.appliedGrantIds.RemoveAll(string.IsNullOrEmpty);
        }
    }
}
