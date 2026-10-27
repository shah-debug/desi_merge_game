using System;
using DesiMerge.Analytics;
using DesiMerge.Audio;
using DesiMerge.Configuration;
using DesiMerge.Monetization;
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

        /// <summary>Item definitions the progression services were built with (collection, stats). May be null in tests.</summary>
        public MergeProgression ItemContent { get; }

        /// <summary>
        /// Persists all pending player data. Called at game over, when leaving gameplay, and on app pause/focus loss/quit.
        /// Each service writes only if it has changes; a failed write keeps its changes for the next flush.
        /// </summary>
        public bool FlushAll()
        {
            var ok = Progress.Flush();
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
        }
    }
}
