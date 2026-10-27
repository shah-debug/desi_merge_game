using System;
using System.Collections.Generic;
using DesiMerge.Configuration;
using DesiMerge.Persistence;
using UnityEngine;

namespace DesiMerge.Progression
{
    [CreateAssetMenu(fileName = "Achievements", menuName = "Desi Merge/Achievement Catalog")]
    public sealed class AchievementCatalog : ScriptableObject
    {
        [SerializeField] private List<AchievementDefinition> achievements = new List<AchievementDefinition>();

        public IReadOnlyList<AchievementDefinition> All => achievements;

        public IReadOnlyList<string> Validate(MergeProgression content)
        {
            var errors = new List<string>();
            var ids = new HashSet<string>();
            foreach (var achievement in achievements)
            {
                if (achievement == null || string.IsNullOrWhiteSpace(achievement.id))
                {
                    errors.Add("An achievement has no id.");
                    continue;
                }

                if (!ids.Add(achievement.id))
                {
                    errors.Add($"Achievement id '{achievement.id}' is used twice.");
                }

                if (achievement.type == AchievementType.DiscoverItem)
                {
                    if (content != null && content.IndexOf(achievement.itemId) < 0)
                    {
                        errors.Add($"Achievement '{achievement.id}' refers to unknown item '{achievement.itemId}'.");
                    }
                }
                else if (achievement.target <= 0)
                {
                    errors.Add($"Achievement '{achievement.id}' needs a positive target.");
                }
            }

            return errors;
        }

        internal void Set(IEnumerable<AchievementDefinition> definitions)
        {
            achievements = new List<AchievementDefinition>(definitions);
        }
    }
}
