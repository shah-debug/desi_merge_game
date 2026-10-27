using System;
using System.IO;
using System.Text.RegularExpressions;
using DesiMerge.Configuration;
using DesiMerge.Core;
using DesiMerge.Persistence;
using DesiMerge.Progression;
using DesiMerge.Settings;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DesiMerge.Tests.EditMode
{
    /// <summary>Phase 4 persistence and progression. Uses in-memory or temp-folder storage and a manual clock only.</summary>
    public class ProgressionTests
    {
        private static readonly DateTime Day0 = new DateTime(2026, 10, 27, 10, 0, 0, DateTimeKind.Utc);

        private TestContent _content;
        private MergeProgression _items;
        private ManualClock _clock;

        [SetUp]
        public void SetUp()
        {
            _content = new TestContent();
            _items = _content.Progression(levels: 5, spawnable: 2);
            _clock = new ManualClock(Day0);
        }

        [TearDown]
        public void TearDown() => _content.DestroyAll();

        // ---------- Save/load robustness ----------

        [Test]
        public void NewPlayer_StartsWithValidDefaults()
        {
            var progress = new PlayerProgressService(new InMemorySaveStore(), _items, _clock);

            Assert.AreEqual(LoadOutcome.NoSave, progress.LoadOutcome);
            Assert.AreEqual(0, progress.HighScore);
            Assert.AreEqual(0, progress.GamesPlayed);
            Assert.AreEqual(0, progress.TotalMerges);
            Assert.AreEqual(0, progress.DiscoveredItemIds.Count);
            Assert.AreEqual(string.Empty, progress.HighestItemId);
            Assert.AreEqual(0, progress.DisplayedStreak());
        }

        [Test]
        public void Progress_RoundTripsThroughFilesAcrossRestarts()
        {
            var directory = Path.Combine(Path.GetTempPath(), "DesiMergeTests", Guid.NewGuid().ToString("N"));
            try
            {
                var first = new PlayerProgressService(new FileSaveStore(directory), _items, _clock);
                first.RecordSeen(_items.Get(0));
                first.RecordMerge(_items.Get(1));
                first.SubmitFinalScore(120);
                Assert.IsTrue(first.Flush());

                var reopened = new PlayerProgressService(new FileSaveStore(directory), _items, _clock);

                Assert.AreEqual(LoadOutcome.Loaded, reopened.LoadOutcome);
                Assert.AreEqual(120, reopened.HighScore);
                Assert.AreEqual(1, reopened.GamesPlayed);
                Assert.AreEqual(1, reopened.TotalMerges);
                Assert.IsTrue(reopened.IsDiscovered("l0") && reopened.IsDiscovered("l1"));
                Assert.AreEqual("l1", reopened.HighestItemId);
                Assert.AreEqual(1, reopened.CurrentStreak);
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void CorruptSave_RecoversFromBackup()
        {
            var store = new InMemorySaveStore();
            var progress = new PlayerProgressService(store, _items, _clock);
            progress.SubmitFinalScore(300);
            progress.Flush();
            progress.SubmitFinalScore(10);
            progress.Flush(); // backup now holds the "300, 1 game" version
            store.Corrupt(SaveKey.PlayerProgress, "{ not json");

            LogAssert.Expect(LogType.Warning, new Regex("Corrupt data"));
            LogAssert.Expect(LogType.Warning, new Regex("Restored 'player_progress' from its backup"));
            var reopened = new PlayerProgressService(store, _items, _clock);

            Assert.AreEqual(LoadOutcome.RecoveredFromBackup, reopened.LoadOutcome);
            Assert.AreEqual(300, reopened.HighScore);
            Assert.AreEqual(1, reopened.GamesPlayed);
            Assert.IsTrue(store.TryRead(SaveKey.PlayerProgress + "_corrupt", out var kept));
            Assert.AreEqual("{ not json", kept);
        }

        [Test]
        public void UnrecoverableSave_FallsBackToDefaultsWithoutOverwritingAtLoad()
        {
            var store = new InMemorySaveStore();
            store.Corrupt(SaveKey.PlayerProgress, "garbage");
            var writesBefore = store.WriteCount;

            LogAssert.Expect(LogType.Warning, new Regex("Corrupt data"));
            LogAssert.Expect(LogType.Warning, new Regex("unreadable; using defaults"));
            var progress = new PlayerProgressService(store, _items, _clock);

            Assert.AreEqual(LoadOutcome.ResetToDefaults, progress.LoadOutcome);
            Assert.AreEqual(0, progress.HighScore);
            Assert.AreEqual(writesBefore + 1, store.WriteCount, "Only the quarantine copy is written at load.");
            Assert.IsTrue(store.TryRead(SaveKey.PlayerProgress, out var main));
            Assert.AreEqual("garbage", main, "The damaged file is not replaced until the player makes progress.");
            Assert.IsTrue(progress.Flush(), "Nothing changed, so nothing is written.");
            Assert.AreEqual("garbage", store.TryRead(SaveKey.PlayerProgress, out var still) ? still : null);
        }

        [Test]
        public void WriteFailure_KeepsChangesAndRetries()
        {
            var store = new InMemorySaveStore();
            var progress = new PlayerProgressService(store, _items, _clock);
            progress.SubmitFinalScore(50);
            store.FailWrites = true;

            LogAssert.Expect(LogType.Warning, new Regex("Could not write 'player_progress'"));
            Assert.IsFalse(progress.Flush());

            store.FailWrites = false;
            Assert.IsTrue(progress.Flush());
            Assert.AreEqual(50, new PlayerProgressService(store, _items, _clock).HighScore);
        }

        [Test]
        public void Version1Save_MigratesWithNewFieldsDefaulted()
        {
            var store = new InMemorySaveStore();
            store.Write(SaveKey.PlayerProgress, "{\"schemaVersion\":1,\"highScore\":777,\"coins\":5,\"gamesPlayed\":4}");

            var progress = new PlayerProgressService(store, _items, _clock);

            Assert.AreEqual(LoadOutcome.Loaded, progress.LoadOutcome);
            Assert.AreEqual(777, progress.HighScore);
            Assert.AreEqual(4, progress.GamesPlayed);
            Assert.AreEqual(0, progress.TotalMerges);
            Assert.AreEqual(0, progress.DiscoveredItemIds.Count);
            Assert.AreEqual(-1, progress.LastPlayedDay);
        }

        [Test]
        public void SettingsAndProgress_ShareTheStoreWithoutInterfering()
        {
            var store = new InMemorySaveStore();
            var settings = new SettingsService(store, new SettingsData());
            var progress = new PlayerProgressService(store, _items, _clock);
            settings.MusicMuted = true;
            settings.SfxVolume = 0.3f;
            progress.SubmitFinalScore(90);
            settings.Flush();
            progress.Flush();

            var settingsAgain = new SettingsService(store, new SettingsData());
            var progressAgain = new PlayerProgressService(store, _items, _clock);

            Assert.IsTrue(settingsAgain.MusicMuted);
            Assert.AreEqual(0.3f, settingsAgain.SfxVolume, 0.0001f);
            Assert.AreEqual(90, progressAgain.HighScore);
        }

        // ---------- Statistics ----------

        [Test]
        public void BestScore_OnlyImprovesNeverRegresses()
        {
            var progress = new PlayerProgressService(new InMemorySaveStore(), _items, _clock);

            Assert.IsTrue(progress.SubmitFinalScore(400));
            Assert.IsFalse(progress.SubmitFinalScore(100));
            Assert.IsFalse(progress.SubmitFinalScore(400));

            Assert.AreEqual(400, progress.HighScore);
            Assert.AreEqual(3, progress.GamesPlayed);
        }

        [Test]
        public void EachRecordedMergeAndGameCountsOnce_WithOneEventEach()
        {
            var progress = new PlayerProgressService(new InMemorySaveStore(), _items, _clock);
            var merges = 0;
            var games = 0;
            progress.MergeRecorded += _ => merges++;
            progress.GameCompleted += _ => games++;

            progress.RecordMerge(_items.Get(1));
            progress.RecordMerge(_items.Get(1));
            progress.RecordMerge(null); // final-level clear
            progress.SubmitFinalScore(10);

            Assert.AreEqual(3, progress.TotalMerges);
            Assert.AreEqual(3, merges);
            Assert.AreEqual(1, progress.GamesPlayed);
            Assert.AreEqual(1, games);
        }

        // ---------- Collection ----------

        [Test]
        public void Discovery_HappensOncePerItemAndPersists()
        {
            var store = new InMemorySaveStore();
            var progress = new PlayerProgressService(store, _items, _clock);
            var discovered = 0;
            progress.ItemDiscovered += _ => discovered++;

            progress.RecordSeen(_items.Get(0));
            progress.RecordSeen(_items.Get(0));
            progress.RecordMerge(_items.Get(2));
            progress.RecordMerge(_items.Get(2));
            progress.Flush();

            Assert.AreEqual(2, discovered);
            Assert.AreEqual("l2", progress.HighestItemId);
            var reopened = new PlayerProgressService(store, _items, _clock);
            CollectionAssert.AreEquivalent(new[] { "l0", "l2" }, reopened.DiscoveredItemIds);
        }

        [Test]
        public void RemovedContentIds_AreKeptButIgnoredForRanking()
        {
            var store = new InMemorySaveStore();
            store.Write(SaveKey.PlayerProgress,
                "{\"schemaVersion\":2,\"discoveredItemIds\":[\"l1\",\"retired_item\",\"\"],\"highestItemId\":\"retired_item\"}");

            var progress = new PlayerProgressService(store, _items, _clock);

            Assert.IsTrue(progress.IsDiscovered("l1"));
            Assert.IsTrue(progress.IsDiscovered("retired_item"), "Unknown IDs are preserved in case the content returns.");
            Assert.AreEqual("l1", progress.HighestItemId, "Highest item is repaired from current content.");
            Assert.AreEqual(2, progress.DiscoveredItemIds.Count, "Blank IDs are dropped.");
        }

        // ---------- Streaks ----------

        [Test]
        public void Streak_FollowsCalendarDayRules()
        {
            var progress = new PlayerProgressService(new InMemorySaveStore(), _items, _clock);

            progress.SubmitFinalScore(1);
            progress.SubmitFinalScore(1); // same day
            Assert.AreEqual(1, progress.CurrentStreak);

            _clock.AdvanceDays(1);
            progress.SubmitFinalScore(1);
            Assert.AreEqual(2, progress.CurrentStreak);

            _clock.AdvanceDays(1);
            Assert.AreEqual(2, progress.DisplayedStreak(), "Not broken until a full day is missed.");

            _clock.AdvanceDays(2); // missed a day
            Assert.AreEqual(0, progress.DisplayedStreak());
            progress.SubmitFinalScore(1);
            Assert.AreEqual(1, progress.CurrentStreak);
            Assert.AreEqual(2, progress.BestStreak);
        }

        [Test]
        public void Streak_IgnoresClockMovedBackwards()
        {
            var progress = new PlayerProgressService(new InMemorySaveStore(), _items, _clock);
            progress.SubmitFinalScore(1);
            _clock.AdvanceDays(1);
            progress.SubmitFinalScore(1);

            _clock.AdvanceDays(-1);
            progress.SubmitFinalScore(1);
            _clock.AdvanceDays(1);
            progress.SubmitFinalScore(1);

            Assert.AreEqual(2, progress.CurrentStreak, "Going back and forth must not add days.");
        }

        [Test]
        public void DayKey_UsesUtcCalendarDays()
        {
            var lateUtc = new DateTime(2026, 10, 27, 23, 59, 0, DateTimeKind.Utc);
            Assert.AreEqual(DayKey.FromUtc(lateUtc), DayKey.FromUtc(new DateTime(2026, 10, 27, 0, 0, 0, DateTimeKind.Utc)));
            Assert.AreEqual(DayKey.FromUtc(lateUtc) + 1, DayKey.FromUtc(lateUtc.AddMinutes(1)));
        }

        // ---------- Daily challenges ----------

        private DailyChallengeCatalog Catalog(params DailyChallengeDefinition[] challenges)
        {
            var catalog = ScriptableObject.CreateInstance<DailyChallengeCatalog>();
            catalog.Set(challenges);
            return catalog;
        }

        [Test]
        public void Daily_MergeChallengeProgressesAndCompletesOnce()
        {
            var store = new InMemorySaveStore();
            var progress = new PlayerProgressService(store, _items, _clock);
            var catalog = Catalog(new DailyChallengeDefinition { id = "m3", title = "Merge 3", type = DailyChallengeType.MergesInDay, target = 3 });
            var daily = new DailyChallengeService(store, catalog, progress, _items, _clock, null);
            var completions = 0;
            daily.Completed += _ => completions++;

            for (var i = 0; i < 5; i++)
            {
                progress.RecordMerge(_items.Get(1));
            }

            Assert.IsTrue(daily.IsCompleted);
            Assert.AreEqual(3, daily.Progress);
            Assert.AreEqual(1, completions);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Daily_ProgressSurvivesRestartAndResetsOnTheNextDay()
        {
            var store = new InMemorySaveStore();
            var catalog = Catalog(new DailyChallengeDefinition { id = "m5", title = "Merge 5", type = DailyChallengeType.MergesInDay, target = 5 });
            var progress = new PlayerProgressService(store, _items, _clock);
            var daily = new DailyChallengeService(store, catalog, progress, _items, _clock, null);
            progress.RecordMerge(_items.Get(1));
            progress.RecordMerge(_items.Get(1));
            daily.Flush();
            daily.Dispose();

            var progressAgain = new PlayerProgressService(store, _items, _clock);
            var dailyAgain = new DailyChallengeService(store, catalog, progressAgain, _items, _clock, null);
            Assert.AreEqual(2, dailyAgain.Progress, "Same day: progress restored.");

            _clock.AdvanceDays(1);
            dailyAgain.Refresh();
            Assert.AreEqual(0, dailyAgain.Progress, "New UTC day: fresh challenge.");
            Assert.IsFalse(dailyAgain.IsCompleted);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Daily_ClockMovedBackDoesNotReopenOrRepeatADay()
        {
            var store = new InMemorySaveStore();
            var catalog = Catalog(new DailyChallengeDefinition { id = "g1", title = "Finish 1", type = DailyChallengeType.CompleteGames, target = 1 });
            var progress = new PlayerProgressService(store, _items, _clock);
            var daily = new DailyChallengeService(store, catalog, progress, _items, _clock, null);
            progress.SubmitFinalScore(5);
            var completedDay = daily.DayNumber;
            Assert.IsTrue(daily.IsCompleted);

            _clock.AdvanceDays(-3);
            daily.Refresh();

            Assert.AreEqual(completedDay, daily.DayNumber);
            Assert.IsTrue(daily.IsCompleted, "Moving the clock back must not reset today's challenge.");
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Daily_RewardIsClaimableExactlyOnceEvenAfterRestart()
        {
            var store = new InMemorySaveStore();
            var catalog = Catalog(new DailyChallengeDefinition
            {
                id = "s100", title = "Score 100", type = DailyChallengeType.ScoreInOneGame, target = 100,
                reward = new RewardDefinition { type = RewardType.Coins, amount = 25 },
            });
            var progress = new PlayerProgressService(store, _items, _clock);
            var daily = new DailyChallengeService(store, catalog, progress, _items, _clock, new ProgressRewardGranter(progress));

            Assert.IsFalse(daily.TryClaim(), "Not completed yet.");
            progress.SubmitFinalScore(150);
            Assert.IsTrue(daily.CanClaim);
            Assert.IsTrue(daily.TryClaim());
            Assert.IsFalse(daily.TryClaim());
            Assert.AreEqual(25, progress.Coins);
            progress.Flush();
            daily.Dispose();

            var progressAgain = new PlayerProgressService(store, _items, _clock);
            var dailyAgain = new DailyChallengeService(store, catalog, progressAgain, _items, _clock, new ProgressRewardGranter(progressAgain));
            Assert.IsTrue(dailyAgain.IsClaimed);
            Assert.IsFalse(dailyAgain.TryClaim());
            Assert.AreEqual(25, progressAgain.Coins);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Daily_WithoutRewardCompletesButIsNotClaimable()
        {
            var store = new InMemorySaveStore();
            var catalog = Catalog(new DailyChallengeDefinition { id = "c", title = "Create l2", type = DailyChallengeType.CreateItem, itemId = "l2" });
            var progress = new PlayerProgressService(store, _items, _clock);
            var daily = new DailyChallengeService(store, catalog, progress, _items, _clock, new ProgressRewardGranter(progress));

            progress.RecordMerge(_items.Get(1));
            Assert.IsFalse(daily.IsCompleted, "A lower item doesn't count.");
            progress.RecordMerge(_items.Get(3));

            Assert.IsTrue(daily.IsCompleted, "Creating a higher item counts.");
            Assert.IsFalse(daily.CanClaim);
            Assert.IsFalse(daily.TryClaim());
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Daily_InvalidOrUnknownContentIsSkipped()
        {
            var catalog = Catalog(
                new DailyChallengeDefinition { id = "bad_item", title = "x", type = DailyChallengeType.CreateItem, itemId = "removed" },
                new DailyChallengeDefinition { id = "", title = "no id", type = DailyChallengeType.MergesInDay, target = 1 });
            LogAssert.Expect(LogType.Warning, new Regex("bad_item"));
            LogAssert.Expect(LogType.Warning, new Regex("invalid"));

            var daily = new DailyChallengeService(new InMemorySaveStore(), catalog, new PlayerProgressService(new InMemorySaveStore(), _items, _clock), _items, _clock, null);

            Assert.IsNull(daily.Current);
            Assert.IsFalse(daily.IsCompleted);
            Object.DestroyImmediate(catalog);
        }

        // ---------- Achievements ----------

        private AchievementCatalog Achievements(params AchievementDefinition[] achievements)
        {
            var catalog = ScriptableObject.CreateInstance<AchievementCatalog>();
            catalog.Set(achievements);
            return catalog;
        }

        [Test]
        public void Achievement_UnlocksOnceNotifiesOnceAndPersists()
        {
            var store = new InMemorySaveStore();
            var catalog = Achievements(new AchievementDefinition
            {
                id = "first", title = "First", type = AchievementType.CompleteGames, target = 1,
                reward = new RewardDefinition { type = RewardType.Coins, amount = 10 },
            });
            var progress = new PlayerProgressService(store, _items, _clock);
            var achievements = new AchievementService(store, catalog, progress, _items, new ProgressRewardGranter(progress));
            var notifications = 0;
            achievements.Unlocked += _ => notifications++;

            progress.SubmitFinalScore(5);
            progress.SubmitFinalScore(5);

            Assert.IsTrue(achievements.IsUnlocked("first"));
            Assert.AreEqual(1, notifications);
            Assert.AreEqual(10, progress.Coins, "Reward granted exactly once.");
            achievements.Flush();
            progress.Flush();
            achievements.Dispose();

            var progressAgain = new PlayerProgressService(store, _items, _clock);
            var again = new AchievementService(store, catalog, progressAgain, _items, new ProgressRewardGranter(progressAgain));
            var renotified = 0;
            again.Unlocked += _ => renotified++;
            progressAgain.SubmitFinalScore(5);

            Assert.IsTrue(again.IsUnlocked("first"));
            Assert.AreEqual(0, renotified);
            Assert.AreEqual(10, progressAgain.Coins);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Achievement_AlreadyEarnedStatsUnlockSilentlyAtStartup()
        {
            var store = new InMemorySaveStore();
            store.Write(SaveKey.PlayerProgress, "{\"schemaVersion\":2,\"totalMerges\":60}");
            var catalog = Achievements(new AchievementDefinition { id = "m50", title = "50", type = AchievementType.TotalMerges, target = 50 });
            var progress = new PlayerProgressService(store, _items, _clock);

            var achievements = new AchievementService(store, catalog, progress, _items, null);

            Assert.IsTrue(achievements.IsUnlocked("m50"));
            Assert.AreEqual(50, achievements.ProgressOf(catalog.All[0]));
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Achievement_ProgressComesFromRealStatistics()
        {
            var catalog = Achievements(
                new AchievementDefinition { id = "m3", title = "3 merges", type = AchievementType.TotalMerges, target = 3 },
                new AchievementDefinition { id = "d4", title = "Find l4", type = AchievementType.DiscoverItem, itemId = "l4" });
            var progress = new PlayerProgressService(new InMemorySaveStore(), _items, _clock);
            var achievements = new AchievementService(new InMemorySaveStore(), catalog, progress, _items, null);

            progress.RecordMerge(_items.Get(1));
            progress.RecordMerge(_items.Get(1));

            Assert.AreEqual(2, achievements.ProgressOf(catalog.All[0]));
            Assert.AreEqual(0, achievements.ProgressOf(catalog.All[1]));
            Assert.AreEqual(0, achievements.UnlockedCount);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Achievement_UnknownItemOrDuplicateIdIsSkipped()
        {
            var catalog = Achievements(
                new AchievementDefinition { id = "gone", title = "Gone", type = AchievementType.DiscoverItem, itemId = "removed" },
                new AchievementDefinition { id = "dup", title = "A", type = AchievementType.CompleteGames, target = 1 },
                new AchievementDefinition { id = "dup", title = "B", type = AchievementType.CompleteGames, target = 2 });
            LogAssert.Expect(LogType.Warning, new Regex("unknown item 'removed'"));
            LogAssert.Expect(LogType.Warning, new Regex("'dup' is used twice"));

            var achievements = new AchievementService(new InMemorySaveStore(), catalog, new PlayerProgressService(new InMemorySaveStore(), _items, _clock), _items, null);

            Assert.AreEqual(1, achievements.Definitions.Count);
            Assert.AreEqual("A", achievements.Definitions[0].title);
            Object.DestroyImmediate(catalog);
        }
    }
}
