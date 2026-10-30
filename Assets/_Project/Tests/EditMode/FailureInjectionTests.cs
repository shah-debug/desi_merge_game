using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DesiMerge.Configuration;
using DesiMerge.Core;
using DesiMerge.Persistence;
using DesiMerge.PowerUps;
using DesiMerge.Progression;
using DesiMerge.Settings;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DesiMerge.Tests.EditMode
{
    /// <summary>
    /// Phase 6.7 failure injection: corrupt, empty, impossible and unreadable saves, failing writes and wrong clocks.
    /// Everything runs on in-memory stores (or a temporary folder); real player saves are never touched.
    /// </summary>
    public class FailureInjectionTests
    {
        private static readonly DateTime Today = new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);

        private readonly List<Object> _created = new List<Object>();
        private TestContent _content;
        private MergeProgression _items;
        private PowerUpCatalog _powerUpCatalog;
        private DailyRewardCatalog _calendar;
        private MissionCatalog _missions;
        private MilestoneCatalog _ranks;
        private ManualClock _clock;

        /// <summary>A save store whose reads or writes can be made to throw, per key.</summary>
        private sealed class FaultyStore : ISaveStore
        {
            public readonly InMemorySaveStore Inner = new InMemorySaveStore();
            public readonly HashSet<string> FailingWrites = new HashSet<string>();
            public readonly HashSet<string> FailingReads = new HashSet<string>();
            public bool FailAllWrites;

            public bool TryRead(string key, out string contents)
            {
                if (FailingReads.Contains(key))
                {
                    throw new IOException("Injected read failure");
                }

                return Inner.TryRead(key, out contents);
            }

            public bool TryReadBackup(string key, out string contents)
            {
                if (FailingReads.Contains(key))
                {
                    throw new IOException("Injected read failure");
                }

                return Inner.TryReadBackup(key, out contents);
            }

            public void Write(string key, string contents)
            {
                if (FailAllWrites || FailingWrites.Contains(key))
                {
                    throw new IOException("Injected write failure");
                }

                Inner.Write(key, contents);
            }

            public void Delete(string key) => Inner.Delete(key);
        }

        [SetUp]
        public void SetUp()
        {
            _content = new TestContent();
            _items = _content.Progression(levels: 5, spawnable: 2);
            _clock = new ManualClock(Today);
            _powerUpCatalog = Track(ScriptableObject.CreateInstance<PowerUpCatalog>());
            _powerUpCatalog.Set(new[]
            {
                new PowerUpDefinition { id = "hammer", title = "Hammer", kind = PowerUpKind.Hammer, freeUsesPerGame = 1, maxSavedCharges = 9 },
                new PowerUpDefinition { id = "shuffle", title = "Shuffle", kind = PowerUpKind.Shuffle, freeUsesPerGame = 1, maxSavedCharges = 9 },
            });
            var hammer = new RewardDefinition { type = RewardType.PowerUpCharges, amount = 1, powerUpId = "hammer" };
            _calendar = Track(ScriptableObject.CreateInstance<DailyRewardCatalog>());
            _calendar.Set(Enumerable.Range(0, 7).Select(i => new DailyRewardDay { title = "d" + i, rewards = new[] { hammer } }).ToArray());
            _missions = Track(ScriptableObject.CreateInstance<MissionCatalog>());
            _missions.Set(new[]
            {
                new MissionDefinition { id = "m1", title = "m1", type = MissionType.Merges, target = 2, reward = hammer },
                new MissionDefinition { id = "w1", title = "w1", type = MissionType.Merges, target = 50, period = MissionPeriod.Weekly, reward = hammer },
            }, 1, 1);
            _ranks = Track(ScriptableObject.CreateInstance<MilestoneCatalog>());
            _ranks.Set(new[] { new MilestoneDefinition { id = "r100", title = "r", score = 100, reward = hammer } });
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

        private GameServices Services(ISaveStore store)
        {
            var progress = new PlayerProgressService(store, _items, _clock);
            var powerUps = new PowerUpService(store, _powerUpCatalog);
            var granter = new ProgressRewardGranter(progress, powerUps);
            return new GameServices(
                new GameStateMachine(), progress, new SettingsService(store, new SettingsData()), new DesiMerge.Audio.SilentAudioService(),
                new DesiMerge.Analytics.NullAnalyticsService(), new DesiMerge.Monetization.UnavailableRewardedAdService(), new SceneNavigator("a", "b"),
                new AchievementService(store, null, progress, _items, granter),
                new DailyChallengeService(store, null, progress, _items, _clock, granter), _clock, powerUps,
                new DesiMerge.Cosmetics.CosmeticService(store, null, null), _items,
                new DailyRewardService(store, _calendar, _clock, granter),
                new MissionService(store, _missions, progress, powerUps, _items, _clock, granter),
                new MilestoneService(store, _ranks, progress, granter));
        }

        // ---------- Corrupt and impossible data ----------

        [TestCase("this is not json")]
        [TestCase("")]
        [TestCase("{}")]
        [TestCase("{\"entries\":null,\"reachedIds\":null,\"claimedIds\":null,\"charges\":null,\"appliedGrantIds\":null,\"discoveredItemIds\":null}")]
        [TestCase("{\"schemaVersion\":99,\"unknownField\":[1,2,3]}")]
        public void BrokenSaveFiles_LoadAsWorkingDefaults_WithoutExceptions(string contents)
        {
            var store = new InMemorySaveStore();
            foreach (var key in new[] { SaveKey.PlayerProgress, SaveKey.Settings, SaveKey.PowerUps, SaveKey.DailyRewards, SaveKey.Missions, SaveKey.Milestones, SaveKey.DailyChallenge, SaveKey.Achievements, SaveKey.Cosmetics })
            {
                store.Write(key, contents);
            }

            var services = Services(store);
            Assert.IsTrue(services.DailyRewards.CanClaim);
            Assert.AreEqual(1, services.Missions.Daily.Count);
            Assert.AreEqual(0, services.Milestones.ClaimableCount);
            Assert.AreEqual(0, services.PowerUps.SavedCharges("hammer"));
            Assert.AreEqual(ClaimResult.Claimed, services.DailyRewards.TryClaim(), "The game keeps working after recovery.");
            Assert.IsTrue(services.FlushAll());
            services.Dispose();
        }

        [Test]
        public void ImpossibleValues_AreClampedToValidRanges()
        {
            var store = new InMemorySaveStore();
            store.Write(SaveKey.DailyRewards, $"{{\"lastClaimDay\":{DayKey.Today(_clock) - 1},\"lastClaimIndex\":99,\"streak\":-4,\"bestStreak\":-9,\"totalClaims\":-1,\"latestDay\":-50}}");
            store.Write(SaveKey.Missions, $"{{\"latestDay\":{DayKey.Today(_clock)},\"entries\":[{{\"id\":\"m1\",\"period\":0,\"periodKey\":{DayKey.Today(_clock)},\"progress\":-50}}]}}");
            store.Write(SaveKey.PowerUps, "{\"charges\":[{\"id\":\"hammer\",\"count\":9999},{\"id\":\"shuffle\",\"count\":-3}]}");
            store.Write(SaveKey.PlayerProgress, "{\"schemaVersion\":2,\"highScore\":-40,\"coins\":-7,\"gamesPlayed\":-2,\"totalMerges\":-1}");

            var services = Services(store);
            Assert.That(services.DailyRewards.TodayIndex, Is.InRange(0, 6));
            Assert.GreaterOrEqual(services.DailyRewards.Streak, 0);
            Assert.AreEqual(0, services.Missions.Daily[0].Progress);
            Assert.AreEqual(9, services.PowerUps.SavedCharges("hammer"), "Charges can't exceed the configured maximum.");
            Assert.AreEqual(0, services.PowerUps.SavedCharges("shuffle"));
            Assert.AreEqual(0, services.Progress.HighScore);
            Assert.GreaterOrEqual(services.Progress.Coins, 0);
            Assert.GreaterOrEqual(services.Progress.GamesPlayed, 0);
            Assert.GreaterOrEqual(services.Progress.TotalMerges, 0);
            services.Dispose();
        }

        [Test]
        public void NewerSchemaVersion_KeepsTheKnownProgress()
        {
            var store = new InMemorySaveStore();
            store.Write(SaveKey.PlayerProgress, "{\"schemaVersion\":42,\"highScore\":777,\"gamesPlayed\":12,\"discoveredItemIds\":[\"l0\",\"l1\"],\"futureThing\":{\"a\":1}}");
            var services = Services(store);
            Assert.AreEqual(777, services.Progress.HighScore);
            Assert.AreEqual(12, services.Progress.GamesPlayed);
            Assert.IsTrue(services.Progress.IsDiscovered("l1"));
            services.Dispose();
        }

        // ---------- Unreadable storage ----------

        [Test]
        public void UnreadableSave_IsNeverOverwrittenByAFreshProfile()
        {
            var store = new FaultyStore();
            var first = Services(store);
            first.Progress.SubmitFinalScore(900);
            first.PowerUps.TryGrant("seed", "hammer", 4);
            Assert.IsTrue(first.FlushAll());
            first.Dispose();

            store.FailingReads.Add(SaveKey.PlayerProgress);
            store.FailingReads.Add(SaveKey.PowerUps);
            var unreadable = Services(store);
            Assert.AreEqual(0, unreadable.Progress.HighScore, "This session can't see the data...");
            Assert.AreEqual(LoadOutcome.ReadFailed, unreadable.Progress.LoadOutcome, "...and says so instead of claiming a fresh start.");
            unreadable.Progress.SubmitFinalScore(10);
            unreadable.PowerUps.TryGrant("other", "hammer", 1);
            unreadable.FlushAll();
            unreadable.FlushAll();
            unreadable.Dispose();

            store.FailingReads.Clear();
            var recovered = Services(store);
            Assert.AreEqual(900, recovered.Progress.HighScore, "The real best score survived.");
            Assert.AreEqual(4, recovered.PowerUps.SavedCharges("hammer"));
            recovered.Dispose();
        }

        // ---------- Failing writes ----------

        [Test]
        public void FailedWrites_AreReported_AndSavedOnTheNextFlush()
        {
            var store = new FaultyStore { FailAllWrites = true };
            var services = Services(store);
            Assert.AreEqual(ClaimResult.Claimed, services.DailyRewards.TryClaim());
            services.Progress.SubmitFinalScore(150);
            Assert.IsFalse(services.FlushAll(), "A failed write is reported, not hidden.");

            var killed = Services(store);
            Assert.IsTrue(killed.DailyRewards.CanClaim, "Nothing reached disk: the claim was not half-saved.");
            Assert.AreEqual(0, killed.PowerUps.SavedCharges("hammer"));
            killed.Dispose();

            store.FailAllWrites = false;
            Assert.IsTrue(services.FlushAll(), "The pending changes are written once storage works again.");
            services.Dispose();
            var reopened = Services(store);
            Assert.IsFalse(reopened.DailyRewards.CanClaim);
            Assert.AreEqual(1, reopened.PowerUps.SavedCharges("hammer"));
            Assert.AreEqual(150, reopened.Progress.HighScore);
            reopened.Dispose();
        }

        [Test]
        public void KilledBetweenGrantAndClaimRecord_NeitherLosesNorDuplicates()
        {
            var store = new FaultyStore();
            store.FailingWrites.Add(SaveKey.DailyRewards);
            store.FailingWrites.Add(SaveKey.Missions);
            var services = Services(store);
            services.Progress.RecordMerge(_items.Get(1));
            services.Progress.RecordMerge(_items.Get(1));
            Assert.AreEqual(ClaimResult.Claimed, services.DailyRewards.TryClaim());
            Assert.AreEqual(ClaimResult.Claimed, services.Missions.TryClaim("m1"));
            services.Progress.Flush();

            store.FailingWrites.Clear();
            var relaunched = Services(store);
            Assert.AreEqual(2, relaunched.PowerUps.SavedCharges("hammer"), "Both grants were saved before the crash.");
            Assert.AreEqual(ClaimResult.Claimed, relaunched.DailyRewards.TryClaim(), "The claim reappears...");
            Assert.AreEqual(2, relaunched.PowerUps.SavedCharges("hammer"), "...but is not paid twice.");
            relaunched.Dispose();
        }

        [Test]
        public void FileStore_InterruptedWrite_KeepsTheLastGoodFile()
        {
            var folder = Path.Combine(Path.GetTempPath(), "desimerge-qa-" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new FileSaveStore(folder);
                store.Write("probe", "{\"v\":1}");
                store.Write("probe", "{\"v\":2}");
                File.WriteAllText(Path.Combine(folder, "probe.json.tmp"), "{\"v\":");
                Assert.IsTrue(store.TryRead("probe", out var main));
                Assert.AreEqual("{\"v\":2}", main, "A torn temp file never replaces the real one.");
                Assert.IsTrue(store.TryReadBackup("probe", out var backup));
                Assert.AreEqual("{\"v\":1}", backup);

                File.Delete(Path.Combine(folder, "probe.json"));
                var repository = new JsonRepository<ProbeData>(store, "probe");
                Assert.AreEqual(1, repository.Load(out var outcome).v, "The backup is used when the main file is gone.");
                Assert.AreEqual(LoadOutcome.RecoveredFromBackup, outcome);
            }
            finally
            {
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
            }
        }

        [Serializable]
        private sealed class ProbeData
        {
            public int v;
        }

        // ---------- Wrong clocks ----------

        [Test]
        public void FarFutureClockMistake_DoesNotLockDailySystemsForYears()
        {
            var store = new InMemorySaveStore();
            _clock.UtcNow = Today.AddYears(10);
            var wrong = Services(store);
            Assert.AreEqual(ClaimResult.Claimed, wrong.DailyRewards.TryClaim());
            wrong.FlushAll();
            wrong.Dispose();

            _clock.UtcNow = Today;
            var corrected = Services(store);
            Assert.IsTrue(corrected.DailyRewards.CanClaim, "Rewards resume once the clock is corrected.");
            Assert.AreEqual(DayKey.Today(_clock), corrected.Missions.DayKey, "Missions follow the corrected date.");
            Assert.AreEqual(1, corrected.PowerUps.SavedCharges("hammer"), "What was already claimed is kept.");
            corrected.Dispose();
        }

        [Test]
        public void ClockMovedBackAFewDays_StillCannotReclaim()
        {
            var store = new InMemorySaveStore();
            var services = Services(store);
            services.DailyRewards.TryClaim();
            services.FlushAll();
            _clock.AdvanceDays(-3);
            var moved = Services(store);
            Assert.IsFalse(moved.DailyRewards.CanClaim);
            Assert.AreEqual(ClaimResult.NotAvailable, moved.DailyRewards.TryClaim());
            Assert.AreEqual(1, moved.PowerUps.SavedCharges("hammer"));
        }

        [Test]
        public void CalendarDay_WithTwoRewardsOfOnePowerUp_IsRejectedAsInvalid()
        {
            var hammer = new RewardDefinition { type = RewardType.PowerUpCharges, amount = 1, powerUpId = "hammer" };
            Assert.IsFalse(DailyRewardCatalog.IsValid(new DailyRewardDay { rewards = new[] { hammer, hammer } }),
                "Two grants of one power-up on one day could pass the room check separately and then fail halfway.");
        }
    }
}
