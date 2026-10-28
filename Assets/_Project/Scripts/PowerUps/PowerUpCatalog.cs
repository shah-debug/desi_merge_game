using System.Collections.Generic;
using UnityEngine;

namespace DesiMerge.PowerUps
{
    /// <summary>Data-driven power-up list. Order is display order. Invalid entries are skipped at runtime.</summary>
    [CreateAssetMenu(fileName = "PowerUps", menuName = "Desi Merge/Power-Up Catalog")]
    public sealed class PowerUpCatalog : ScriptableObject
    {
        [SerializeField] private List<PowerUpDefinition> powerUps = new List<PowerUpDefinition>();

        public IReadOnlyList<PowerUpDefinition> PowerUps => powerUps;

        public static bool IsValid(PowerUpDefinition definition)
        {
            return definition != null && !string.IsNullOrWhiteSpace(definition.id) && definition.freeUsesPerGame >= 0 && definition.maxSavedCharges >= 0;
        }

        /// <summary>Returns human-readable problems; empty when usable.</summary>
        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            var ids = new HashSet<string>();
            var kinds = new HashSet<PowerUpKind>();
            for (var i = 0; i < powerUps.Count; i++)
            {
                var definition = powerUps[i];
                if (!IsValid(definition))
                {
                    errors.Add($"Power-up {i} needs an ID and non-negative limits.");
                    continue;
                }

                if (!ids.Add(definition.id))
                {
                    errors.Add($"Power-up ID '{definition.id}' is used more than once.");
                }

                if (!kinds.Add(definition.kind))
                {
                    errors.Add($"More than one power-up has kind {definition.kind}.");
                }
            }

            return errors;
        }

        internal void Set(IEnumerable<PowerUpDefinition> definitions)
        {
            powerUps = new List<PowerUpDefinition>(definitions);
        }
    }
}
