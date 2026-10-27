using System.Collections.Generic;
using DesiMerge.Configuration;
using UnityEngine;

namespace DesiMerge.Progression
{
    /// <summary>
    /// The pool of daily challenges. Today's challenge is the entry at (UTC day number mod count): deterministic,
    /// offline, the same for every player on the same day, and stable across app restarts.
    /// </summary>
    [CreateAssetMenu(fileName = "DailyChallenges", menuName = "Desi Merge/Daily Challenge Catalog")]
    public sealed class DailyChallengeCatalog : ScriptableObject
    {
        [SerializeField] private List<DailyChallengeDefinition> challenges = new List<DailyChallengeDefinition>();

        public IReadOnlyList<DailyChallengeDefinition> All => challenges;

        public static bool IsValid(DailyChallengeDefinition challenge, MergeProgression content)
        {
            if (challenge == null || string.IsNullOrWhiteSpace(challenge.id))
            {
                return false;
            }

            return challenge.type == DailyChallengeType.CreateItem
                ? content == null || content.IndexOf(challenge.itemId) >= 0
                : challenge.target > 0;
        }

        public IReadOnlyList<string> Validate(MergeProgression content)
        {
            var errors = new List<string>();
            var ids = new HashSet<string>();
            foreach (var challenge in challenges)
            {
                if (!IsValid(challenge, content))
                {
                    errors.Add($"Daily challenge '{challenge?.id}' is invalid (missing id, target or unknown item).");
                }
                else if (!ids.Add(challenge.id))
                {
                    errors.Add($"Daily challenge id '{challenge.id}' is used twice.");
                }
            }

            return errors;
        }

        internal void Set(IEnumerable<DailyChallengeDefinition> definitions)
        {
            challenges = new List<DailyChallengeDefinition>(definitions);
        }
    }
}
