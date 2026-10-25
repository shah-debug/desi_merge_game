using System;

namespace DesiMerge.Monetization
{
    /// <summary>
    /// Provider-agnostic rewarded ads. Gameplay must stay fully playable when ads are unavailable.
    /// The callback is always invoked exactly once per <see cref="Show"/> call.
    /// </summary>
    public interface IRewardedAdService
    {
        bool IsReady(RewardedPlacement placement);

        void Show(RewardedPlacement placement, Action<RewardedAdResult> onComplete);
    }
}
