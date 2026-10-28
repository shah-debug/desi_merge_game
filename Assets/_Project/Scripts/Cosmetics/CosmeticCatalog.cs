using System.Collections.Generic;
using UnityEngine;

namespace DesiMerge.Cosmetics
{
    [CreateAssetMenu(fileName = "Cosmetics", menuName = "Desi Merge/Cosmetic Catalog")]
    public sealed class CosmeticCatalog : ScriptableObject
    {
        [SerializeField] private List<CosmeticDefinition> cosmetics = new List<CosmeticDefinition>();

        public IReadOnlyList<CosmeticDefinition> Cosmetics => cosmetics;

        public static bool IsValid(CosmeticDefinition definition)
        {
            return definition != null && !string.IsNullOrWhiteSpace(definition.id) &&
                   (definition.unlock != CosmeticUnlock.Achievement || !string.IsNullOrWhiteSpace(definition.achievementId));
        }

        /// <summary>Returns human-readable problems; empty when usable.</summary>
        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            var ids = new HashSet<string>();
            foreach (var definition in cosmetics)
            {
                if (!IsValid(definition))
                {
                    errors.Add("A cosmetic needs an ID (and an achievement ID when unlocked by an achievement).");
                }
                else if (!ids.Add(definition.id))
                {
                    errors.Add($"Cosmetic ID '{definition.id}' is used more than once.");
                }
            }

            return errors;
        }

        internal void Set(IEnumerable<CosmeticDefinition> definitions)
        {
            cosmetics = new List<CosmeticDefinition>(definitions);
        }
    }
}
