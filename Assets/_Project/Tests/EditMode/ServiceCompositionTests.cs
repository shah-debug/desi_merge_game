using DesiMerge.Analytics;
using DesiMerge.Configuration;
using DesiMerge.Core;
using DesiMerge.Monetization;
using DesiMerge.Persistence;
using DesiMerge.Settings;
using NUnit.Framework;
using UnityEngine;

namespace DesiMerge.Tests.EditMode
{
    public class ServiceCompositionTests
    {
        private GameConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<GameConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void DefaultConfig_IsValid()
        {
            CollectionAssert.IsEmpty(_config.Validate());
        }

        [Test]
        public void Config_ReportsOutOfRangeValues()
        {
            _config.SetForTests(10, new SettingsData { musicVolume = 3f }, true, true);

            Assert.AreEqual(2, _config.Validate().Count);
        }

        [Test]
        public void DevelopmentBuild_UsesSimulatedAdsAndLogAnalytics()
        {
            var services = GameServicesFactory.Create(_config, new InMemorySaveStore(), isDevelopmentBuild: true);

            Assert.IsInstanceOf<SimulatedRewardedAdService>(services.RewardedAds);
            Assert.IsInstanceOf<LogAnalyticsService>(services.Analytics);
            Assert.AreEqual(GameState.Boot, services.GameState.Current);
        }

        [Test]
        public void ReleaseBuild_NeverUsesSimulatedAds()
        {
            var services = GameServicesFactory.Create(_config, new InMemorySaveStore(), isDevelopmentBuild: false);

            Assert.IsInstanceOf<UnavailableRewardedAdService>(services.RewardedAds);
            Assert.IsInstanceOf<NullAnalyticsService>(services.Analytics);
        }

        /// <summary>Phase 6: a config with no optional content (no items, catalogs, audio) still yields working, offline services.</summary>
        [Test]
        public void MissingOptionalContent_ServicesStillWork()
        {
            var store = new InMemorySaveStore();
            var services = GameServicesFactory.Create(_config, store, isDevelopmentBuild: false);

            Assert.AreEqual(0, services.PowerUps.Definitions.Count);
            Assert.AreEqual(0, services.Achievements.Definitions.Count);
            Assert.IsNull(services.DailyChallenges.Current);
            Assert.IsFalse(services.Ads.IsSupported, "Release builds without an SDK never offer ads.");
            Assert.IsFalse(services.Ads.TryShow(RewardedPlacement.ContinueAfterGameOver, _ => Assert.Fail("No callback expected.")));
            Assert.AreEqual(0, services.PowerUps.Available("hammer"));
            Assert.IsFalse(services.DailyChallenges.TryClaim());
            services.Progress.SubmitFinalScore(50);
            Assert.IsTrue(services.FlushAll());
            Assert.IsTrue(store.TryRead(SaveKey.PlayerProgress, out _));
            services.Dispose();
        }

        /// <summary>Phase 6: a failing disk never throws out of FlushAll; changes are kept and written once it works again.</summary>
        [Test]
        public void FlushAll_WriteFailureKeepsChangesForTheNextFlush()
        {
            var store = new InMemorySaveStore();
            var services = GameServicesFactory.Create(_config, store, isDevelopmentBuild: false);
            services.Progress.SubmitFinalScore(70);

            store.FailWrites = true;
            Assert.IsFalse(services.FlushAll());
            Assert.IsFalse(store.TryRead(SaveKey.PlayerProgress, out _));

            store.FailWrites = false;
            Assert.IsTrue(services.FlushAll());
            var reopened = GameServicesFactory.Create(_config, store, isDevelopmentBuild: false);
            Assert.AreEqual(70, reopened.Progress.HighScore);
            services.Dispose();
            reopened.Dispose();
        }

        [Test]
        public void DevelopmentBuild_RespectsConfigToggles()
        {
            _config.SetForTests(60, new SettingsData(), simulateAds: false, logAnalytics: false);

            var services = GameServicesFactory.Create(_config, new InMemorySaveStore(), isDevelopmentBuild: true);

            Assert.IsInstanceOf<UnavailableRewardedAdService>(services.RewardedAds);
            Assert.IsInstanceOf<NullAnalyticsService>(services.Analytics);
        }

        [Test]
        public void FlushAll_PersistsProgressAndSettings()
        {
            var store = new InMemorySaveStore();
            var services = GameServicesFactory.Create(_config, store, isDevelopmentBuild: false);
            services.Progress.AddCoins(3);
            services.Settings.MusicVolume = 0.3f;

            services.FlushAll();

            Assert.IsTrue(store.TryRead(SaveKey.PlayerProgress, out _));
            Assert.IsTrue(store.TryRead(SaveKey.Settings, out _));
        }

        [Test]
        public void UnavailableAds_ReportNotAvailable()
        {
            var ads = new UnavailableRewardedAdService();
            RewardedAdResult? result = null;

            ads.Show(RewardedPlacement.ContinueAfterGameOver, r => result = r);

            Assert.IsFalse(ads.IsReady(RewardedPlacement.ContinueAfterGameOver));
            Assert.AreEqual(RewardedAdResult.NotAvailable, result);
        }

        [TestCase(RewardedAdResult.Completed)]
        [TestCase(RewardedAdResult.Skipped)]
        [TestCase(RewardedAdResult.Failed)]
        public void SimulatedAds_InvokeCallbackOnceWithConfiguredResult(RewardedAdResult expected)
        {
            var ads = new SimulatedRewardedAdService { NextResult = expected };
            var calls = 0;
            RewardedAdResult? result = null;

            ads.Show(RewardedPlacement.DoubleCoins, r =>
            {
                calls++;
                result = r;
            });

            Assert.AreEqual(1, calls);
            Assert.AreEqual(expected, result);
        }
    }
}
