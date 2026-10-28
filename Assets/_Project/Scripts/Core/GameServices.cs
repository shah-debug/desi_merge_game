using System;
using DesiMerge.Analytics;
using DesiMerge.Audio;
using DesiMerge.Configuration;
using DesiMerge.Cosmetics;
using DesiMerge.Monetization;
using DesiMerge.PowerUps;
using DesiMerge.Progression;
using DesiMerge.Settings;

namespace DesiMerge.Core
{
    /// <summary>
    /// The set of application-lifetime services, built once by <see cref="GameBootstrapper"/>.
    /// Passed explicitly to scene entry points; there is intentionally no static accessor.
    /// </summary>
    public sealed class GameServices : IDisposable
    {
        public GameServices(
            IGameStateService gameState,
            IPlayerProgressService progress,
            ISettingsService settings,
            IAudioService audio,
            IAnalyticsService analytics,
            IRewardedAdService rewardedAds,
            ISceneNavigator navigator,
            IAchievementService achievements,
            IDailyChallengeService dailyChallenges,
            IClock clock,
            IPowerUpService powerUps,
            ICosmeticService cosmetics,
            MergeProgression itemContent = null)
        {
            ItemContent = itemContent;
            GameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Audio = audio ?? throw new ArgumentNullException(nameof(audio));
            Analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
            RewardedAds = rewardedAds ?? throw new ArgumentNullException(nameof(rewardedAds));
            Navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
            Achievements = achievements ?? throw new ArgumentNullException(nameof(achievements));
            DailyChallenges = dailyChallenges ?? throw new ArgumentNullException(nameof(dailyChallenges));
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            PowerUps = powerUps ?? throw new ArgumentNullException(nameof(powerUps));
            Cosmetics = cosmetics ?? throw new ArgumentNullException(nameof(cosmetics));
            // The only way gameplay and UI reach the ad provider: one request at a time, duplicate callbacks ignored.
            Ads = new RewardedAdCoordinator(RewardedAds, Analytics);
        }

        public IGameStateService GameState { get; }
        public IPlayerProgressService Progress { get; }
        public ISettingsService Settings { get; }
        public IAudioService Audio { get; }
        public IAnalyticsService Analytics { get; }
        public IRewardedAdService RewardedAds { get; }
        public ISceneNavigator Navigator { get; }
        public IAchievementService Achievements { get; }
        public IDailyChallengeService DailyChallenges { get; }
        public IClock Clock { get; }
        public IPowerUpService PowerUps { get; }
        public ICosmeticService Cosmetics { get; }

        /// <summary>Rewarded-ad lifecycle over <see cref="RewardedAds"/> (the provider). Use this, never the provider.</summary>
        public RewardedAdCoordinator Ads { get; }

        /// <summary>Item definitions the progression services were built with (collection, stats). May be null in tests.</summary>
        public MergeProgression ItemContent { get; }

        /// <summary>
        /// Persists all pending player data. Called at game over, when leaving gameplay, and on app pause/focus loss/quit.
        /// Each service writes only if it has changes; a failed write keeps its changes for the next flush.
        /// </summary>
        public bool FlushAll()
        {
            var ok = Progress.Flush();
            // Power-ups before the files that record claims, so a granted reward is never lost (see PowerUpService).
            ok &= PowerUps.Flush();
            ok &= Cosmetics.Flush();
            ok &= Achievements.Flush();
            ok &= DailyChallenges.Flush();
            Settings.Flush();
            return ok;
        }

        /// <summary>Releases services that hold subscriptions (audio → settings, achievements/daily → progress).</summary>
        public void Dispose()
        {
            (Audio as IDisposable)?.Dispose();
            (Achievements as IDisposable)?.Dispose();
            (DailyChallenges as IDisposable)?.Dispose();
            (Cosmetics as IDisposable)?.Dispose();
            Ads.Abandon();
        }
    }
}
