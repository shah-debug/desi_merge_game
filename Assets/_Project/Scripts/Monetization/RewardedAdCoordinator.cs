using System;
using System.Collections.Generic;
using DesiMerge.Analytics;
using UnityEngine;

namespace DesiMerge.Monetization
{
    /// <summary>
    /// Central rewarded-ad lifecycle, shared by every placement (one per app, owned by GameServices).
    ///  - One request at a time: a second request while one is in flight is refused.
    ///  - Each request finishes exactly once: the first provider callback wins; repeats and callbacks for old
    ///    requests are ignored (and counted), so a reward can never be granted twice.
    ///  - A provider that throws is treated as <see cref="RewardedAdResult.Failed"/>.
    ///  - App pause/resume during an ad is normal (full-screen ads pause the app); the request simply stays in
    ///    flight until the provider calls back. If the app is killed meanwhile, nothing is granted.
    /// Callers grant their reward only when the result is <see cref="RewardedAdResult.Completed"/>.
    /// </summary>
    public sealed class RewardedAdCoordinator
    {
        private readonly IRewardedAdService _provider;
        private readonly IAnalyticsService _analytics;
        private int _lastRequestId;
        private int _activeRequestId;
        private Action<RewardedAdResult> _activeCallback;
        private RewardedPlacement _activePlacement;

        public RewardedAdCoordinator(IRewardedAdService provider, IAnalyticsService analytics)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
        }

        public bool IsSupported => _provider.IsSupported;

        /// <summary>A request is waiting for the provider's callback.</summary>
        public bool IsBusy => _activeRequestId != 0;

        /// <summary>Provider callbacks that were ignored (duplicates or stale). Diagnostics and tests.</summary>
        public int IgnoredCallbacks { get; private set; }

        public bool IsReady(RewardedPlacement placement)
        {
            if (IsBusy || !_provider.IsSupported)
            {
                return false;
            }

            try
            {
                return _provider.IsReady(placement);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Ads] Availability check failed: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// Requests an ad. Returns false (and never calls <paramref name="onFinished"/>) if another request is in
        /// flight or no ad is ready. Otherwise <paramref name="onFinished"/> is called exactly once, possibly
        /// synchronously.
        /// </summary>
        public bool TryShow(RewardedPlacement placement, Action<RewardedAdResult> onFinished)
        {
            if (onFinished == null)
            {
                throw new ArgumentNullException(nameof(onFinished));
            }

            if (!IsReady(placement))
            {
                return false;
            }

            var requestId = ++_lastRequestId;
            _activeRequestId = requestId;
            _activeCallback = onFinished;
            _activePlacement = placement;
            _analytics.TrackEvent(AnalyticsEvents.RewardedAdRequested, new Dictionary<string, object> { { "placement", placement.ToString() } });

            try
            {
                _provider.Show(placement, result => Finish(requestId, result));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Ads] Showing the ad failed: {exception.Message}");
                Finish(requestId, RewardedAdResult.Failed);
            }

            return true;
        }

        /// <summary>
        /// Stops waiting for the current request (e.g. the screen that asked for it is closing). Its callback
        /// still arrives once with <see cref="RewardedAdResult.Failed"/>; a late provider callback is ignored.
        /// </summary>
        public void Abandon()
        {
            if (IsBusy)
            {
                Finish(_activeRequestId, RewardedAdResult.Failed);
            }
        }

        private void Finish(int requestId, RewardedAdResult result)
        {
            if (requestId != _activeRequestId)
            {
                IgnoredCallbacks++;
                Debug.LogWarning($"[Ads] Ignored a duplicate or late ad callback ({result}).");
                return;
            }

            var callback = _activeCallback;
            _activeRequestId = 0;
            _activeCallback = null;
            _analytics.TrackEvent(AnalyticsEvents.RewardedAdResult, new Dictionary<string, object>
            {
                { "placement", _activePlacement.ToString() },
                { "result", result.ToString() },
            });
            callback(result);
        }
    }
}
