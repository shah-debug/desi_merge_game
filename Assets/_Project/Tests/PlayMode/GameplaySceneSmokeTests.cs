using System;
using System.Collections;
using System.Text.RegularExpressions;
using DesiMerge.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DesiMerge.Tests.PlayMode
{
    /// <summary>
    /// Loads the generated Gameplay scene and plays real-time drops. This checks scene wiring, not exact
    /// physics outcomes (those are covered deterministically in MergeBoardTests / GameplayLoopTests).
    /// Set DESIMERGE_CAPTURE_DIR to save rendered screenshots of the scene for review.
    /// </summary>
    public class GameplaySceneSmokeTests
    {
        private const string SceneName = "Gameplay";

        /// <summary>Unloads the Gameplay scene so its bodies can't interact with later physics tests.</summary>
        [UnityTearDown]
        public IEnumerator UnloadGameplayScene()
        {
            Time.timeScale = 1f;
            var scene = SceneManager.GetSceneByName(SceneName);
            if (!scene.isLoaded)
            {
                yield break;
            }

            SceneManager.SetActiveScene(SceneManager.CreateScene("SmokeTestEmpty"));
            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator GameplayScene_LoadsAcceptsDropsAndRestarts()
        {
            GameplayEntryPoint entry = null;
            yield return LoadGameplayScene(e => entry = e);

            foreach (var x in new[] { -2f, -1f, 0f, 1f, 2f, -1.5f, 0.5f, 1.5f })
            {
                Assert.IsTrue(entry.Loop.TryDrop(x), $"Drop at {x} was rejected.");
                yield return new WaitForSeconds(entry.Config.DropCooldownSeconds + 0.05f);
            }

            yield return new WaitForSeconds(1.5f);

            Assert.Greater(entry.Board.ActiveCount, 0);
            Assert.IsFalse(entry.Loop.Session.IsOver);

            Assert.IsTrue(entry.Loop.EndGame());
            yield return null;

            entry.Loop.Restart();
            yield return null;
            Assert.AreEqual(0, entry.Board.ActiveCount);
            Assert.AreEqual(0, entry.Loop.Session.Score);
            Assert.IsFalse(entry.Loop.Session.IsOver);
        }

        [UnityTest]
        public IEnumerator GameplayScene_ShowsEveryLevel()
        {
            GameplayEntryPoint entry = null;
            yield return LoadGameplayScene(e => entry = e);

            // One of each level, stacked in columns, so every placeholder appears in the capture.
            var progression = entry.Config.Progression;
            for (var level = 0; level < progression.Count; level++)
            {
                var column = level % 3;
                entry.Board.SpawnLocal(level, new Vector2(-2f + column * 2f, 1f + level * 0.9f));
            }

            yield return new WaitForSeconds(2.5f);
            Assert.AreEqual(progression.Count, entry.Board.ActiveCount, "Different levels must not merge.");
            yield return SceneCapture.Capture(entry.gameObject.scene, "gameplay-all-levels.png", 540, 960);
        }

        private static IEnumerator LoadGameplayScene(Action<GameplayEntryPoint> onLoaded)
        {
            if (!Application.CanStreamedLevelBeLoaded(SceneName))
            {
                Assert.Ignore("Gameplay scene is not in Build Settings. Run Desi Merge > Setup > Create All Content.");
            }

            // Played without Bootstrap, the entry point falls back to in-memory development services.
            LogAssert.Expect(LogType.Warning, new Regex("Started without Bootstrap"));
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            var entry = SceneTestUtility.Find<GameplayEntryPoint>(SceneName);
            Assert.IsNotNull(entry, "Gameplay scene has no GameplayEntryPoint.");
            Assert.IsNotNull(entry.Loop, "Gameplay did not start; check the Console for configuration errors.");
            onLoaded(entry);
        }
    }

    internal static class SceneTestUtility
    {
        public static T Find<T>(string sceneName) where T : Component
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.isLoaded)
            {
                return null;
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<T>(true);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        public static IEnumerator WaitForScene(string sceneName, float timeoutSeconds = 15f)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Assert.Fail($"Scene '{sceneName}' did not load within {timeoutSeconds} s.");
                }

                yield return null;
            }

            // One more frame so entry points' Start (development fallback) has run.
            yield return null;
        }
    }
}
