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
