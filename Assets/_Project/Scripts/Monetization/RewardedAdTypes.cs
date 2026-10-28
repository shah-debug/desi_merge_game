namespace DesiMerge.Monetization
{
    /// <summary>Where a rewarded ad is offered. Each placement maps to a reward decided by the caller.</summary>
    public enum RewardedPlacement
    {
        ContinueAfterGameOver = 0,
        /// <summary>Reserved (Phase 1). Not offered: the game has no coin economy.</summary>
        DoubleCoins = 1,
    }

    /// <summary>
    /// How a rewarded-ad request ended. Only <see cref="Completed"/> (the provider's verified reward callback)
    /// may grant a reward; every other value grants nothing. Numeric values are stable (analytics).
    /// </summary>
    public enum RewardedAdResult
    {
        /// <summary>The provider confirmed the reward (ad watched to completion). Grant the reward.</summary>
        Completed = 0,
        /// <summary>The player closed or dismissed the ad before the reward was earned.</summary>
        Skipped = 1,
        /// <summary>The ad loaded but failed to show.</summary>
        Failed = 2,
        /// <summary>No ad provider is available (no SDK integrated, or ads disabled).</summary>
        NotAvailable = 3,
        /// <summary>The network had no ad to serve.</summary>
        NoFill = 4,
        /// <summary>The ad failed to load.</summary>
        LoadFailed = 5,
        /// <summary>No network connection.</summary>
        NetworkUnavailable = 6,
    }
}
