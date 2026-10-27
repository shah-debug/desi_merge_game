using DesiMerge.Persistence;
using DesiMerge.Progression;
using DesiMerge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.Tests.PlayMode
{
    /// <summary>Gameplay → progression wiring with real physics merges, the in-memory store and a manual clock.</summary>
    public class ProgressionIntegrationTests
    {
        private GameplayTestRig _rig;

        [SetUp]
        public void SetUp()
        {
            _rig = new GameplayTestRig();
            _rig.CreateLoop();
        }

        [TearDown]
        public void TearDown() => _rig.Dispose();

        private void MergeOnePair(float x = 0f)
        {
            _rig.SpawnOnFloor(0, x - 0.25f);
            _rig.SpawnOnFloor(0, x + 0.25f);
            _rig.Simulate(5);
        }

        [Test]
        public void PhysicsMerge_IsRecordedExactlyOnce()
        {
            MergeOnePair();

            Assert.AreEqual(1, _rig.Services.Progress.TotalMerges);
            Assert.IsTrue(_rig.Services.Progress.IsDiscovered("l1"));
            _rig.Simulate(20);
            Assert.AreEqual(1, _rig.Services.Progress.TotalMerges, "Later physics steps must not record the merge again.");
        }

        [Test]
        public void Drop_DiscoversTheDroppedItem()
        {
            var level = _rig.Loop.Queue.Current;

            _rig.Loop.TryDrop(0f);

            Assert.IsTrue(_rig.Services.Progress.IsDiscovered(_rig.Progression.Get(level).Id));
        }

        [Test]
        public void GameOver_CountsOneCompletedGameAndBestScore()
        {
            MergeOnePair();
            var score = _rig.Loop.Session.Score;

            _rig.Loop.EndGame();
            _rig.Loop.EndGame();

            var progress = _rig.Services.Progress;
            Assert.AreEqual(1, progress.GamesPlayed);
            Assert.AreEqual(score, progress.HighScore);
            Assert.AreEqual(1, progress.CurrentStreak);
        }

        [Test]
        public void Restart_KeepsLongTermProgress()
        {
            MergeOnePair();
            _rig.Loop.EndGame();
            var best = _rig.Services.Progress.HighScore;

            _rig.Loop.Restart();

            Assert.AreEqual(0, _rig.Loop.Session.Score, "Session state resets...");
            Assert.AreEqual(best, _rig.Services.Progress.HighScore, "...long-term records don't.");
            Assert.AreEqual(1, _rig.Services.Progress.GamesPlayed);
            Assert.AreEqual(1, _rig.Services.Progress.TotalMerges);
        }

        [Test]
        public void LeavingToMenu_DoesNotFinalizeTheGame()
        {
            MergeOnePair();

            _rig.Loop.ReturnToMainMenu();
            _rig.Loop.ReturnToMainMenu();

            Assert.AreEqual(0, _rig.Services.Progress.GamesPlayed);
            Assert.AreEqual(0, _rig.Services.Progress.HighScore);
            Assert.AreEqual(1, _rig.Services.Progress.TotalMerges, "Merges that really happened still count.");
            Assert.IsTrue(_rig.Store.TryRead(SaveKey.PlayerProgress, out _), "Progress is saved when leaving.");
        }

        [Test]
        public void Toasts_ShownOncePerDiscoveryAchievementAndDailyCompletion()
        {
            MergeOnePair(-2f);
            MergeOnePair(2f);

            var toasts = _rig.View.Toasts;
            Assert.AreEqual(1, toasts.FindAll(t => t.Contains("L1")).Count, "Discovery toast once.");
            Assert.AreEqual(1, toasts.FindAll(t => t.Contains("Two Merges")).Count, "Achievement toast once.");
            Assert.AreEqual(1, toasts.FindAll(t => t.Contains("Daily challenge complete")).Count, "Daily toast once.");
        }

        [Test]
        public void AppRestart_RestoresProgressAchievementsAndDaily()
        {
            MergeOnePair(-2f);
            MergeOnePair(2f);
            _rig.Loop.EndGame();
            _rig.Loop.Dispose();
            _rig.Services.Dispose();

            _rig.Services = _rig.BuildServices();

            var services = _rig.Services;
            Assert.AreEqual(1, services.Progress.GamesPlayed);
            Assert.AreEqual(2, services.Progress.TotalMerges);
            Assert.IsTrue(services.Achievements.IsUnlocked("first_game"));
            Assert.IsTrue(services.Achievements.IsUnlocked("merges_2"));
            Assert.IsTrue(services.DailyChallenges.IsCompleted);
        }

        [Test]
        public void SettingsStillPersistAlongsideProgression()
        {
            _rig.Services.Settings.MusicVolume = 0.35f;
            _rig.Loop.EndGame(); // flushes progress
            _rig.Services.FlushAll();

            var reopened = _rig.BuildServices();

            Assert.AreEqual(0.35f, reopened.Settings.MusicVolume, 0.0001f);
            Assert.AreEqual(1, reopened.Progress.GamesPlayed);
            reopened.Dispose();
        }
    }

    /// <summary>Progression UI shows only what the services report.</summary>
    public class ProgressionUiTests
    {
        private GameplayTestRig _rig;
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            _rig = new GameplayTestRig();
            _root = new GameObject("UiRoot", typeof(RectTransform));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            _rig.Dispose();
        }

        private T Child<T>(string name, Transform parent = null) where T : Component
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent != null ? parent : _root.transform, false);
            // RectTransform already exists on the new object; adding a second one fails.
            return go.TryGetComponent<T>(out var existing) ? existing : go.AddComponent<T>();
        }

        [Test]
        public void Collection_ShowsDiscoveredAndLockedFromSavedData()
        {
            _rig.Services.Progress.RecordSeen(_rig.Progression.Get(0));
            _rig.Services.Progress.RecordMerge(_rig.Progression.Get(2));

            var grid = Child<RectTransform>("Grid");
            var template = Child<CollectionSlot>("Template", grid);
            template.SetReferencesForTests(Child<Image>("Icon", template.transform), Child<Text>("Label", template.transform), null);
            var counter = Child<Text>("Counter");
            var panelObject = new GameObject("Collection", typeof(RectTransform));
            panelObject.transform.SetParent(_root.transform, false);
            panelObject.SetActive(false);
            var panel = panelObject.AddComponent<CollectionPanel>();
            panel.SetReferencesForTests(grid, template, counter, null);
            panel.Bind(_rig.Services.Progress, _rig.Progression, null);

            panel.Open();

            Assert.AreEqual(_rig.Progression.Count, panel.Slots.Count);
            Assert.IsTrue(panel.Slots[0].ShowsDiscovered);
            Assert.IsFalse(panel.Slots[1].ShowsDiscovered);
            Assert.AreEqual("???", panel.Slots[1].LabelText);
            Assert.IsTrue(panel.Slots[2].ShowsDiscovered);
            Assert.AreEqual($"2 / {_rig.Progression.Count} discovered", panel.CounterText);
        }

        [Test]
        public void Collection_ItemWithoutSpriteUsesPlaceholder()
        {
            var noSprite = _rig.Progression.Get(0);
            noSprite.Set(noSprite.Id, noSprite.DisplayName, null, Color.white, noSprite.Radius, 1f, 10);
            _rig.Services.Progress.RecordSeen(noSprite);
            var placeholder = Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), Vector2.one * 0.5f);

            var grid = Child<RectTransform>("Grid");
            var template = Child<CollectionSlot>("Template", grid);
            var icon = Child<Image>("Icon", template.transform);
            template.SetReferencesForTests(icon, Child<Text>("Label", template.transform), null);
            var panel = Child<CollectionPanel>("Collection");
            panel.SetReferencesForTests(grid, template, null, placeholder);
            panel.Bind(_rig.Services.Progress, _rig.Progression, null);

            Assert.DoesNotThrow(() => panel.Open());
            Assert.AreSame(placeholder, panel.Slots[0].GetComponentInChildren<Image>(true).sprite);
            Object.DestroyImmediate(placeholder.texture);
            Object.DestroyImmediate(placeholder);
        }

        [Test]
        public void Achievements_RowsReflectRealProgress()
        {
            _rig.Services.Progress.RecordMerge(_rig.Progression.Get(1));
            _rig.Services.Progress.SubmitFinalScore(40);

            var list = Child<RectTransform>("List");
            var template = Child<AchievementRow>("Row", list);
            template.SetReferencesForTests(Child<Text>("Title", template.transform), Child<Text>("Progress", template.transform), null);
            var stats = Child<Text>("Stats");
            var panel = Child<AchievementsPanel>("Achievements");
            panel.SetReferencesForTests(stats, list, template);
            panel.Bind(_rig.Services.Achievements, _rig.Services.Progress, _rig.Progression, null);

            panel.Open();

            Assert.AreEqual(3, panel.Rows.Count);
            Assert.IsTrue(panel.Rows[0].ShowsUnlocked, "first_game");
            Assert.AreEqual("1 / 2", panel.Rows[1].ProgressText, "merges_2");
            Assert.IsFalse(panel.Rows[2].ShowsUnlocked, "discover_l2");
            StringAssert.StartsWith("40\n1\n1\nL1", panel.StatsText);
        }

        [Test]
        public void DailyCard_ShowsProgressAndHidesClaimWithoutReward()
        {
            var claim = Child<Button>("Claim");
            var card = Child<DailyChallengeCard>("Card");
            card.SetReferencesForTests(Child<Text>("Title"), Child<Text>("Progress"), Child<Text>("Status"), claim);
            card.Bind(_rig.Services.DailyChallenges, _rig.Services.Progress);

            Assert.AreEqual("0 / 2", card.ProgressText);
            Assert.AreEqual("In progress", card.StatusText);

            _rig.Services.Progress.RecordMerge(_rig.Progression.Get(1));
            _rig.Services.Progress.RecordMerge(_rig.Progression.Get(1));

            Assert.AreEqual("2 / 2", card.ProgressText, "Updated from the service event, not polling.");
            Assert.AreEqual("Completed!", card.StatusText);
            Assert.IsFalse(card.ClaimVisible, "No reward configured, so no Claim button.");
        }
    }
}
