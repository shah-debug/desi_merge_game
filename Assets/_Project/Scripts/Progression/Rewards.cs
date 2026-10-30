using System;
using System.Collections.Generic;
using DesiMerge.PowerUps;
using UnityEngine;

namespace DesiMerge.Progression
{
    /// <summary>
    /// Reward kinds. The game design has no currency economy: <see cref="Coins"/> maps to the dormant Phase 1 coin
    /// balance (unused by shipped content). <see cref="PowerUpCharges"/> adds saved power-up charges.
    /// </summary>
    public enum RewardType
    {
        None = 0,
        Coins = 1,
        PowerUpCharges = 2,
    }

    [Serializable]
    public struct RewardDefinition
    {
        public RewardType type;
        public int amount;
        [Tooltip("Power-up ID for PowerUpCharges.")]
        public string powerUpId;

        public bool IsSomething => type != RewardType.None && amount > 0;
    }

    /// <summary>Applies rewards. Each reward carries a stable grant ID so it is applied at most once.</summary>
    public interface IRewardGranter
    {
        /// <summary>
        /// Applies the reward for <paramref name="grantId"/>. Returns true if it is applied (now or earlier) and false
        /// if it could not be applied; the caller must then not record the reward as claimed.
        /// </summary>
        bool Grant(string grantId, RewardDefinition reward);

        /// <summary>True if the reward was already applied or can be applied now (e.g. the power-up storage has room).</summary>
        bool CanGrant(string grantId, RewardDefinition reward);
    }

    public enum ClaimResult
    {
        Claimed = 0,
        NotAvailable = 1,
        StorageFull = 2,
        Failed = 3,
    }

    public static class RewardClaims
    {
        /// <summary>
        /// Applies every reward under "<paramref name="grantPrefix"/>:index". Nothing is applied unless all of them fit.
        /// Grants are idempotent, so retrying after an interrupted claim never applies a reward twice.
        /// </summary>
        public static ClaimResult GrantAll(IRewardGranter granter, string grantPrefix, IReadOnlyList<RewardDefinition> rewards)
        {
            if (granter == null)
            {
                return ClaimResult.Failed;
            }

            var count = rewards?.Count ?? 0;
            for (var i = 0; i < count; i++)
            {
                if (!granter.CanGrant(GrantId(grantPrefix, i), rewards[i]))
                {
                    return ClaimResult.StorageFull;
                }
            }

            for (var i = 0; i < count; i++)
            {
                if (!granter.Grant(GrantId(grantPrefix, i), rewards[i]))
                {
                    return ClaimResult.Failed;
                }
            }

            return ClaimResult.Claimed;
        }

        public static string GrantId(string prefix, int index) => prefix + ":" + index;
    }

    public sealed class ProgressRewardGranter : IRewardGranter
    {
        private readonly IPlayerProgressService _progress;
        private readonly IPowerUpService _powerUps;

        public ProgressRewardGranter(IPlayerProgressService progress, IPowerUpService powerUps = null)
        {
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _powerUps = powerUps;
        }

        public bool Grant(string grantId, RewardDefinition reward)
        {
            if (!reward.IsSomething)
            {
                return true;
            }

            switch (reward.type)
            {
                case RewardType.Coins:
                    _progress.AddCoins(reward.amount);
                    return true;
                case RewardType.PowerUpCharges:
                    return _powerUps != null && _powerUps.TryGrant(grantId, reward.powerUpId, reward.amount);
                default:
                    return false;
            }
        }

        public bool CanGrant(string grantId, RewardDefinition reward)
        {
            if (!reward.IsSomething || reward.type == RewardType.Coins)
            {
                return true;
            }

            return reward.type == RewardType.PowerUpCharges && _powerUps != null &&
                   (_powerUps.HasApplied(grantId) || _powerUps.CanGrant(reward.powerUpId, reward.amount));
        }
    }
}
