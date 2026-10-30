using System;
using System.Collections.Generic;
using System.Linq;
using DesiMerge.Audio;
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
    public class RewardSystemsTests
    {
        private static readonly DateTime Wednesday = new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);

        private readonly List<Object> _created = new List<Object>();
        private TestContent _content;
        private MergeProgression _items;
        private PowerUpCatalog _powerUpCatalog;
        private ManualClock _clock;
        private InMemorySaveStore _store;
        private PlayerProgressService _progress;
        private PowerUpService _powerUps;
        private ProgressRewardGranter _granter;

        [SetUp]
        public void SetUp()
        {
            _content = new TestContent();
            _items = _content.Progression(levels: 5, spawnable: 2);
            _powerUpCatalog = Track(ScriptableObject.CreateInstance<PowerUpCatalog>());
            _powerUpCatalog.Set(new[]
            {
                new PowerUpDefinition { id = "hammer", title = "Hammer", kind = PowerUpKind.Hammer, freeUsesPerGame = 1, maxSavedCharges = 3 },
                new PowerUpDefinition { id = "shuffle", title = "Shuffle", kind = PowerUpKind.Shuffle, freeUsesPerGame = 1, maxSavedCharges = 3 },
            });
            _clock = new ManualClock(Wednesday);
            _store = new InMemorySaveStore();
            Reopen();
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

        /// <summary>Simulates an app restart: every service is rebuilt from the same save store.</summary>
        private void Reopen()
        {
            _progress = new PlayerProgressService(_store, _items, _clock);
            _powerUps = new PowerUpService(_store, _powerUpCatalog);
            _granter = new ProgressRewardGranter(_progress, _powerUps);
        }

        private static RewardDefinition Hammer(int amount = 1) => new RewardDefinition { type = RewardType.PowerUpCharges, amount = amount, powerUpId = "hammer" };
        private static RewardDefinition Shuffle(int amount = 1) => new RewardDefinition { type = RewardType.PowerUpCharges, amount = amount, powerUpId = "shuffle" };

        // ---------- Daily rewards ----------

        private DailyRewardCatalog Calendar(int days = 7)
        {
            var catalog = Track(ScriptableObject.CreateInstance<DailyRewardCatalog>());
            catalog.Set(Enumerable.Range(0, days).Select(i => new DailyRewardDay { title = "Day " + (i + 1), rewards = new[] { i % 2 == 0 ? Hammer() : Shuffle() } }).ToArray());
            return catalog;
        }

        private DailyRewardService Daily(DailyRewardCatalog catalog) => new DailyRewardService(_store, catalog, _clock, _granter);

        [Test]
        public void DailyReward_FirstClaim_GrantsDayOneExactlyOnce_AndSurvivesRestart()
        {
            var calendar = Calendar();
            var daily = Daily(calendar);
            Assert.IsTrue(daily.CanClaim);
            Assert.AreEqual(0, daily.TodayIndex);
            Assert.AreEqual(DailyRewardDayState.Available, daily.StateOf(0));

            Assert.AreEqual(ClaimResult.Claimed, daily.TryClaim());
            Assert.AreEqual(1, _powerUps.SavedCharges("hammer"));
            Assert.AreEqual(ClaimResult.NotAvailable, daily.TryClaim(), "Rapid second tap");
            Assert.AreEqual(1, _powerUps.SavedCharges("hammer"));
            Assert.AreEqual(1, daily.Streak);

            Reopen();
            var reopened = Daily(calendar);
            Assert.IsTrue(reopened.ClaimedToday);
            Assert.IsFalse(reopened.CanClaim);
            Assert.AreEqual(DailyRewardDayState.Claimed, reopened.StateOf(0));
            Assert.AreEqual(ClaimResult.NotAvailable, reopened.TryClaim());
            Assert.AreEqual(1, _powerUps.SavedCharges("hammer"));
        }

        [Test]
        public void DailyReward_ConsecutiveDays_MoveAlongTheCalendar_AndBuildTheStreak()
        {
            var daily = Daily(Calendar());
            daily.TryClaim();
            _clock.AdvanceDays(1);
            daily.Refresh();

            Assert.AreEqual(1, daily.TodayIndex);
            Assert.AreEqual(DailyRewardDayState.Claimed, daily.StateOf(0));
            Assert.AreEqual(DailyRewardDayState.Available, daily.StateOf(1));
            Assert.AreEqual(DailyRewardDayState.Upcoming, daily.StateOf(2));
            Assert.AreEqual(ClaimResult.Claimed, daily.TryClaim());
            Assert.AreEqual(2, daily.Streak);
            Assert.AreEqual(1, _powerUps.SavedCharges("shuffle"));
        }

        [Test]
        public void DailyReward_MissedDay_RestartsAtDayOne_AndKeepsTheBestStreak()
        {
            var daily = Daily(Calendar());
            daily.TryClaim();
            _clock.AdvanceDays(1);
            daily.TryClaim();
            _clock.AdvanceDays(2);
            daily.Refresh();

            Assert.AreEqual(0, daily.TodayIndex, "A missed day restarts the calendar.");
            Assert.AreEqual(0, daily.Streak);
            Assert.AreEqual(DailyRewardDayState.Upcoming, daily.StateOf(1));
            Assert.AreEqual(ClaimResult.Claimed, daily.TryClaim());
            Assert.AreEqual(1, daily.Streak);
            Assert.AreEqual(2, daily.BestStreak);
        }

        [Test]
        public void DailyReward_WrapsToDayOne_AfterTheLastDay()
        {
            var daily = Daily(Calendar(days: 3));
            for (var day = 0; day < 3; day++)
            {
                Assert.AreEqual(day, daily.TodayIndex);
                Assert.AreEqual(ClaimResult.Claimed, daily.TryClaim());
                _clock.AdvanceDays(1);
                daily.Refresh();
            }

            Assert.AreEqual(0, daily.TodayIndex);
            Assert.IsTrue(daily.CanClaim);
            Assert.AreEqual(3, daily.Streak, "The streak continues across calendar cycles.");
        }

        [Test]
        public void DailyReward_ClockMovedBack_NeverReopensAClaimedDay()
        {
            var calendar = Calendar();
            var daily = Daily(calendar);
            daily.TryClaim();
            _clock.AdvanceDays(-3);
            daily.Refresh();

            Assert.IsTrue(daily.ClockMovedBack);
            Assert.IsFalse(daily.CanClaim);
            Assert.AreEqual(ClaimResult.NotAvailable, daily.TryClaim());
            Reopen();
            Assert.IsFalse(Daily(calendar).CanClaim);
            Assert.AreEqual(1, _powerUps.SavedCharges("hammer"));
        }

        [Test]
        public void DailyReward_FullStorage_ClaimsNothing_AndCanBeClaimedLater()
        {
            Assert.IsTrue(_powerUps.TryGrant("setup", "hammer", 3));
            var daily = Daily(Calendar());

            Assert.AreEqual(ClaimResult.StorageFull, daily.TryClaim());
            Assert.IsTrue(daily.CanClaim, "The day stays claimable.");
            Assert.AreEqual(3, _powerUps.SavedCharges("hammer"));

            Assert.IsTrue(_powerUps.TryConsume("hammer"));
            Assert.IsTrue(_powerUps.TryConsume("hammer"));
            Assert.AreEqual(2, _powerUps.SavedCharges("hammer"));
            Assert.AreEqual(ClaimResult.Claimed, daily.TryClaim());
            Assert.AreEqual(3, _powerUps.SavedCharges("hammer"));
        }

        [Test]
        public void DailyReward_ClaimInterruptedAfterTheGrant_IsNotPaidTwice()
        {
            var daily = Daily(Calendar());
            var prefix = DailyRewardService.GrantPrefixFor(DayKey.Today(_clock));
            Assert.IsTrue(_granter.Grant(RewardClaims.GrantId(prefix, 0), Hammer()));
            Assert.AreEqual(1, _powerUps.SavedCharges("hammer"));

            Assert.AreEqual(ClaimResult.Claimed, daily.TryClaim());
            Assert.AreEqual(1, _powerUps.SavedCharges("hammer"), "The already-applied grant is recognised.");
        }

        // ---------- Missions ----------

        private MissionCatalog Missions(params MissionDefinition[] missions)
        {
            var catalog = Track(ScriptableObject.CreateInstance<MissionCatalog>());
            var daily = missions.Count(m => m.period == MissionPeriod.Daily);
            var weekly = missions.Count(m => m.period == MissionPeriod.Weekly);
            catalog.Set(missions, Mathf.Max(1, daily), Mathf.Max(1, weekly));
            return catalog;
        }

        private static MissionDefinition Mission(string id, MissionType type, long target, MissionPeriod period = MissionPeriod.Daily, string itemId = "", string powerUpId = "") =>
            new MissionDefinition { id = id, title = id, description = id, type = type, target = target, period = period, itemId = itemId, powerUpId = powerUpId, reward = Hammer() };

        private MissionService MissionsService(MissionCatalog catalog) => new MissionService(_store, catalog, _progress, _powerUps, _items, _clock, _granter);

        private static MissionState State(IMissionService missions, string id) => missions.Daily.Concat(missions.Weekly).Single(state => state.Definition.id == id);

        [Test]
        public void Missions_SelectionIsDeterministic_DistinctAndRotates()
        {
            var pool = Enumerable.Range(0, 5).Select(i => Mission("m" + i, MissionType.Merges, 1)).ToList();
            var first = MissionService.SelectFor(100, pool, 3).Select(m => m.id).ToList();
            CollectionAssert.AreEqual(first, MissionService.SelectFor(100, pool, 3).Select(m => m.id).ToList());
            CollectionAssert.AllItemsAreUnique(first);
            CollectionAssert.AreNotEqual(first, MissionService.SelectFor(101, pool, 3).Select(m => m.id).ToList());
            Assert.AreEqual(2, MissionService.SelectFor(7, pool.Take(2).ToList(), 3).Count, "Never more missions than the pool has.");
        }

        [Test]
        public void Missions_MergeProgress_CompletesOnce_AndStopsCounting()
        {
            var missions = MissionsService(Missions(Mission("merge3", MissionType.Merges, 3)));
            var completed = 0;
            missions.Completed += _ => completed++;

            for (var i = 0; i < 5; i++)
            {
                _progress.RecordMerge(_items.Get(1));
            }

            var state = State(missions, "merge3");
            Assert.AreEqual(3, state.Progress);
            Assert.IsTrue(state.Completed);
            Assert.IsTrue(state.CanClaim);
            Assert.AreEqual(1, completed, "Completion is announced once.");
            Assert.AreEqual(1, missions.ClaimableCount);
        }

        [Test]
        public void Missions_RewardIsClaimedExactlyOnce_AndTheClaimIsSaved()
        {
            var catalog = Missions(Mission("play1", MissionType.CompleteGames, 1));
            var missions = MissionsService(catalog);
            Assert.AreEqual(ClaimResult.NotAvailable, missions.TryClaim("play1"), "Not completed yet.");
            _progress.SubmitFinalScore(10);

            Assert.AreEqual(ClaimResult.Claimed, missions.TryClaim("play1"));
            Assert.AreEqual(ClaimResult.NotAvailable, missions.TryClaim("play1"));
            Assert.AreEqual(1, _powerUps.SavedCharges("hammer"));

            missions.Dispose();
            Reopen();
            var reopened = MissionsService(catalog);
            Assert.IsTrue(State(reopened, "play1").Claimed);
            Assert.AreEqual(ClaimResult.NotAvailable, reopened.TryClaim("play1"));
            Assert.AreEqual(1, _powerUps.SavedCharges("hammer"));
        }

        [Test]
        public void Missions_DailyResetsEachDay_WeeklyResetsOnMonday()
        {
            var missions = MissionsService(Missions(Mission("d", MissionType.Merges, 10), Mission("w", MissionType.Merges, 100, MissionPeriod.Weekly)));
            _progress.RecordMerge(_items.Get(1));
            _progress.RecordMerge(_items.Get(1));

            _clock.AdvanceDays(1);
            missions.Refresh();
            Assert.AreEqual(0, State(missions, "d").Progress, "Thursday: new daily missions.");
            Assert.AreEqual(2, State(missions, "w").Progress, "Same week: weekly progress kept.");

            _clock.AdvanceDays(4);
            missions.Refresh();
            Assert.AreEqual(DayOfWeek.Monday, _clock.UtcNow.DayOfWeek);
            Assert.AreEqual(0, State(missions, "w").Progress, "Monday: new weekly missions.");
        }

        [Test]
        public void Missions_ScoreCreateAndPowerUpTypes_UseRealEvents()
        {
            var missions = MissionsService(Missions(
                Mission("score", MissionType.ScoreInOneGame, 300),
                Mission("create", MissionType.CreateItem, 1, itemId: "l2"),
                Mission("hammer", MissionType.UsePowerUps, 2, powerUpId: "hammer"),
                Mission("clean", MissionType.GamesWithoutPowerUps, 1)));

            _progress.RecordMerge(_items.Get(1));
            Assert.IsFalse(State(missions, "create").Completed);
            _progress.RecordMerge(_items.Get(3));
            Assert.IsTrue(State(missions, "create").Completed, "A higher item also counts.");

            _powerUps.BeginGame();
            _powerUps.TryConsume("shuffle");
            _powerUps.TryConsume("hammer");
            Assert.AreEqual(1, State(missions, "hammer").Progress, "Only Hammer uses count.");
            _progress.SubmitFinalScore(250);
            Assert.AreEqual(250, State(missions, "score").Progress);
            Assert.AreEqual(0, State(missions, "clean").Progress, "That game used power-ups.");

            _powerUps.BeginGame();
            _progress.SubmitFinalScore(320);
            Assert.IsTrue(State(missions, "score").Completed);
            Assert.IsTrue(State(missions, "clean").Completed);
        }

        [Test]
        public void Missions_AfterDispose_EventsNoLongerCount()
        {
            var missions = MissionsService(Missions(Mission("merge", MissionType.Merges, 10)));
            _progress.RecordMerge(_items.Get(1));
            missions.Dispose();
            _progress.RecordMerge(_items.Get(1));
            Assert.AreEqual(1, State(missions, "merge").Progress, "No leftover subscription after dispose.");
        }

        // ---------- Milestones ----------

        private MilestoneCatalog Ranks()
        {
            var catalog = Track(ScriptableObject.CreateInstance<MilestoneCatalog>());
            catalog.Set(new[]
            {
                new MilestoneDefinition { id = "r500", title = "Five Hundred", score = 500, reward = Shuffle() },
                new MilestoneDefinition { id = "r100", title = "Hundred", score = 100, reward = Hammer() },
            });
            return catalog;
        }

        [Test]
        public void Milestones_UnlockAtTheExactThreshold_OnlyOnce()
        {
            var milestones = new MilestoneService(_store, Ranks(), _progress, _granter);
            var reached = new List<string>();
            milestones.Reached += m => reached.Add(m.id);

            _progress.SubmitFinalScore(99);
            Assert.IsFalse(milestones.IsReached("r100"));
            Assert.IsNull(milestones.Current);
            Assert.AreEqual("r100", milestones.Next.id, "Definitions are sorted by score.");
            Assert.AreEqual(0.99f, milestones.ProgressToNext, 0.001f);

            _progress.SubmitFinalScore(100);
            _progress.SubmitFinalScore(150);
            CollectionAssert.AreEqual(new[] { "r100" }, reached);
            Assert.AreEqual("r100", milestones.Current.id);
            Assert.AreEqual(0.125f, milestones.ProgressToNext, 0.001f);
        }

        [Test]
        public void Milestones_ExistingBestScore_IsHonouredAtStartup_WithoutResettingIt()
        {
            _progress.SubmitFinalScore(640);
            _progress.Flush();
            Reopen();
            var milestones = new MilestoneService(_store, Ranks(), _progress, _granter);

            Assert.AreEqual(640, _progress.HighScore);
            Assert.IsTrue(milestones.IsReached("r100"));
            Assert.IsTrue(milestones.IsReached("r500"));
            Assert.AreEqual(2, milestones.ClaimableCount);
            Assert.IsNull(milestones.Next);
        }

        [Test]
        public void Milestones_RewardIsClaimedOnce_AndPersisted()
        {
            var catalog = Ranks();
            var milestones = new MilestoneService(_store, catalog, _progress, _granter);
            Assert.AreEqual(ClaimResult.NotAvailable, milestones.TryClaim("r100"));
            _progress.SubmitFinalScore(120);
            Assert.AreEqual(ClaimResult.Claimed, milestones.TryClaim("r100"));
            Assert.AreEqual(ClaimResult.NotAvailable, milestones.TryClaim("r100"));
            Assert.AreEqual(1, _powerUps.SavedCharges("hammer"));
            milestones.Flush();
            milestones.Dispose();

            Reopen();
            var reopened = new MilestoneService(_store, catalog, _progress, _granter);
            Assert.IsTrue(reopened.IsClaimed("r100"));
            Assert.AreEqual(0, reopened.ClaimableCount);
        }

        // ---------- Achievements and collection ----------

        [Test]
        public void AchievementReward_IsAppliedOnceAtUnlock_EvenAfterRestart()
        {
            var catalog = Track(ScriptableObject.CreateInstance<AchievementCatalog>());
            catalog.Set(new[] { new AchievementDefinition { id = "first_merge", title = "First", type = AchievementType.TotalMerges, target = 1, reward = Hammer(2) } });
            var achievements = new AchievementService(_store, catalog, _progress, _items, _granter);
            var unlocked = 0;
            achievements.Unlocked += _ => unlocked++;

            _progress.RecordMerge(_items.Get(1));
            _progress.RecordMerge(_items.Get(1));
            Assert.AreEqual(1, unlocked);
            Assert.AreEqual(2, _powerUps.SavedCharges("hammer"));
            _progress.Flush();
            achievements.Flush();
            achievements.Dispose();

            Reopen();
            var reopened = new AchievementService(_store, catalog, _progress, _items, _granter);
            Assert.IsTrue(reopened.IsUnlocked("first_merge"));
            Assert.AreEqual(2, _powerUps.SavedCharges("hammer"), "Not granted again after a restart.");
        }

        [Test]
        public void Collection_FirstDiscovery_IsReportedOnce_AndPersisted()
        {
            var discovered = new List<string>();
            _progress.ItemDiscovered += item => discovered.Add(item.Id);
            _progress.RecordMerge(_items.Get(2));
            _progress.RecordMerge(_items.Get(2));
            CollectionAssert.AreEqual(new[] { "l2" }, discovered);
            _progress.Flush();

            Reopen();
            Assert.IsTrue(_progress.IsDiscovered("l2"));
        }

        // ---------- Audio and settings ----------

        [Test]
        public void AudioService_ProvidesExactlyOneListener_OnItsHost()
        {
            var host = Track(new GameObject("AudioHost"));
            var library = Track(ScriptableObject.CreateInstance<AudioLibrary>());
            library.Set(Array.Empty<AudioLibrary.SfxEntry>(), Array.Empty<AudioLibrary.MusicEntry>());
            var settings = new SettingsService(new InMemorySaveStore(), new SettingsData());

            var audio = new AudioService(host, library, settings);
            var again = new AudioService(host, library, settings);

            Assert.IsNotNull(audio.Listener);
            Assert.IsTrue(audio.Listener.enabled);
            Assert.AreEqual(1, host.GetComponents<AudioListener>().Length);
            Assert.AreSame(audio.Listener, again.Listener);
            audio.Dispose();
            again.Dispose();
        }

        [Test]
        public void AudioService_MusicVolumeAndMute_ApplyImmediately()
        {
            var host = Track(new GameObject("AudioHost"));
            var clip = Track(AudioClip.Create("loop", 4410, 1, 44100, false));
            var library = Track(ScriptableObject.CreateInstance<AudioLibrary>());
            library.Set(Array.Empty<AudioLibrary.SfxEntry>(), new[] { new AudioLibrary.MusicEntry { track = MusicTrack.Menu, clip = clip, volume = 0.5f } });
            var settings = new SettingsService(new InMemorySaveStore(), new SettingsData { musicVolume = 1f });
            var audio = new AudioService(host, library, settings);

            audio.PlayMusic(MusicTrack.Menu);
            Assert.AreEqual(0.5f, audio.MusicSource.volume, 0.0001f);
            settings.MusicVolume = 0.4f;
            Assert.AreEqual(0.2f, audio.MusicSource.volume, 0.0001f);
            settings.MusicMuted = true;
            Assert.AreEqual(0f, audio.MusicSource.volume);
            settings.MusicMuted = false;
            Assert.AreEqual(0.2f, audio.MusicSource.volume, 0.0001f);
            audio.Dispose();
        }

        [Test]
        public void AudioLibrary_ShippedLibrary_HasAClipForEveryCue()
        {
            var library = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioLibrary>("Assets/_Project/Audio/AudioLibrary.asset");
            Assert.IsNotNull(library);
            foreach (SfxCue cue in Enum.GetValues(typeof(SfxCue)))
            {
                Assert.IsTrue(library.TryGetSfx(cue, out var clip, out var volume), $"No clip for {cue}");
                Assert.Greater(volume, 0f, cue.ToString());
                Assert.Greater(clip.length, 0f, cue.ToString());
            }

            foreach (var track in new[] { MusicTrack.Menu, MusicTrack.Gameplay })
            {
                Assert.IsTrue(library.TryGetMusic(track, out _, out _), $"No music for {track}");
            }
        }

        [Test]
        public void Settings_TutorialFlag_PersistsAndOldSavesLoadWithItUnset()
        {
            var store = new InMemorySaveStore();
            store.Write(SaveKey.Settings, "{\"schemaVersion\":2,\"musicVolume\":0.3,\"sfxVolume\":0.6,\"musicMuted\":true,\"sfxMuted\":false}");
            var settings = new SettingsService(store, new SettingsData());
            Assert.IsFalse(settings.TutorialCompleted);
            Assert.AreEqual(0.3f, settings.MusicVolume, 0.0001f);
            Assert.IsTrue(settings.MusicMuted);

            settings.TutorialCompleted = true;
            settings.Flush();
            var reopened = new SettingsService(store, new SettingsData());
            Assert.IsTrue(reopened.TutorialCompleted);
            Assert.AreEqual(0.6f, reopened.SfxVolume, 0.0001f);
        }

        [Test]
        public void ShippedCatalogs_AreValid_AndOnlyUseRealPowerUps()
        {
            T Load<T>(string path) where T : Object => UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            var items = Load<MergeProgression>("Assets/_Project/ScriptableObjects/MergeProgression.asset");
            var powerUps = Load<PowerUpCatalog>("Assets/_Project/ScriptableObjects/Progression/PowerUps.asset");
            var calendar = Load<DailyRewardCatalog>("Assets/_Project/ScriptableObjects/Progression/DailyRewards.asset");
            var missions = Load<MissionCatalog>("Assets/_Project/ScriptableObjects/Progression/Missions.asset");
            var ranks = Load<MilestoneCatalog>("Assets/_Project/ScriptableObjects/Progression/Milestones.asset");
            Assert.IsNotNull(calendar);
            Assert.IsNotNull(missions);
            Assert.IsNotNull(ranks);
            var ids = new HashSet<string>(powerUps.PowerUps.Select(p => p.id));

            void CheckReward(RewardDefinition reward, string owner)
            {
                Assert.AreEqual(RewardType.PowerUpCharges, reward.type, owner + ": only power-up charges are real rewards (no coin economy).");
                Assert.IsTrue(ids.Contains(reward.powerUpId), owner);
                Assert.Greater(reward.amount, 0, owner);
            }

            Assert.AreEqual(7, calendar.Days.Count);
            foreach (var day in calendar.Days)
            {
                Assert.IsTrue(DailyRewardCatalog.IsValid(day));
                foreach (var reward in day.rewards)
                {
                    CheckReward(reward, day.title);
                }
            }

            Assert.AreEqual(missions.All.Count, missions.All.Select(m => m.id).Distinct().Count(), "Mission IDs are unique.");
            foreach (var mission in missions.All)
            {
                Assert.IsTrue(MissionCatalog.IsValid(mission, items), mission.id);
                CheckReward(mission.reward, mission.id);
            }

            Assert.GreaterOrEqual(missions.All.Count(m => m.period == MissionPeriod.Daily), missions.DailySlots);
            Assert.GreaterOrEqual(missions.All.Count(m => m.period == MissionPeriod.Weekly), missions.WeeklySlots);
            foreach (var rank in ranks.All)
            {
                Assert.IsTrue(MilestoneCatalog.IsValid(rank), rank.id);
                CheckReward(rank.reward, rank.id);
            }
        }
    }
}
