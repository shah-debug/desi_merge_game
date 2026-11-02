using System;
using System.Linq;
using DesiMerge.Configuration;
using DesiMerge.Core;
using DesiMerge.Persistence;
using DesiMerge.PowerUps;
using DesiMerge.Progression;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DesiMerge.Tests.EditMode
{
    /// <summary>Diamonds = the saved coin balance: one-time grants, daily-reward payout, shipped prices and calendar.</summary>
    public class DiamondEconomyTests
    {
        private TestContent _content;
        private MergeProgression _items;
        private ManualClock _clock;

        [SetUp]
        public void SetUp()
        {
            _content = new TestContent();
            _items = _content.Progression(levels: 4, spawnable: 2);
            _clock = new ManualClock(new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc));
        }

        [TearDown]
        public void TearDown() => _content.DestroyAll();

        [Test]
        public void DiamondGrant_IsPaidOnce_SavedImmediately_AndSurvivesRestart()
        {
            var store = new InMemorySaveStore();
            var progress = new PlayerProgressService(store, _items, _clock);
            Assert.IsTrue(progress.TryGrantCoins("daily:1", 25));
            Assert.IsTrue(progress.TryGrantCoins("daily:1", 25), "A repeat is accepted...");
            Assert.AreEqual(25, progress.Coins, "...but never paid twice.");
            Assert.IsFalse(progress.TryGrantCoins("bad", 0));

            var relaunched = new PlayerProgressService(store, _items, _clock);
            Assert.AreEqual(25, relaunched.Coins, "Saved at once, without waiting for a flush.");
            Assert.IsTrue(relaunched.HasAppliedCoinGrant("daily:1"));
            Assert.IsTrue(relaunched.TryGrantCoins("daily:1", 25));
            Assert.AreEqual(25, relaunched.Coins);
        }

        [Test]
        public void Spending_NeverGoesNegative()
        {
            var progress = new PlayerProgressService(new InMemorySaveStore(), _items, _clock);
            progress.TryGrantCoins("g", 25);
            Assert.IsFalse(progress.TrySpendCoins(30));
            Assert.AreEqual(25, progress.Coins);
            Assert.IsTrue(progress.TrySpendCoins(20));
            Assert.AreEqual(5, progress.Coins);
        }

        [Test]
        public void DailyReward_PaysItsDiamondsOnce_EvenWhenTheClaimIsRetried()
        {
            var store = new InMemorySaveStore();
            var progress = new PlayerProgressService(store, _items, _clock);
            var catalog = ScriptableObject.CreateInstance<DailyRewardCatalog>();
            try
            {
                catalog.Set(new[] { new DailyRewardDay { title = "d1", rewards = new[] { new RewardDefinition { type = RewardType.Coins, amount = 20 } } } });
                var granter = new ProgressRewardGranter(progress, null);
                var prefix = DailyRewardService.GrantPrefixFor(DayKey.Today(_clock));
                Assert.IsTrue(granter.Grant(RewardClaims.GrantId(prefix, 0), new RewardDefinition { type = RewardType.Coins, amount = 20 }));
                var daily = new DailyRewardService(store, catalog, _clock, granter);
                Assert.AreEqual(ClaimResult.Claimed, daily.TryClaim(), "The interrupted claim completes...");
                Assert.AreEqual(20, progress.Coins, "...without paying the diamonds twice.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void ShippedPrices_AndCalendar_UseDiamonds()
        {
            var powerUps = AssetDatabase.LoadAssetAtPath<PowerUpCatalog>("Assets/_Project/ScriptableObjects/Progression/PowerUps.asset");
            Assert.AreEqual(30, powerUps.PowerUps.Single(p => p.id == "hammer").diamondPrice);
            Assert.AreEqual(20, powerUps.PowerUps.Single(p => p.id == "shuffle").diamondPrice);
            var calendar = AssetDatabase.LoadAssetAtPath<DailyRewardCatalog>("Assets/_Project/ScriptableObjects/Progression/DailyRewards.asset");
            Assert.IsTrue(calendar.Days.All(day => day.rewards.Count(r => r.type == RewardType.Coins && r.amount > 0) == 1), "Every calendar day gives diamonds once.");
            Assert.IsTrue(calendar.Days.All(DailyRewardCatalog.IsValid));
        }
    }
}
