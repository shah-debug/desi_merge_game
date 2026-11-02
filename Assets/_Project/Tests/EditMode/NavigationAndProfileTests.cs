using System;
using System.Linq;
using DesiMerge.Configuration;
using DesiMerge.Core;
using DesiMerge.Persistence;
using DesiMerge.Progression;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace DesiMerge.Tests.EditMode
{
    public class NavigationAndProfileTests
    {
        private TestContent _content;
        private MergeProgression _items;
        private ManualClock _clock;

        [SetUp]
        public void SetUp()
        {
            _content = new TestContent();
            _items = _content.Progression(levels: 5, spawnable: 2);
            _clock = new ManualClock(new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc));
        }

        [TearDown]
        public void TearDown() => _content.DestroyAll();

        [Test]
        public void ScoreHistory_RecordsEachGame_FlagsNewBests_AndKeepsTheLastTen()
        {
            var progress = new PlayerProgressService(new InMemorySaveStore(), _items, _clock);
            progress.SubmitFinalScore(100);
            progress.SubmitFinalScore(80);
            progress.SubmitFinalScore(150);

            var games = progress.RecentGames;
            CollectionAssert.AreEqual(new long[] { 100, 80, 150 }, games.Select(g => g.score).ToArray());
            CollectionAssert.AreEqual(new[] { true, false, true }, games.Select(g => g.newBest).ToArray());
            Assert.AreEqual(DayKey.Today(_clock), games[2].day);

            for (var i = 0; i < 12; i++)
            {
                progress.SubmitFinalScore(i);
            }

            Assert.AreEqual(PlayerProgressData.MaxRecentGames, progress.RecentGames.Count);
            Assert.AreEqual(11, progress.RecentGames[progress.RecentGames.Count - 1].score, "Newest last.");
            Assert.AreEqual(150, progress.HighScore, "History never changes the best score.");
        }

        [Test]
        public void ScoreHistory_RevivedGameUpdatesItsOwnEntry_NotANewOne()
        {
            var progress = new PlayerProgressService(new InMemorySaveStore(), _items, _clock);
            progress.SubmitFinalScore(300);
            progress.SubmitFinalScore(120);
            progress.SubmitRevivedFinalScore(420);

            Assert.AreEqual(2, progress.RecentGames.Count);
            Assert.AreEqual(420, progress.RecentGames[1].score);
            Assert.IsTrue(progress.RecentGames[1].newBest);
            Assert.AreEqual(2, progress.GamesPlayed);
        }

        [Test]
        public void ScoreHistory_PersistsAndOldSavesStartEmptyWithoutLosingProgress()
        {
            var store = new InMemorySaveStore();
            store.Write(SaveKey.PlayerProgress, "{\"schemaVersion\":2,\"highScore\":640,\"gamesPlayed\":9,\"totalMerges\":120,\"discoveredItemIds\":[\"l0\",\"l1\"]}");
            var progress = new PlayerProgressService(store, _items, _clock);
            Assert.AreEqual(0, progress.RecentGames.Count);
            Assert.AreEqual(640, progress.HighScore);
            Assert.AreEqual(9, progress.GamesPlayed);

            progress.SubmitFinalScore(700);
            progress.Flush();
            var reopened = new PlayerProgressService(store, _items, _clock);
            Assert.AreEqual(1, reopened.RecentGames.Count);
            Assert.AreEqual(700, reopened.RecentGames[0].score);
            Assert.IsTrue(reopened.RecentGames[0].newBest);
            Assert.AreEqual(700, reopened.HighScore);
        }

        [Test]
        public void LauncherIcon_AndroidAdaptiveIcon_HasBothOriginalLayers()
        {
            var kinds = PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android);
            var adaptive = kinds.FirstOrDefault(kind => kind.ToString().Contains("Adaptive"));
            Assert.IsNotNull(adaptive, "Unity reports an adaptive icon kind for Android.");
            var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, adaptive);
            Assert.IsNotEmpty(icons);
            var background = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Icon/LauncherBackground.png");
            var foreground = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Icon/LauncherForeground.png");
            Assert.IsNotNull(background);
            Assert.IsNotNull(foreground);
            Assert.AreEqual(432, foreground.width, "Adaptive layers are 108 dp at 4× = 432 px.");
            foreach (var icon in icons)
            {
                Assert.AreSame(background, icon.GetTexture(0), $"{icon.width}px background layer");
                Assert.AreSame(foreground, icon.GetTexture(1), $"{icon.width}px foreground layer");
            }
        }

        [Test]
        public void LauncherIcon_ForegroundArtStaysInsideTheSafeZone()
        {
            var foreground = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Icon/LauncherForeground.png");
            var readable = new Texture2D(2, 2);
            readable.LoadImage(System.IO.File.ReadAllBytes("Assets/_Project/Art/Icon/LauncherForeground.png"));
            try
            {
                var outside = 0;
                for (var y = 0; y < readable.height; y++)
                {
                    for (var x = 0; x < readable.width; x++)
                    {
                        var p = new Vector2((x + 0.5f) / readable.width * 2f - 1f, (y + 0.5f) / readable.height * 2f - 1f);
                        if (p.magnitude > 66f / 108f && readable.GetPixel(x, y).a > 0.1f)
                        {
                            outside++;
                        }
                    }
                }

                Assert.IsNotNull(foreground);
                Assert.AreEqual(0, outside, "Foreground pixels outside the 66 dp safe circle could be cut off by launcher masks.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(readable);
            }
        }
    }
}
