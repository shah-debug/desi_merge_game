using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DesiMerge.Core;
using DesiMerge.Gameplay;
using DesiMerge.UI.Animation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DesiMerge.Tests.PlayMode
{
    public class VisualRedesignSceneTests
    {
        private const string SceneName = "Gameplay";

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Time.timeScale = 1f;
            var scene = SceneManager.GetSceneByName(SceneName);
            if (!scene.isLoaded)
            {
                yield break;
            }

            SceneManager.SetActiveScene(SceneManager.CreateScene("VisualTestEmpty"));
            yield return SceneManager.UnloadSceneAsync(scene);
        }

        private static IEnumerator Load(Action<GameplayEntryPoint> onLoaded)
        {
            if (!Application.CanStreamedLevelBeLoaded(SceneName))
            {
                Assert.Ignore("Gameplay scene is not in Build Settings. Run Desi Merge > Setup > Create All Content.");
            }

            LogAssert.Expect(LogType.Warning, new Regex("Started without Bootstrap"));
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;
            var entry = SceneTestUtility.Find<GameplayEntryPoint>(SceneName);
            Assert.IsNotNull(entry?.Loop, "Gameplay did not start.");
            onLoaded(entry);
        }

        [UnityTest]
        public IEnumerator Decorations_HaveNoCollidersOrBodies_OnlyWallsAndItemsCollide()
        {
            GameplayEntryPoint entry = null;
            yield return Load(e => entry = e);
            entry.Board.SpawnLocal(0, new Vector2(0f, 1f));
            yield return null;

            var colliders = SceneManager.GetSceneByName(SceneName).GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Collider2D>(true)).ToList();
            Assert.IsNotEmpty(colliders);
            foreach (var collider in colliders)
            {
                var isWall = collider.transform.parent != null && collider.transform.parent.name == "Walls" && collider.GetComponent<Renderer>() == null;
                var isItem = collider.GetComponent<MergeItem>() != null;
                Assert.IsTrue(isWall || isItem, $"Unexpected collider on '{collider.name}'");
            }

            var art = entry.Board.transform.Find("Art");
            Assert.IsNotNull(art, "Board art layer is missing");
            Assert.Greater(art.GetComponentsInChildren<SpriteRenderer>(true).Length, 4);
            Assert.IsEmpty(art.GetComponentsInChildren<Collider2D>(true));
            Assert.IsEmpty(art.GetComponentsInChildren<Rigidbody2D>(true));
            var itemOrder = entry.Board.ActiveItems[0].GetComponentInChildren<SpriteRenderer>().sortingOrder;
            foreach (var decoration in art.GetComponentsInChildren<SpriteRenderer>(true))
            {
                Assert.Less(decoration.sortingOrder, itemOrder, $"Decoration '{decoration.name}' could hide items.");
            }
        }

        [UnityTest]
        public IEnumerator PauseRestart_AsksForConfirmation_ThenRestartsThroughTheLoop()
        {
            GameplayEntryPoint entry = null;
            yield return Load(e => entry = e);
            var hud = entry.Hud;
            var loop = entry.Loop;
            entry.Board.SpawnLocal(0, new Vector2(-1f, 0.4f));
            entry.Board.SpawnLocal(2, new Vector2(1f, 0.6f));
            yield return new WaitForSeconds(0.3f);

            Assert.IsTrue(loop.Pause());
            hud.PauseRestartButton.onClick.Invoke();
            Assert.IsTrue(hud.RestartConfirmVisible);
            Assert.IsTrue(loop.IsPaused, "Asking for confirmation must not restart or resume.");
            Assert.AreEqual(2, entry.Board.ActiveCount);

            hud.CancelRestartButton.onClick.Invoke();
            Assert.IsFalse(hud.RestartConfirmVisible);
            Assert.IsTrue(loop.IsPaused);
            Assert.AreEqual(2, entry.Board.ActiveCount);

            hud.PauseRestartButton.onClick.Invoke();
            hud.ConfirmRestartButton.onClick.Invoke();
            Assert.IsFalse(hud.RestartConfirmVisible);
            Assert.AreEqual(GameState.Playing, entry.Services.GameState.Current);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.AreEqual(0, entry.Board.ActiveCount);
            Assert.AreEqual(0, loop.Session.Score);

            Assert.IsTrue(loop.Pause());
            Assert.IsFalse(hud.RestartConfirmVisible, "The pause card always opens on its main actions.");
            Assert.IsTrue(loop.Resume());
        }

        [UnityTest]
        public IEnumerator NextPreview_ShowsTheArtworkOfTheItemDroppedNext()
        {
            GameplayEntryPoint entry = null;
            yield return Load(e => entry = e);
            var loop = entry.Loop;
            var progression = entry.Config.Progression;

            for (var drop = 0; drop < 3; drop++)
            {
                var shownLevel = loop.Queue.Next;
                Assert.AreSame(progression.Get(shownLevel).Sprite, entry.Hud.NextItemSprite);

                Assert.IsTrue(loop.TryDrop(-1.5f + drop * 1.5f));
                yield return new WaitForSeconds(entry.Config.DropCooldownSeconds + 0.05f);

                var before = new HashSet<MergeItem>(entry.Board.ActiveItems);
                Assert.IsTrue(loop.TryDrop(-1.5f + drop * 1.5f));
                var dropped = entry.Board.ActiveItems.Single(item => !before.Contains(item));
                Assert.AreEqual(shownLevel, dropped.Level, "The item shown as NEXT is the one that drops after the current one.");
                Assert.AreSame(progression.Get(shownLevel).Sprite, dropped.GetComponentInChildren<SpriteRenderer>().sprite);
                yield return new WaitForSeconds(entry.Config.DropCooldownSeconds + 0.05f);
            }
        }

        [UnityTest]
        public IEnumerator MergePop_ScalesOnlyTheVisualChild_NeverTheColliderBody()
        {
            GameplayEntryPoint entry = null;
            yield return Load(e => entry = e);
            entry.Board.SpawnLocal(0, new Vector2(-0.25f, 0.35f));
            entry.Board.SpawnLocal(0, new Vector2(0.25f, 0.35f));

            MergeItem merged = null;
            ScalePunch punch = null;
            var deadline = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < deadline)
            {
                merged = entry.Board.ActiveItems.FirstOrDefault(item => item.Level == 1);
                punch = merged != null ? merged.GetComponentInChildren<ScalePunch>() : null;
                if (punch != null && punch.IsPlaying)
                {
                    break;
                }

                yield return null;
            }

            Assert.IsNotNull(merged, "The two items did not merge.");
            Assert.IsTrue(punch.IsPlaying, "The merge pop did not play.");
            for (var frame = 0; frame < 5 && punch.IsPlaying; frame++)
            {
                Assert.AreEqual(Vector3.one * (merged.Radius * 2f), merged.transform.localScale, "Collider body scale changed during the pop.");
                Assert.AreEqual(0.5f, merged.GetComponent<CircleCollider2D>().radius);
                Assert.AreNotSame(merged.transform, punch.transform);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator PowerUpButtons_ShowSelectedAndUnavailableStates_FromTheRealService()
        {
            GameplayEntryPoint entry = null;
            yield return Load(e => entry = e);
            entry.Board.SpawnLocal(0, new Vector2(0f, 0.4f));
            yield return new WaitForSeconds(0.2f);
            var buttons = entry.Hud.PowerUpButtons;
            var hammer = buttons.Single(button => button.PowerUpId == "hammer");
            var shuffle = buttons.Single(button => button.PowerUpId == "shuffle");
            Assert.IsFalse(hammer.ShowsUnavailable);
            Assert.IsFalse(hammer.ShowsSelected);

            entry.Loop.UsePowerUp("hammer");
            Assert.IsTrue(hammer.ShowsSelected);
            Assert.IsFalse(hammer.ShowsUnavailable, "The selected power-up stays bright.");
            Assert.IsTrue(shuffle.ShowsUnavailable, "Other power-ups are unavailable while Hammer waits for a target.");
            Assert.IsTrue(entry.Hud.TargetingVisible);

            entry.Loop.CancelPowerUp();
            Assert.IsFalse(hammer.ShowsSelected);
            Assert.IsFalse(shuffle.ShowsUnavailable);
            Assert.IsFalse(entry.Hud.TargetingVisible);
        }
    }
}
