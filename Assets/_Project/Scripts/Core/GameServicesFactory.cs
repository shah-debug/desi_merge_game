using DesiMerge.Analytics;
using DesiMerge.Audio;
using DesiMerge.Configuration;
using DesiMerge.Monetization;
using DesiMerge.Persistence;
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
        public static GameServices Create(GameConfig config, ISaveStore saveStore, bool isDevelopmentBuild, GameObject audioHost = null)
        {
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

            return new GameServices(
                new GameStateMachine(),
                new PlayerProgressService(saveStore),
                settings,
                audio,
                analytics,
                rewardedAds,
                new SceneNavigator(config.MainMenuSceneName, config.GameplaySceneName));
        }
    }
}
