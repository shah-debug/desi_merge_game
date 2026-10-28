using System;

namespace DesiMerge.Monetization
{
    /// <summary>
    /// Provider boundary for rewarded ads: the only place an ad SDK would be called. Gameplay must stay fully playable
    /// when ads are unavailable. Gameplay and UI never call a provider directly; they go through
    /// <see cref="RewardedAdCoordinator"/>, which allows one request at a time and ignores duplicate callbacks.
    /// Providers report <see cref="RewardedAdResult.Completed"/> only from the SDK's verified reward callback,
    /// never from "ad closed" or "ad shown".
    /// </summary>
    public interface IRewardedAdService
    {
        /// <summary>False when this build has no ad provider at all (ad offers are hidden instead of shown as unavailable).</summary>
        bool IsSupported { get; }

        bool IsReady(RewardedPlacement placement);

        /// <summary>Shows an ad. <paramref name="onComplete"/> should be invoked once; the coordinator tolerates repeats.</summary>
        void Show(RewardedPlacement placement, Action<RewardedAdResult> onComplete);
    }
}
