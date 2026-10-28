using System.Collections.Generic;
using DesiMerge.PowerUps;
using DesiMerge.Progression;
using UnityEditor;
using UnityEngine;

namespace DesiMerge.Editor.Content
{
    /// <summary>
    /// Phase 5 content: the default power-up catalog (Hammer, Shuffle; stable IDs) and power-up charge rewards for the
    /// shipped daily challenges. Existing catalogs are kept; only shipped challenges that still have no reward get one.
    /// </summary>
    internal static class PowerUpContentBuilder
    {
        public const string HammerId = "hammer";
        public const string ShuffleId = "shuffle";

        /// <summary>Shipped daily challenge ID → (power-up, charges).</summary>
        private static readonly Dictionary<string, (string PowerUp, int Amount)> DailyRewards = new Dictionary<string, (string, int)>
        {
            { "daily_merges_20", (HammerId, 1) },
            { "daily_score_300", (ShuffleId, 1) },
            { "daily_create_aam", (HammerId, 1) },
            { "daily_games_3", (ShuffleId, 1) },
            { "daily_merges_40", (HammerId, 2) },
            { "daily_score_600", (ShuffleId, 2) },
            { "daily_create_chai", (HammerId, 2) },
        };

        public static void EnsurePowerUpCatalog()
        {
            ContentPaths.EnsureFolder(ContentPaths.ProgressionFolder);
            if (AssetDatabase.LoadAssetAtPath<PowerUpCatalog>(ContentPaths.PowerUpsPath) != null)
            {
                return;
            }

            var catalog = ScriptableObject.CreateInstance<PowerUpCatalog>();
            catalog.Set(new[]
            {
                new PowerUpDefinition { id = HammerId, title = "Hammer", kind = PowerUpKind.Hammer, freeUsesPerGame = 1, maxSavedCharges = 9 },
                new PowerUpDefinition { id = ShuffleId, title = "Shuffle", kind = PowerUpKind.Shuffle, freeUsesPerGame = 1, maxSavedCharges = 9 },
            });
            AssetDatabase.CreateAsset(catalog, ContentPaths.PowerUpsPath);
        }

        /// <summary>Gives shipped daily challenges their power-up reward if they have none (a designer's reward is kept).</summary>
        public static void EnsureDailyRewards()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<DailyChallengeCatalog>(ContentPaths.DailyChallengesPath);
            if (catalog == null)
            {
                return;
            }

            var changed = false;
            foreach (var challenge in catalog.All)
            {
                if (challenge != null && challenge.reward.type == RewardType.None && challenge.id != null &&
                    DailyRewards.TryGetValue(challenge.id, out var reward))
                {
                    challenge.reward = new RewardDefinition { type = RewardType.PowerUpCharges, amount = reward.Amount, powerUpId = reward.PowerUp };
                    changed = true;
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(catalog);
            }
        }
    }
}
