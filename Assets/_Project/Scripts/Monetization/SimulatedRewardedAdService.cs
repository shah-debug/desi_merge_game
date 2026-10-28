using System;

namespace DesiMerge.Monetization
{
    /// <summary>
    /// Editor/development stand-in that completes immediately with a configurable result,
    /// so reward flows can be built and tested without an ad SDK. It never shows a real ad.
    /// </summary>
    public sealed class SimulatedRewardedAdService : IRewardedAdService
    {
        public RewardedAdResult NextResult { get; set; } = RewardedAdResult.Completed;

        public bool IsSupported => true;

        public bool IsReady(RewardedPlacement placement) => NextResult != RewardedAdResult.NotAvailable;

        public void Show(RewardedPlacement placement, Action<RewardedAdResult> onComplete)
        {
            if (onComplete == null)
            {
                throw new ArgumentNullException(nameof(onComplete));
            }

            onComplete(NextResult);
        }
    }
}
