using System;
using System.Collections.Generic;
using DesiMerge.Analytics;
using DesiMerge.Configuration;
using DesiMerge.Core;
using DesiMerge.Cosmetics;
using DesiMerge.Gameplay;
using DesiMerge.Monetization;
using DesiMerge.Persistence;
using DesiMerge.PowerUps;
using DesiMerge.Progression;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DesiMerge.Tests.EditMode
{
    /// <summary>Phase 5: power-up inventory, idempotent rewards, the ad coordinator, shuffle planning and cosmetics.</summary>
    public class PowerUpAndAdTests
    {
        private static readonly DateTime Day0 = new DateTime(2026, 10, 27, 10, 0, 0, DateTimeKind.Utc);

        private readonly List<Object> _created = new List<Object>();
        private TestContent _content;
        private MergeProgression _items;
        private ManualClock _clock;
        private PowerUpCatalog _powerUps;

        [SetUp]
        public void SetUp()
        {
            _content = new TestContent();
            _items = _content.Progression(levels: 5, spawnable: 2);
            _clock = new ManualClock(Day0);
            _powerUps = Track(ScriptableObject.CreateInstance<PowerUpCatalog>());
            _powerUps.Set(new[]
            {
                new PowerUpDefinition { id = "hammer", title = "Hammer", kind = PowerUpKind.Hammer, freeUsesPerGame = 1, maxSavedCharges = 3 },
                new PowerUpDefinition { id = "shuffle", title = "Shuffle", kind = PowerUpKind.Shuffle, freeUsesPerGame = 2, maxSavedCharges = 3 },
            });
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
            _content.DestroyAll();
        }

        private T Track<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }

        // ---------- Power-up inventory ----------

        [Test]
        public void FreeUses_ComeFromConfigAndResetEveryGame()
        {
            var service = new PowerUpService(new InMemorySaveStore(), _powerUps);

            Assert.AreEqual(1, service.Available("hammer"));
            Assert.AreEqual(2, service.Available("shuffle"));
            Assert.IsTrue(service.TryConsume("hammer"));
            Assert.IsFalse(service.TryConsume("hammer"), "The free use is gone and no charges are saved.");
            Assert.AreEqual(0, service.Available("hammer"));

            service.BeginGame();

            Assert.AreEqual(1, service.Available("hammer"));
        }

        [Test]
        public void Consume_UsesFreeUsesBeforeSavedCharges_AndRefusesUnknownIds()
        {
            var store = new InMemorySaveStore();
            var service = new PowerUpService(store, _powerUps);
            Assert.IsTrue(service.TryGrant("test:1", "hammer", 2));

            Assert.IsTrue(service.TryConsume("hammer"));
            Assert.AreEqual(0, service.FreeUsesLeft("hammer"));
            Assert.AreEqual(2, service.SavedCharges("hammer"), "Saved charges are kept while free uses remain.");
            Assert.IsTrue(service.TryConsume("hammer"));
            Assert.AreEqual(1, service.SavedCharges("hammer"));
            Assert.IsFalse(service.TryConsume("undo"));
            Assert.AreEqual(0, service.Available("undo"));
        }

        [Test]
        public void SavedCharges_PersistAcrossRestart_FreeUsesDoNot()
        {
            var store = new InMemorySaveStore();
            var first = new PowerUpService(store, _powerUps);
            first.TryGrant("test:1", "shuffle", 3);
            first.TryConsume("shuffle");
            first.TryConsume("shuffle");
            first.TryConsume("shuffle"); // both free uses, then one saved charge
            Assert.IsTrue(first.Flush());

            var second = new PowerUpService(store, _powerUps);

            Assert.AreEqual(2, second.SavedCharges("shuffle"));
            Assert.AreEqual(2, second.FreeUsesLeft("shuffle"), "A new app session starts a new game allowance.");
        }

        [Test]
        public void Grant_IsAppliedOncePerGrantId_EvenAfterRestart()
        {
            var store = new InMemorySaveStore();
            var service = new PowerUpService(store, _powerUps);

            Assert.IsTrue(service.TryGrant("daily:1:x", "hammer", 1));
            Assert.IsTrue(service.TryGrant("daily:1:x", "hammer", 1), "A repeat reports success...");
            Assert.AreEqual(1, service.SavedCharges("hammer"), "...but adds nothing.");

            var reopened = new PowerUpService(store, _powerUps); // TryGrant saved immediately
            Assert.IsTrue(reopened.HasApplied("daily:1:x"));
            Assert.IsTrue(reopened.TryGrant("daily:1:x", "hammer", 1));
            Assert.AreEqual(1, reopened.SavedCharges("hammer"));
        }

        [Test]
        public void Grant_RefusesUnknownPowerUpBadAmountAndOverflow()
        {
            var service = new PowerUpService(new InMemorySaveStore(), _powerUps);

            Assert.IsFalse(service.TryGrant("g1", "undo", 1));
            Assert.IsFalse(service.TryGrant("g2", "hammer", 0));
            Assert.IsTrue(service.TryGrant("g3", "hammer", 3));
            Assert.IsFalse(service.TryGrant("g4", "hammer", 1), "Max saved charges is 3.");
            Assert.AreEqual(3, service.SavedCharges("hammer"));
            Assert.IsFalse(service.HasApplied("g4"), "A refused grant is not recorded, so it can be retried.");
        }

        [Test]
        public void CorruptPowerUpSave_UsesDefaultsAndKeepsTheBadCopy()
        {
            var store = new InMemorySaveStore();
            store.Corrupt(SaveKey.PowerUps, "{ not json");

            var service = new PowerUpService(store, _powerUps);

            Assert.AreEqual(LoadOutcome.ResetToDefaults, service.LoadOutcome);
            Assert.AreEqual(0, service.SavedCharges("hammer"));
            Assert.IsTrue(store.TryRead(SaveKey.PowerUps + "_corrupt", out _));
        }

        [Test]
        public void TamperedSave_IsClampedOnLoad()
        {
            var store = new InMemorySaveStore();
            store.Write(SaveKey.PowerUps, "{\"schemaVersion\":1,\"charges\":[{\"id\":\"hammer\",\"count\":-5},{\"id\":\"\",\"count\":4}],\"appliedGrantIds\":[\"\"]}");

            var service = new PowerUpService(store, _powerUps);

            Assert.AreEqual(0, service.SavedCharges("hammer"));
        }

        [Test]
        public void Catalog_ValidateReportsDuplicateIdsAndKinds()
        {
            var catalog = Track(ScriptableObject.CreateInstance<PowerUpCatalog>());
            catalog.Set(new[]
            {
                new PowerUpDefinition { id = "a", kind = PowerUpKind.Hammer },
                new PowerUpDefinition { id = "a", kind = PowerUpKind.Hammer },
                new PowerUpDefinition { id = "", kind = PowerUpKind.Shuffle },
            });

            Assert.AreEqual(3, catalog.Validate().Count);
            Assert.AreEqual(0, _powerUps.Validate().Count);
        }

        // ---------- Rewards from progression ----------

        private DailyChallengeCatalog DailyWithHammerReward()
        {
            var catalog = Track(ScriptableObject.CreateInstance<DailyChallengeCatalog>());
            catalog.Set(new[]
            {
                new DailyChallengeDefinition
                {
                    id = "s100", title = "Score 100", type = DailyChallengeType.ScoreInOneGame, target = 100,
                    reward = new RewardDefinition { type = RewardType.PowerUpCharges, amount = 1, powerUpId = "hammer" },
                },
            });
            return catalog;
        }

        [Test]
        public void DailyPowerUpReward_IsGrantedExactlyOnce()
        {
            var store = new InMemorySaveStore();
            var progress = new PlayerProgressService(store, _items, _clock);
            var powerUps = new PowerUpService(store, _powerUps);
            var daily = new DailyChallengeService(store, DailyWithHammerReward(), progress, _items, _clock, new ProgressRewardGranter(progress, powerUps));

            Assert.IsFalse(daily.TryClaim(), "Nothing before completion.");
            Assert.AreEqual(0, powerUps.SavedCharges("hammer"));
            progress.SubmitFinalScore(120);
            Assert.IsTrue(daily.TryClaim());
            Assert.IsFalse(daily.TryClaim());
            Assert.AreEqual(1, powerUps.SavedCharges("hammer"));
            daily.Dispose();
        }

        [Test]
        public void InterruptedClaim_GrantSavedButClaimNot_ClaimingAgainDoesNotDuplicate()
        {
            var store = new InMemorySaveStore();
            var catalog = DailyWithHammerReward();
            var progress = new PlayerProgressService(store, _items, _clock);
            var powerUps = new PowerUpService(store, _powerUps);
            var daily = new DailyChallengeService(store, catalog, progress, _items, _clock, new ProgressRewardGranter(progress, powerUps));
            progress.SubmitFinalScore(120);
            Assert.IsTrue(daily.Flush());
            // The app dies right after the grant was written but before the claim was: simulate exactly that state.
            Assert.IsTrue(powerUps.TryGrant(DailyChallengeService.GrantIdFor(daily.DayNumber, daily.Current), "hammer", 1));
            daily.Dispose();

            var progressAgain = new PlayerProgressService(store, _items, _clock);
            var powerUpsAgain = new PowerUpService(store, _powerUps);
            var dailyAgain = new DailyChallengeService(store, catalog, progressAgain, _items, _clock, new ProgressRewardGranter(progressAgain, powerUpsAgain));

            Assert.IsTrue(dailyAgain.CanClaim, "The claim was never recorded...");
            Assert.IsTrue(dailyAgain.TryClaim());
            Assert.AreEqual(1, powerUpsAgain.SavedCharges("hammer"), "...but the reward is not granted twice.");
            Assert.IsTrue(dailyAgain.IsClaimed);
            dailyAgain.Dispose();
        }

        [Test]
        public void ClaimRefusedWhenChargesAreFull_StaysClaimable()
        {
            var store = new InMemorySaveStore();
            var progress = new PlayerProgressService(store, _items, _clock);
            var powerUps = new PowerUpService(store, _powerUps);
            powerUps.TryGrant("earlier", "hammer", 3);
            var daily = new DailyChallengeService(store, DailyWithHammerReward(), progress, _items, _clock, new ProgressRewardGranter(progress, powerUps));
            progress.SubmitFinalScore(120);

            Assert.IsFalse(daily.TryClaim());
            Assert.IsTrue(daily.CanClaim);
            Assert.IsFalse(daily.IsClaimed);

            powerUps.TryConsume("hammer"); // free use
            powerUps.TryConsume("hammer"); // one saved charge → room again
            Assert.IsTrue(daily.TryClaim());
            Assert.AreEqual(3, powerUps.SavedCharges("hammer"));
            daily.Dispose();
        }

        [Test]
        public void AchievementPowerUpReward_GrantedOnceAndNotLostWhenFull()
        {
            var store = new InMemorySaveStore();
            var catalog = Track(ScriptableObject.CreateInstance<AchievementCatalog>());
            catalog.Set(new[]
            {
                new AchievementDefinition
                {
                    id = "first_game", title = "First", type = AchievementType.CompleteGames, target = 1,
                    reward = new RewardDefinition { type = RewardType.PowerUpCharges, amount = 1, powerUpId = "hammer" },
                },
            });
            var progress = new PlayerProgressService(store, _items, _clock);
            var powerUps = new PowerUpService(store, _powerUps);
            powerUps.TryGrant("earlier", "hammer", 3);
            var achievements = new AchievementService(store, catalog, progress, _items, new ProgressRewardGranter(progress, powerUps));

            progress.SubmitFinalScore(10);
            Assert.IsTrue(achievements.IsUnlocked("first_game"));
            Assert.AreEqual(3, powerUps.SavedCharges("hammer"), "Full: not granted yet.");

            powerUps.TryConsume("hammer");
            powerUps.TryConsume("hammer");
            progress.SubmitFinalScore(10); // any progress change re-evaluates
            progress.SubmitFinalScore(10);

            Assert.AreEqual(3, powerUps.SavedCharges("hammer"), "Granted exactly once when there was room.");
            achievements.Dispose();
        }

        // ---------- Revived games ----------

        [Test]
        public void RevivedFinalScore_UpdatesBestOnly_GameCountedOnce()
        {
            var store = new InMemorySaveStore();
            var progress = new PlayerProgressService(store, _items, _clock);
            var games = 0;
            var finals = new List<long>();
            progress.GameCompleted += _ => games++;
            progress.FinalScoreRecorded += finals.Add;

            Assert.IsTrue(progress.SubmitFinalScore(100));
            Assert.IsTrue(progress.SubmitRevivedFinalScore(180));
            Assert.IsFalse(progress.SubmitRevivedFinalScore(150));

            Assert.AreEqual(1, progress.GamesPlayed);
            Assert.AreEqual(1, games);
            Assert.AreEqual(180, progress.HighScore);
            Assert.AreEqual(1, progress.CurrentStreak);
            CollectionAssert.AreEqual(new long[] { 100, 180, 150 }, finals);
        }

        [Test]
        public void Daily_RevivedGameCountsOnceButItsHigherScoreCounts()
        {
            var store = new InMemorySaveStore();
            var catalog = Track(ScriptableObject.CreateInstance<DailyChallengeCatalog>());
            catalog.Set(new[] { new DailyChallengeDefinition { id = "s200", title = "Score 200", type = DailyChallengeType.ScoreInOneGame, target = 200 } });
            var progress = new PlayerProgressService(store, _items, _clock);
            var daily = new DailyChallengeService(store, catalog, progress, _items, _clock, null);

            progress.SubmitFinalScore(120);
            Assert.AreEqual(120, daily.Progress);
            progress.SubmitRevivedFinalScore(240);
            Assert.IsTrue(daily.IsCompleted);
            daily.Dispose();

            var gamesCatalog = Track(ScriptableObject.CreateInstance<DailyChallengeCatalog>());
            gamesCatalog.Set(new[] { new DailyChallengeDefinition { id = "g2", title = "Two games", type = DailyChallengeType.CompleteGames, target = 2 } });
            var store2 = new InMemorySaveStore();
            var progress2 = new PlayerProgressService(store2, _items, _clock);
            var daily2 = new DailyChallengeService(store2, gamesCatalog, progress2, _items, _clock, null);
            progress2.SubmitFinalScore(10);
            progress2.SubmitRevivedFinalScore(20);
            Assert.AreEqual(1, daily2.Progress, "A revived game is still one game.");
            daily2.Dispose();
        }

        // ---------- Rewarded-ad coordinator ----------

        private sealed class ScriptedProvider : IRewardedAdService
        {
            public readonly List<Action<RewardedAdResult>> Callbacks = new List<Action<RewardedAdResult>>();
            public bool IsSupported { get; set; } = true;
            public bool Ready { get; set; } = true;
            public bool Throw { get; set; }
            public int Shows { get; private set; }

            public bool IsReady(RewardedPlacement placement) => Ready;

            public void Show(RewardedPlacement placement, Action<RewardedAdResult> onComplete)
            {
                Shows++;
                if (Throw)
                {
                    throw new InvalidOperationException("SDK exploded");
                }

                Callbacks.Add(onComplete);
            }
        }

        [TestCase(RewardedAdResult.Completed)]
        [TestCase(RewardedAdResult.Skipped)]
        [TestCase(RewardedAdResult.Failed)]
        [TestCase(RewardedAdResult.NoFill)]
        [TestCase(RewardedAdResult.LoadFailed)]
        [TestCase(RewardedAdResult.NetworkUnavailable)]
        public void Coordinator_ReportsEachRequestOnce_WithTheProviderResult(RewardedAdResult providerResult)
        {
            var provider = new ScriptedProvider();
            var ads = new RewardedAdCoordinator(provider, new NullAnalyticsService());
            var results = new List<RewardedAdResult>();

            Assert.IsTrue(ads.TryShow(RewardedPlacement.ContinueAfterGameOver, results.Add));
            Assert.IsTrue(ads.IsBusy);
            Assert.AreEqual(0, results.Count, "Nothing is reported before the provider calls back.");
            provider.Callbacks[0](providerResult);

            CollectionAssert.AreEqual(new[] { providerResult }, results);
            Assert.IsFalse(ads.IsBusy);
        }

        [Test]
        public void Coordinator_IgnoresDuplicateCallbacks()
        {
            var provider = new ScriptedProvider();
            var ads = new RewardedAdCoordinator(provider, new NullAnalyticsService());
            var completed = 0;
            ads.TryShow(RewardedPlacement.ContinueAfterGameOver, result => completed += result == RewardedAdResult.Completed ? 1 : 0);

            provider.Callbacks[0](RewardedAdResult.Completed);
            provider.Callbacks[0](RewardedAdResult.Completed);
            provider.Callbacks[0](RewardedAdResult.Skipped);

            Assert.AreEqual(1, completed);
            Assert.AreEqual(2, ads.IgnoredCallbacks);
        }

        [Test]
        public void Coordinator_RefusesConcurrentRequests()
        {
            var provider = new ScriptedProvider();
            var ads = new RewardedAdCoordinator(provider, new NullAnalyticsService());
            var secondCalled = false;

            Assert.IsTrue(ads.TryShow(RewardedPlacement.ContinueAfterGameOver, _ => { }));
            Assert.IsFalse(ads.IsReady(RewardedPlacement.ContinueAfterGameOver));
            Assert.IsFalse(ads.TryShow(RewardedPlacement.ContinueAfterGameOver, _ => secondCalled = true));

            Assert.AreEqual(1, provider.Shows);
            Assert.IsFalse(secondCalled);
        }

        [Test]
        public void Coordinator_NotReadyOrUnsupported_StartsNothing()
        {
            var provider = new ScriptedProvider { Ready = false };
            var ads = new RewardedAdCoordinator(provider, new NullAnalyticsService());
            var called = false;

            Assert.IsFalse(ads.TryShow(RewardedPlacement.ContinueAfterGameOver, _ => called = true));
            provider.Ready = true;
            provider.IsSupported = false;
            Assert.IsFalse(ads.TryShow(RewardedPlacement.ContinueAfterGameOver, _ => called = true));

            Assert.AreEqual(0, provider.Shows);
            Assert.IsFalse(called);
        }

        [Test]
        public void Coordinator_ProviderExceptionBecomesFailure()
        {
            var provider = new ScriptedProvider { Throw = true };
            var ads = new RewardedAdCoordinator(provider, new NullAnalyticsService());
            var results = new List<RewardedAdResult>();

            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"\[Ads\] Showing the ad failed"));
            Assert.IsTrue(ads.TryShow(RewardedPlacement.ContinueAfterGameOver, results.Add));

            CollectionAssert.AreEqual(new[] { RewardedAdResult.Failed }, results);
            Assert.IsFalse(ads.IsBusy);
        }

        [Test]
        public void Coordinator_AbandonedRequest_IgnoresTheLateCompletion()
        {
            var provider = new ScriptedProvider();
            var ads = new RewardedAdCoordinator(provider, new NullAnalyticsService());
            var results = new List<RewardedAdResult>();
            ads.TryShow(RewardedPlacement.ContinueAfterGameOver, results.Add);

            ads.Abandon();
            provider.Callbacks[0](RewardedAdResult.Completed);

            CollectionAssert.AreEqual(new[] { RewardedAdResult.Failed }, results);
            Assert.AreEqual(1, ads.IgnoredCallbacks);
        }

        [Test]
        public void Coordinator_SynchronousProviderAndTheShippedFallbacks()
        {
            var simulated = new RewardedAdCoordinator(new SimulatedRewardedAdService { NextResult = RewardedAdResult.Skipped }, new NullAnalyticsService());
            var result = RewardedAdResult.Completed;
            Assert.IsTrue(simulated.TryShow(RewardedPlacement.ContinueAfterGameOver, r => result = r));
            Assert.AreEqual(RewardedAdResult.Skipped, result);
            Assert.IsFalse(simulated.IsBusy);

            var unavailable = new RewardedAdCoordinator(new UnavailableRewardedAdService(), new NullAnalyticsService());
            Assert.IsFalse(unavailable.IsSupported);
            Assert.IsFalse(unavailable.TryShow(RewardedPlacement.ContinueAfterGameOver, _ => Assert.Fail("Must not be called.")));
        }

        // ---------- Shuffle planning ----------

        [TestCase(1)]
        [TestCase(7)]
        [TestCase(42)]
        public void ShufflePlan_KeepsItemsInsideWithoutOverlap(int seed)
        {
            var random = new System.Random(seed);
            var radii = new List<float>();
            for (var i = 0; i < 14; i++)
            {
                radii.Add(0.3f + 0.1f * random.Next(5));
            }

            var positions = new List<Vector2>();
            Assert.IsTrue(ShufflePlanner.TryPlan(radii, 3.25f, 8.6f, 0.04f, new System.Random(seed), positions));

            Assert.AreEqual(radii.Count, positions.Count);
            Assert.IsFalse(ShufflePlanner.HasOverlap(positions, radii));
            Assert.IsTrue(ShufflePlanner.AllInside(positions, radii, 3.25f, 8.6f));
            for (var i = 0; i < positions.Count; i++)
            {
                Assert.GreaterOrEqual(positions[i].x - radii[i], -3.25f + 0.04f - 0.0001f, "Gap to the left wall.");
                Assert.LessOrEqual(positions[i].x + radii[i], 3.25f - 0.04f + 0.0001f, "Gap to the right wall.");
            }
        }

        [Test]
        public void ShufflePlan_FailsWhenTheStackWouldReachTheLimit()
        {
            var radii = new List<float>();
            for (var i = 0; i < 40; i++)
            {
                radii.Add(0.7f);
            }

            var positions = new List<Vector2>();
            Assert.IsFalse(ShufflePlanner.TryPlan(radii, 3.25f, 8.6f, 0.04f, new System.Random(1), positions));
            Assert.AreEqual(0, positions.Count);
        }

        [Test]
        public void ShufflePlan_IsDeterministicForASeed()
        {
            var radii = new List<float> { 0.3f, 0.4f, 0.3f, 0.5f, 0.6f, 0.3f };
            var a = new List<Vector2>();
            var b = new List<Vector2>();

            ShufflePlanner.TryPlan(radii, 3.25f, 8.6f, 0.04f, new System.Random(5), a);
            ShufflePlanner.TryPlan(radii, 3.25f, 8.6f, 0.04f, new System.Random(5), b);

            CollectionAssert.AreEqual(a, b);
        }

        // ---------- Cosmetics foundation ----------

        private CosmeticCatalog Cosmetics()
        {
            var catalog = Track(ScriptableObject.CreateInstance<CosmeticCatalog>());
            catalog.Set(new[]
            {
                new CosmeticDefinition { id = "classic", title = "Classic", kind = CosmeticKind.ContainerTheme, unlock = CosmeticUnlock.Default },
                new CosmeticDefinition { id = "truck", title = "Truck Art", kind = CosmeticKind.ContainerTheme, unlock = CosmeticUnlock.Achievement, achievementId = "first_game" },
            });
            return catalog;
        }

        [Test]
        public void Cosmetics_SelectionRequiresAnOwnedExistingItem_AndPersists()
        {
            var store = new InMemorySaveStore();
            var achievementCatalog = Track(ScriptableObject.CreateInstance<AchievementCatalog>());
            achievementCatalog.Set(new[] { new AchievementDefinition { id = "first_game", title = "First", type = AchievementType.CompleteGames, target = 1 } });
            var progress = new PlayerProgressService(store, _items, _clock);
            var achievements = new AchievementService(store, achievementCatalog, progress, _items, null);
            var cosmetics = new CosmeticService(store, Cosmetics(), achievements);

            Assert.AreEqual("classic", cosmetics.Selected(CosmeticKind.ContainerTheme).id);
            Assert.IsFalse(cosmetics.TrySelect("truck"), "Not owned yet.");
            Assert.IsFalse(cosmetics.TrySelect("missing"));

            progress.SubmitFinalScore(10); // unlocks first_game → owns "truck"
            Assert.IsTrue(cosmetics.IsOwned("truck"));
            Assert.AreEqual("classic", cosmetics.Selected(CosmeticKind.ContainerTheme).id, "Unlocking doesn't change the selection.");
            Assert.IsTrue(cosmetics.TrySelect("truck"));
            Assert.IsTrue(cosmetics.Flush());
            Assert.IsTrue(achievements.Flush());
            cosmetics.Dispose();
            achievements.Dispose();

            var reopened = new CosmeticService(store, Cosmetics(), null);
            Assert.AreEqual("truck", reopened.Selected(CosmeticKind.ContainerTheme).id);
        }

        [Test]
        public void Cosmetics_InvalidSavedSelectionFallsBackToDefault()
        {
            var store = new InMemorySaveStore();
            store.Write(SaveKey.Cosmetics, "{\"schemaVersion\":1,\"ownedIds\":[],\"selectedContainerTheme\":\"truck\"}");

            var cosmetics = new CosmeticService(store, Cosmetics(), null);

            Assert.AreEqual("classic", cosmetics.Selected(CosmeticKind.ContainerTheme).id, "Selected but not owned → default.");
            Assert.IsNull(cosmetics.Selected(CosmeticKind.ItemSkin), "No item skins defined.");
        }
    }
}
