using DesiMerge.Analytics;
using DesiMerge.Audio;
using DesiMerge.Configuration;
using DesiMerge.Cosmetics;
using DesiMerge.Monetization;
using DesiMerge.Persistence;
using DesiMerge.PowerUps;
using DesiMerge.Progression;
using DesiMerge.Settings;
using UnityEngine;

namespace DesiMerge.Core
{
    /// <summary>Chooses concrete implementations. The only place that knows which implementation backs each interface.</summary>
    public static class GameServicesFactory
    {
        /// <param name="audioHost">
        /// GameObject that will own the AudioSources. Null (or no AudioLibrary in the config) gives silent audio.
        /// </param>
        /// <param name="clock">Time source for daily challenges and streaks; defaults to the system clock.</param>
        public static GameServices Create(GameConfig config, ISaveStore saveStore, bool isDevelopmentBuild, GameObject audioHost = null, IClock clock = null)
        {
            clock = clock ?? new SystemClock();

            IAnalyticsService analytics = isDevelopmentBuild && config.LogAnalyticsEvents
                ? new LogAnalyticsService()
                : new NullAnalyticsService();

            IRewardedAdService rewardedAds = isDevelopmentBuild && config.SimulateRewardedAds
                ? new SimulatedRewardedAdService()
                : new UnavailableRewardedAdService();

            var settings = new SettingsService(saveStore, config.DefaultSettings);
            IAudioService audio = audioHost != null && config.AudioLibrary != null
                ? new AudioService(audioHost, config.AudioLibrary, settings)
                : new SilentAudioService();

            var content = config.ItemProgression;
            var progress = new PlayerProgressService(saveStore, content, clock);
            var powerUps = new PowerUpService(saveStore, config.PowerUps);
            var rewards = new ProgressRewardGranter(progress, powerUps);
            var achievements = new AchievementService(saveStore, config.Achievements, progress, content, rewards);
            var daily = new DailyChallengeService(saveStore, config.DailyChallenges, progress, content, clock, rewards);

            return new GameServices(
                new GameStateMachine(),
                progress,
                settings,
                audio,
                analytics,
                rewardedAds,
                new SceneNavigator(config.MainMenuSceneName, config.GameplaySceneName),
                achievements,
                daily,
                clock,
                powerUps,
                new CosmeticService(saveStore, config.Cosmetics, achievements),
                content,
                new DailyRewardService(saveStore, config.DailyRewards, clock, rewards),
                new MissionService(saveStore, config.Missions, progress, powerUps, content, clock, rewards),
                new MilestoneService(saveStore, config.Milestones, progress, rewards));
        }
    }
}
