using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using DesiMerge.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DesiMerge.Tests.PlayMode
{
    /// <summary>
    /// DM-01: a game in progress is saved when the app goes to the background and resumed on the next start. The scene
    /// runs on in-memory development services, so the player's real saves are never touched.
    /// </summary>
    public class RunResumeTests
    {
        private const string GameplayScene = "Gameplay";

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            var empty = SceneManager.GetSceneByName("ResumeEmpty");
            SceneManager.SetActiveScene(empty.isLoaded ? empty : SceneManager.CreateScene("ResumeEmpty"));
            var scene = SceneManager.GetSceneByName(GameplayScene);
            if (scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        [UnityTest]
        public IEnumerator BackgroundedGame_IsSaved_AndResumesWithItsItemsScoreAndQueue_OnThePauseCard()
        {
            if (!Application.CanStreamedLevelBeLoaded(GameplayScene))
            {
                Assert.Ignore("Gameplay scene is not in Build Settings.");
            }

            LogAssert.Expect(LogType.Warning, new Regex("Started without Bootstrap"));
            yield return SceneManager.LoadSceneAsync(GameplayScene, LoadSceneMode.Single);
            yield return null;
            var entry = SceneTestUtility.Find<GameplayEntryPoint>(GameplayScene);
            entry.Hud.CloseTutorial();
            var loop = entry.Loop;
            var store = entry.Services.RunSnapshots;
            Assert.IsNotNull(store);
            Assert.IsFalse(loop.SaveRunSnapshot(), "An untouched game isn't worth saving.");

            Assert.IsTrue(loop.TryDrop(-1f));
            entry.Board.SpawnLocal(2, new Vector2(1.5f, 0.6f));
            entry.Board.SpawnLocal(3, new Vector2(-1.5f, 0.6f));
            yield return new WaitForSeconds(1f);
            loop.Session.AddMergeScore(75);
            var score = loop.Session.Score;
            var items = entry.Board.ActiveItems.Select(i => i.Level).OrderBy(l => l).ToArray();
            var current = loop.Queue.Current;
            var next = loop.Queue.Next;

            Assert.IsTrue(loop.HandleAppInterrupted(), "Backgrounding pauses.");
            Assert.IsTrue(loop.SaveRunSnapshot());
            var saved = store.Load();
            Assert.IsTrue(saved.active);
            Assert.AreEqual(score, saved.score);
            Assert.AreEqual(items.Length, saved.items.Count);

            // A fresh start (as after Android closed the app) begins from the saved game.
            loop.Restart();
            Assert.IsFalse(store.Load().active, "Restart abandons the saved game.");
            store.Save(saved);
            Assert.IsTrue(loop.TryResumeSavedGame());
            yield return null;
            Assert.AreEqual(score, loop.Session.Score);
            CollectionAssert.AreEqual(items, entry.Board.ActiveItems.Select(i => i.Level).OrderBy(l => l).ToArray(), "Same items.");
            Assert.AreEqual(current, loop.Queue.Current);
            Assert.AreEqual(next, loop.Queue.Next);
            Assert.IsTrue(loop.IsPaused, "Waits on the pause card.");
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(GameplayScene), "resumed-game-9x16.png", 540, 960);

            Assert.IsTrue(loop.Resume());
            Assert.IsTrue(loop.EndGame());
            Assert.IsFalse(store.Load().active, "A finished game leaves nothing to resume.");
        }

        [UnityTest]
        public IEnumerator InvalidSavedGame_IsDiscarded_AndANewGameStarts()
        {
            if (!Application.CanStreamedLevelBeLoaded(GameplayScene))
            {
                Assert.Ignore("Gameplay scene is not in Build Settings.");
            }

            LogAssert.Expect(LogType.Warning, new Regex("Started without Bootstrap"));
            yield return SceneManager.LoadSceneAsync(GameplayScene, LoadSceneMode.Single);
            yield return null;
            var entry = SceneTestUtility.Find<GameplayEntryPoint>(GameplayScene);
            entry.Hud.CloseTutorial();
            var store = entry.Services.RunSnapshots;
            store.Save(new RunSnapshotData { active = true, sessionId = "x", items = { new RunSnapshotItem { level = 999 } } });
            LogAssert.Expect(LogType.Warning, new Regex("doesn't fit this board"));
            Assert.IsFalse(entry.Loop.TryResumeSavedGame());
            Assert.IsFalse(store.Load().active);
        }
    }
}
