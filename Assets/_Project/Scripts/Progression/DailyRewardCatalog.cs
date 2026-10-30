using System;
using System.Collections.Generic;
using UnityEngine;

namespace DesiMerge.Progression
{
    [Serializable]
    public sealed class DailyRewardDay
    {
        public string title;
        public RewardDefinition[] rewards = Array.Empty<RewardDefinition>();
        public bool special;
    }

    [CreateAssetMenu(fileName = "DailyRewards", menuName = "Desi Merge/Daily Reward Calendar")]
    public sealed class DailyRewardCatalog : ScriptableObject
    {
        [SerializeField] private DailyRewardDay[] days = Array.Empty<DailyRewardDay>();

        public IReadOnlyList<DailyRewardDay> Days => days;

        public static bool IsValid(DailyRewardDay day)
        {
            if (day?.rewards == null || day.rewards.Length == 0)
            {
                return false;
            }

            var seen = new HashSet<string>();
            foreach (var reward in day.rewards)
            {
                if (!reward.IsSomething || !seen.Add(reward.type + ":" + reward.powerUpId))
                {
                    return false;
                }
            }

            return true;
        }

        internal void Set(DailyRewardDay[] calendar) => days = calendar;
    }
}
