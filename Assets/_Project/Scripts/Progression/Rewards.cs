using System;

namespace DesiMerge.Progression
{
    /// <summary>
    /// Reward kinds. The game design does not define an economy yet, so shipped content uses <see cref="None"/>;
    /// <see cref="Coins"/> maps to the existing (Phase 1) coin balance and is available for designers to opt into.
    /// </summary>
    public enum RewardType
    {
        None = 0,
        Coins = 1,
    }

    [Serializable]
    public struct RewardDefinition
    {
        public RewardType type;
        public int amount;

        public bool IsSomething => type != RewardType.None && amount > 0;
    }

    /// <summary>Applies a reward. Callers guarantee each reward is granted once (tracked by ID in save data).</summary>
    public interface IRewardGranter
    {
        void Grant(RewardDefinition reward);
    }

    public sealed class ProgressRewardGranter : IRewardGranter
    {
        private readonly IPlayerProgressService _progress;

        public ProgressRewardGranter(IPlayerProgressService progress)
        {
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
        }

        public void Grant(RewardDefinition reward)
        {
            if (reward.IsSomething && reward.type == RewardType.Coins)
            {
                _progress.AddCoins(reward.amount);
            }
        }
    }
}
