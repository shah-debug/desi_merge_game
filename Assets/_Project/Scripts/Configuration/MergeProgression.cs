using System.Collections.Generic;
using UnityEngine;

namespace DesiMerge.Configuration
{
    /// <summary>
    /// Ordered merge chain: two items at level N merge into level N + 1. The last entry is the final level.
    /// Levels are zero-based indices into <see cref="Levels"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "MergeProgression", menuName = "Desi Merge/Merge Progression")]
    public sealed class MergeProgression : ScriptableObject
    {
        [SerializeField] private List<MergeItemDefinition> levels = new List<MergeItemDefinition>();

        [Tooltip("How many of the lowest levels can appear as dropped items (e.g. 5 = first five levels).")]
        [SerializeField, Min(1)] private int spawnableLevelCount = 5;

        public IReadOnlyList<MergeItemDefinition> Levels => levels;
        public int Count => levels.Count;
        public int SpawnableLevelCount => spawnableLevelCount;
        public int FinalLevel => levels.Count - 1;

        public MergeItemDefinition Get(int level) => levels[level];

        public bool IsValidLevel(int level) => level >= 0 && level < levels.Count;

        /// <summary>Level of the item with this stable ID, or -1 if no current definition has it (e.g. removed content).</summary>
        public int IndexOf(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return -1;
            }

            for (var i = 0; i < levels.Count; i++)
            {
                if (levels[i] != null && levels[i].Id == id)
                {
                    return i;
                }
            }

            return -1;
        }

        public bool IsFinalLevel(int level) => level == FinalLevel;

        /// <summary>Returns false for the final level (it has no next item) or an invalid level.</summary>
        public bool TryGetNextLevel(int level, out int nextLevel)
        {
            nextLevel = level + 1;
            return IsValidLevel(level) && nextLevel < levels.Count;
        }

        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            if (levels.Count < 2)
            {
                errors.Add("Merge progression needs at least two levels.");
                return errors;
            }

            if (spawnableLevelCount < 1 || spawnableLevelCount >= levels.Count)
            {
                errors.Add($"Spawnable level count {spawnableLevelCount} must be between 1 and {levels.Count - 1}.");
            }

            var ids = new HashSet<string>();
            for (var i = 0; i < levels.Count; i++)
            {
                var definition = levels[i];
                if (definition == null)
                {
                    errors.Add($"Level {i} is missing its definition.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(definition.Id))
                {
                    errors.Add($"Level {i} ({definition.name}) has no id.");
                }
                else if (!ids.Add(definition.Id))
                {
                    errors.Add($"Level {i} reuses id '{definition.Id}'.");
                }

                if (definition.Sprite == null)
                {
                    errors.Add($"Level {i} ({definition.Id}) has no sprite.");
                }

                if (definition.Radius <= 0f)
                {
                    errors.Add($"Level {i} ({definition.Id}) radius must be positive.");
                }

                for (var j = 0; j < i; j++)
                {
                    if (levels[j] == definition)
                    {
                        errors.Add($"Level {i} repeats the definition used at level {j}.");
                    }
                }
            }

            return errors;
        }

        /// <summary>Used by editor tooling and tests to author progressions in code.</summary>
        internal void Set(IEnumerable<MergeItemDefinition> orderedLevels, int spawnableCount)
        {
            levels = new List<MergeItemDefinition>(orderedLevels);
            spawnableLevelCount = spawnableCount;
        }
    }
}
