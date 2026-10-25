namespace DesiMerge.Monetization
{
    /// <summary>Where a rewarded ad is offered. Each placement maps to a reward decided by the caller.</summary>
    public enum RewardedPlacement
    {
        ContinueAfterGameOver = 0,
        DoubleCoins = 1,
    }

    public enum RewardedAdResult
    {
        /// <summary>The ad was watched to completion; grant the reward.</summary>
        Completed = 0,
        /// <summary>The player closed the ad early; do not grant the reward.</summary>
        Skipped = 1,
        /// <summary>The ad failed to show.</summary>
        Failed = 2,
        /// <summary>No ad provider is available (e.g. offline, or no SDK integrated).</summary>
        NotAvailable = 3,
    }
}
