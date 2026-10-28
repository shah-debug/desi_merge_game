using System;

namespace DesiMerge.Monetization
{
    /// <summary>Default for release builds until an ad SDK is integrated: unsupported, never ready, always reports NotAvailable.</summary>
    public sealed class UnavailableRewardedAdService : IRewardedAdService
    {
        public bool IsSupported => false;

        public bool IsReady(RewardedPlacement placement) => false;

        public void Show(RewardedPlacement placement, Action<RewardedAdResult> onComplete)
        {
            if (onComplete == null)
            {
                throw new ArgumentNullException(nameof(onComplete));
            }

            onComplete(RewardedAdResult.NotAvailable);
        }
    }
}
