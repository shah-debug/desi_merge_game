using System;
using DesiMerge.Analytics;
using DesiMerge.Audio;
using DesiMerge.Monetization;
using DesiMerge.Progression;
using DesiMerge.Settings;

namespace DesiMerge.Core
{
    /// <summary>
    /// The set of application-lifetime services, built once by <see cref="GameBootstrapper"/>.
    /// Passed explicitly to scene entry points; there is intentionally no static accessor.
    /// </summary>
    public sealed class GameServices
    {
        public GameServices(
            IGameStateService gameState,
            IPlayerProgressService progress,
            ISettingsService settings,
            IAudioService audio,
            IAnalyticsService analytics,
            IRewardedAdService rewardedAds)
        {
            GameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Audio = audio ?? throw new ArgumentNullException(nameof(audio));
            Analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
            RewardedAds = rewardedAds ?? throw new ArgumentNullException(nameof(rewardedAds));
        }

        public IGameStateService GameState { get; }
        public IPlayerProgressService Progress { get; }
        public ISettingsService Settings { get; }
        public IAudioService Audio { get; }
        public IAnalyticsService Analytics { get; }
        public IRewardedAdService RewardedAds { get; }

        /// <summary>Persists all pending player data. Called on app pause/quit.</summary>
        public void FlushAll()
        {
            Progress.Flush();
            Settings.Flush();
        }
    }
}
