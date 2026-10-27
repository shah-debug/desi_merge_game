using System.Collections.Generic;
using DesiMerge.Audio;
using DesiMerge.Progression;
using DesiMerge.Settings;
using UnityEngine;

namespace DesiMerge.Configuration
{
    /// <summary>
    /// Designer-tunable, read-only-at-runtime application configuration.
    /// One asset is referenced by the bootstrapper in the Bootstrap scene.
    /// Gameplay tuning (merge items, physics) will live in separate assets.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Desi Merge/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [Tooltip("Android defaults to 30 FPS unless this is set.")]
        [SerializeField, Range(30, 120)] private int targetFrameRate = 60;

        [Tooltip("Scene loaded after bootstrapping. Leave empty to stay in the Bootstrap scene.")]
        [SerializeField] private string firstSceneName = string.Empty;

        [Tooltip("Scene names used by the scene navigator (must be in Build Settings).")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string gameplaySceneName = "Gameplay";

        [Tooltip("Used when the player has no saved settings yet.")]
        [SerializeField] private SettingsData defaultSettings = new SettingsData();

        [Tooltip("Clips for music and sound effects. Empty = silent audio service.")]
        [SerializeField] private AudioLibrary audioLibrary;

        [Header("Progression content")]
        [Tooltip("Item definitions used for the collection and to rank discoveries (same asset the gameplay uses).")]
        [SerializeField] private MergeProgression itemProgression;
        [SerializeField] private AchievementCatalog achievements;
        [SerializeField] private DailyChallengeCatalog dailyChallenges;

        [Header("Development builds only")]
        [SerializeField] private bool simulateRewardedAds = true;
        [SerializeField] private bool logAnalyticsEvents = true;

        public int TargetFrameRate => targetFrameRate;
        public string FirstSceneName => firstSceneName;
        public string MainMenuSceneName => mainMenuSceneName;
        public string GameplaySceneName => gameplaySceneName;
        public AudioLibrary AudioLibrary => audioLibrary;
        public MergeProgression ItemProgression => itemProgression;
        public AchievementCatalog Achievements => achievements;
        public DailyChallengeCatalog DailyChallenges => dailyChallenges;
        public SettingsData DefaultSettings => defaultSettings;
        public bool SimulateRewardedAds => simulateRewardedAds;
        public bool LogAnalyticsEvents => logAnalyticsEvents;

        /// <summary>Returns human-readable problems; empty when the config is usable.</summary>
        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            if (targetFrameRate < 30 || targetFrameRate > 120)
            {
                errors.Add($"Target frame rate {targetFrameRate} is outside 30-120.");
            }

            if (defaultSettings == null)
            {
                errors.Add("Default settings are missing.");
            }
            else
            {
                if (defaultSettings.musicVolume < 0f || defaultSettings.musicVolume > 1f)
                {
                    errors.Add("Default music volume must be within 0-1.");
                }

                if (defaultSettings.sfxVolume < 0f || defaultSettings.sfxVolume > 1f)
                {
                    errors.Add("Default SFX volume must be within 0-1.");
                }
            }

            return errors;
        }

        /// <summary>Test hook for building configs in code.</summary>
        internal void SetForTests(int frameRate, SettingsData settings, bool simulateAds, bool logAnalytics)
        {
            targetFrameRate = frameRate;
            defaultSettings = settings;
            simulateRewardedAds = simulateAds;
            logAnalyticsEvents = logAnalytics;
        }
    }
}
