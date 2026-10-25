using DesiMerge.Analytics;
using DesiMerge.Audio;
using DesiMerge.Configuration;
using DesiMerge.Monetization;
using DesiMerge.Persistence;
using DesiMerge.Progression;
using DesiMerge.Settings;

namespace DesiMerge.Core
{
    /// <summary>Chooses concrete implementations. The only place that knows which implementation backs each interface.</summary>
    public static class GameServicesFactory
    {
        public static GameServices Create(GameConfig config, ISaveStore saveStore, bool isDevelopmentBuild)
        {
            IAnalyticsService analytics = isDevelopmentBuild && config.LogAnalyticsEvents
                ? new LogAnalyticsService()
                : new NullAnalyticsService();

            IRewardedAdService rewardedAds = isDevelopmentBuild && config.SimulateRewardedAds
                ? new SimulatedRewardedAdService()
                : new UnavailableRewardedAdService();

            return new GameServices(
                new GameStateMachine(),
                new PlayerProgressService(saveStore),
                new SettingsService(saveStore, config.DefaultSettings),
                new SilentAudioService(),
                analytics,
                rewardedAds);
        }
    }
}
