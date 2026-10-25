using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DesiMerge.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

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
            Capture(entry, "gameplay-after-drops.png");

            Assert.IsTrue(entry.Loop.EndGame());
            yield return null;
            Capture(entry, "gameplay-game-over.png");

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
            Capture(entry, "gameplay-all-levels.png");
        }

        private static IEnumerator LoadGameplayScene(Action<GameplayEntryPoint> onLoaded)
        {
            if (!Application.CanStreamedLevelBeLoaded(SceneName))
            {
                Assert.Ignore("Gameplay scene is not in Build Settings. Run Desi Merge > Setup > Create Gameplay Content.");
            }

            // Played without Bootstrap, the entry point falls back to in-memory development services.
            LogAssert.Expect(LogType.Warning, new Regex("Started without Bootstrap"));
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            var entry = FindInScene<GameplayEntryPoint>();
            Assert.IsNotNull(entry, "Gameplay scene has no GameplayEntryPoint.");
            Assert.IsNotNull(entry.Loop, "Gameplay did not start; check the Console for configuration errors.");
            onLoaded(entry);
        }

        private static T FindInScene<T>() where T : Component
        {
            foreach (var root in SceneManager.GetSceneByName(SceneName).GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<T>();
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>Renders the camera plus the HUD (temporarily switched to camera space) into a PNG.</summary>
        private static void Capture(GameplayEntryPoint entry, string fileName)
        {
            var directory = Environment.GetEnvironmentVariable("DESIMERGE_CAPTURE_DIR");
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            var camera = FindInScene<Camera>();
            var canvases = new List<Canvas>();
            foreach (var root in entry.gameObject.scene.GetRootGameObjects())
            {
                canvases.AddRange(root.GetComponentsInChildren<Canvas>());
            }

            foreach (var canvas in canvases)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 5f;
            }

            var target = new RenderTexture(540, 960, 24);
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            // In batch mode nothing has rendered before this point, and the first offscreen render was observed to
            // bind sprite textures incorrectly (walls drawn with the circle texture). A warm-up render avoids that.
            camera.Render();
            camera.Render();

            RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            RenderTexture.active = null;
            camera.targetTexture = null;

            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, fileName), image.EncodeToPNG());
            Object.Destroy(image);
            target.Release();
            Object.Destroy(target);

            foreach (var canvas in canvases)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
        }
    }
}
