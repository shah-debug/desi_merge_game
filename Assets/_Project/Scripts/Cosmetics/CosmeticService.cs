using System;
using System.Collections.Generic;
using DesiMerge.Persistence;
using DesiMerge.Progression;

namespace DesiMerge.Cosmetics
{
    /// <summary>Save file "cosmetics.json": owned cosmetic IDs and the selected ID per kind.</summary>
    [Serializable]
    public sealed class CosmeticSaveData
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public List<string> ownedIds = new List<string>();
        public string selectedContainerTheme = string.Empty;
        public string selectedItemSkin = string.Empty;
    }

    public interface ICosmeticService
    {
        IReadOnlyList<CosmeticDefinition> Definitions { get; }

        event Action Changed;

        bool IsOwned(string id);

        /// <summary>The selected cosmetic of a kind; falls back to the kind's default when the saved one is missing or not owned.</summary>
        CosmeticDefinition Selected(CosmeticKind kind);

        /// <summary>Selects an existing, owned cosmetic. Returns false and changes nothing otherwise.</summary>
        bool TrySelect(string id);

        bool Flush();
    }

    /// <summary>
    /// Ownership is separate from selection: unlocking never changes the selection, and only owned, existing
    /// cosmetics can be selected. Achievement-linked cosmetics are owned once their achievement is unlocked
    /// (checked at startup and on every unlock). There is no purchase path.
    /// </summary>
    public sealed class CosmeticService : ICosmeticService, IDisposable
    {
        private readonly JsonRepository<CosmeticSaveData> _repository;
        private readonly CosmeticSaveData _data;
        private readonly List<CosmeticDefinition> _definitions = new List<CosmeticDefinition>();
        private readonly HashSet<string> _owned;
        private readonly IAchievementService _achievements;
        private bool _dirty;

        public CosmeticService(ISaveStore store, CosmeticCatalog catalog, IAchievementService achievements)
        {
            _repository = new JsonRepository<CosmeticSaveData>(store, SaveKey.Cosmetics);
            _data = _repository.Load();
            _data.schemaVersion = CosmeticSaveData.CurrentSchemaVersion;
            _owned = new HashSet<string>(_data.ownedIds ?? new List<string>());
            _owned.RemoveWhere(string.IsNullOrWhiteSpace);
            _achievements = achievements;

            if (catalog != null)
            {
                var ids = new HashSet<string>();
                foreach (var definition in catalog.Cosmetics)
                {
                    if (CosmeticCatalog.IsValid(definition) && ids.Add(definition.id))
                    {
                        _definitions.Add(definition);
                    }
                }
            }

            UnlockEarned();
            if (_achievements != null)
            {
                _achievements.Unlocked += OnAchievementUnlocked;
            }
        }

        public IReadOnlyList<CosmeticDefinition> Definitions => _definitions;

        public event Action Changed;

        public bool IsOwned(string id)
        {
            var definition = Find(id);
            return definition != null && (definition.unlock == CosmeticUnlock.Default || _owned.Contains(id));
        }

        public CosmeticDefinition Selected(CosmeticKind kind)
        {
            var saved = Find(SelectedId(kind));
            if (saved != null && saved.kind == kind && IsOwned(saved.id))
            {
                return saved;
            }

            foreach (var definition in _definitions)
            {
                if (definition.kind == kind && definition.unlock == CosmeticUnlock.Default)
                {
                    return definition;
                }
            }

            return null;
        }

        public bool TrySelect(string id)
        {
            var definition = Find(id);
            if (definition == null || !IsOwned(id))
            {
                return false;
            }

            if (definition.kind == CosmeticKind.ContainerTheme)
            {
                _data.selectedContainerTheme = id;
            }
            else
            {
                _data.selectedItemSkin = id;
            }

            _dirty = true;
            Changed?.Invoke();
            return true;
        }

        public bool Flush()
        {
            if (!_dirty)
            {
                return true;
            }

            _data.ownedIds = new List<string>(_owned);
            if (!_repository.Save(_data))
            {
                return false;
            }

            _dirty = false;
            return true;
        }

        public void Dispose()
        {
            if (_achievements != null)
            {
                _achievements.Unlocked -= OnAchievementUnlocked;
            }
        }

        private void OnAchievementUnlocked(AchievementDefinition achievement) => UnlockEarned();

        private void UnlockEarned()
        {
            if (_achievements == null)
            {
                return;
            }

            var changed = false;
            foreach (var definition in _definitions)
            {
                if (definition.unlock == CosmeticUnlock.Achievement && _achievements.IsUnlocked(definition.achievementId) && _owned.Add(definition.id))
                {
                    changed = true;
                }
            }

            if (changed)
            {
                _dirty = true;
                Changed?.Invoke();
            }
        }

        private string SelectedId(CosmeticKind kind) => kind == CosmeticKind.ContainerTheme ? _data.selectedContainerTheme : _data.selectedItemSkin;

        private CosmeticDefinition Find(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            foreach (var definition in _definitions)
            {
                if (definition.id == id)
                {
                    return definition;
                }
            }

            return null;
        }
    }
}
