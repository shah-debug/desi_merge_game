using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DesiMerge.Core;
using DesiMerge.Gameplay;
using DesiMerge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DesiMerge.Tests.PlayMode
{
    /// <summary>
    /// Phase 6.7 crash hunting on the real generated scenes (in-memory saves only). Every test fails on any logged
    /// error or exception (Unity Test Framework default). Stress length: DESIMERGE_STRESS_SECONDS (default 40).
    /// </summary>
    public class ProductionStressTests
    {
        private const string GameplayScene = "Gameplay";
        private const string MenuScene = "MainMenu";

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            var empty = SceneManager.GetSceneByName("StressEmpty");
            SceneManager.SetActiveScene(empty.isLoaded ? empty : SceneManager.CreateScene("StressEmpty"));
            foreach (var name in new[] { GameplayScene, MenuScene })
            {
                var scene = SceneManager.GetSceneByName(name);
                if (scene.isLoaded)
                {
                    yield return SceneManager.UnloadSceneAsync(scene);
                }
            }
        }

        private static IEnumerator LoadGameplay(Action<GameplayEntryPoint> onLoaded)
        {
            if (!Application.CanStreamedLevelBeLoaded(GameplayScene))
            {
                Assert.Ignore("Gameplay scene is not in Build Settings.");
            }

            LogAssert.Expect(LogType.Warning, new Regex("Started without Bootstrap"));
            yield return SceneManager.LoadSceneAsync(GameplayScene, LoadSceneMode.Single);
            yield return null;
            var entry = SceneTestUtility.Find<GameplayEntryPoint>(GameplayScene);
            Assert.IsNotNull(entry?.Loop, "Gameplay did not start.");
            entry.Hud.CloseTutorial();
            onLoaded(entry);
        }

        /// <summary>Counts merges and the score they should give, straight from the board's events.</summary>
        private sealed class Ledger : IDisposable
        {
            private readonly GameplayEntryPoint _entry;
            public int Merges;
            public long ExpectedScore;
            public int FinalClears;

            public Ledger(GameplayEntryPoint entry)
            {
                _entry = entry;
                entry.Board.ItemMerged += OnMerged;
                entry.Board.FinalLevelCleared += OnFinal;
            }

            public void Reset()
            {
                Merges = 0;
                ExpectedScore = 0;
                FinalClears = 0;
            }

            private void OnMerged(int level, Vector2 position)
            {
                if (_entry.Loop.Session.IsOver)
                {
                    return;
                }

                Merges++;
                ExpectedScore += ScoreRules.ForUpgrade(_entry.Config.Progression, level);
            }

            private void OnFinal(Vector2 position)
            {
                if (_entry.Loop.Session.IsOver)
                {
                    return;
                }

                Merges++;
                FinalClears++;
                ExpectedScore += ScoreRules.ForFinalClear(_entry.Config);
            }

            public void Dispose()
            {
                _entry.Board.ItemMerged -= OnMerged;
                _entry.Board.FinalLevelCleared -= OnFinal;
            }
        }

        /// <summary>
        /// Fails if an item has left the container (centre past the visible wall art, below the floor, or NaN).
        /// Returns the deepest overlap of an item's edge into a wall, which is measured and reported rather than
        /// asserted to zero: a heavily loaded physics stack always overlaps by a small amount.
        /// </summary>
        private static float AssertBoardSane(GameplayEntryPoint entry, string when)
        {
            var half = entry.Config.HalfWidth;
            var deepest = 0f;
            foreach (var item in entry.Board.ActiveItems)
            {
                Assert.IsTrue(item != null && item.isActiveAndEnabled, $"{when}: a destroyed or disabled item is still listed as active.");
                var local = entry.Board.WorldToLocal(item.transform.position);
                Assert.IsFalse(float.IsNaN(local.x) || float.IsNaN(local.y), $"{when}: NaN position.");
                Assert.LessOrEqual(Mathf.Abs(local.x), half + 0.5f, $"{when}: item escaped through a wall at {local}.");
                Assert.GreaterOrEqual(local.y, -0.6f, $"{when}: item fell through the floor at {local}.");
                deepest = Mathf.Max(deepest, Mathf.Abs(local.x) + item.Radius - half);
            }

            return deepest;
        }

        private static int CountSceneObjects(string sceneName) =>
            SceneManager.GetSceneByName(sceneName).GetRootGameObjects().Sum(root => root.GetComponentsInChildren<Transform>(true).Length);

        [UnityTest]
        [Timeout(2400000)]
        public IEnumerator Stress_RapidDropsEdgesPausesAndRestarts_KeepScoreAndBoardConsistent()
        {
            GameplayEntryPoint entry = null;
            yield return LoadGameplay(e => entry = e);
            var loop = entry.Loop;
            var half = entry.Config.HalfWidth;
            var seconds = float.TryParse(Environment.GetEnvironmentVariable("DESIMERGE_STRESS_SECONDS"), out var configured) ? configured : 40f;
            var random = new System.Random(1234);
            var positions = new[] { -half - 1f, half + 1f, -half, half, 0f, 0f, 0.01f };
            var frameTimes = new List<float>(20000);
            var ledger = new Ledger(entry);
            var games = 1;
            var drops = 0;
            var pauses = 0;
            var maxActive = 0;
            var maxObjects = 0;
            var maxPenetration = 0f;
            var penetrationSamples = 0;
            var deepSamples = 0;
            var lastScore = 0L;
            var objectsAtStart = CountSceneObjects(GameplayScene);
            GC.Collect();
            var memoryAtStart = GC.GetTotalMemory(true);
            var gcAtStart = GC.CollectionCount(0);
            var started = Time.realtimeSinceStartup;
            var nextPause = started + 5f;

            while (Time.realtimeSinceStartup - started < seconds)
            {
                frameTimes.Add(Time.unscaledDeltaTime);
                if (loop.Session.IsOver)
                {
                    Assert.AreEqual(ledger.ExpectedScore, loop.Session.Score, "Final score equals the merges that happened.");
                    Assert.IsTrue(loop.Restart());
                    ledger.Reset();
                    lastScore = 0;
                    games++;
                    Assert.AreEqual(0, entry.Board.ActiveCount, "Restart leaves an empty board.");
                    Assert.AreEqual(0, loop.Session.Score);
                }
                else if (Time.realtimeSinceStartup > nextPause)
                {
                    Assert.IsTrue(loop.Pause());
                    Assert.IsFalse(loop.TryDrop(0f), "No drops while paused.");
                    yield return null;
                    yield return null;
                    Assert.IsTrue(loop.Resume());
                    pauses++;
                    nextPause = Time.realtimeSinceStartup + 5f + (float)random.NextDouble() * 3f;
                }
                else
                {
                    var x = random.Next(4) == 0 ? (float)(random.NextDouble() * 2 - 1) * half : positions[random.Next(positions.Length)];
                    drops += loop.TryDrop(x) ? 1 : 0;
                    loop.TryDrop(x);
                }

                Assert.GreaterOrEqual(loop.Session.Score, lastScore, "Score never goes down within a game.");
                lastScore = loop.Session.Score;
                Assert.AreEqual(ledger.Merges, loop.Session.MergeCount, "Every merge is counted exactly once.");
                Assert.AreEqual(ledger.ExpectedScore, loop.Session.Score, "Score equals the sum of the merges (no double scoring).");
                maxActive = Mathf.Max(maxActive, entry.Board.ActiveCount);
                Assert.LessOrEqual(entry.Board.ActiveCount, 150, "Item count stays bounded.");
                if (frameTimes.Count % 30 == 0)
                {
                    var depth = AssertBoardSane(entry, $"game {games}");
                    maxPenetration = Mathf.Max(maxPenetration, depth);
                    penetrationSamples++;
                    deepSamples += depth > 0.1f ? 1 : 0;
                    maxObjects = Mathf.Max(maxObjects, CountSceneObjects(GameplayScene));
                }

                yield return null;
            }

            ledger.Dispose();
            var gcCount = GC.CollectionCount(0) - gcAtStart;
            var memoryAtEnd = GC.GetTotalMemory(false);
            frameTimes.Sort();
            float Percentile(float p) => frameTimes[Mathf.Clamp((int)(frameTimes.Count * p), 0, frameTimes.Count - 1)] * 1000f;
            var summary = $"[Stress] seconds={seconds:0} frames={frameTimes.Count} games={games} drops={drops} pauses={pauses} " +
                          $"maxActiveItems={maxActive} sceneObjects start={objectsAtStart} max={maxObjects} " +
                          $"frameMs p50={Percentile(0.5f):0.0} p95={Percentile(0.95f):0.0} p99={Percentile(0.99f):0.0} max={Percentile(1f):0.0} " +
                          $"gen0GCs={gcCount} managedMB start={memoryAtStart / 1048576f:0.0} end={memoryAtEnd / 1048576f:0.0} " +
                          $"wallOverlap max={maxPenetration:0.000} samplesOver0.1={deepSamples}/{penetrationSamples}";
            Debug.Log(summary);
            var dir = Environment.GetEnvironmentVariable("DESIMERGE_CAPTURE_DIR");
            if (!string.IsNullOrEmpty(dir))
            {
                System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "stress-results.txt"), summary + Environment.NewLine);
            }

            Assert.Greater(drops, 10, "The stress session actually played.");
            Assert.LessOrEqual(maxObjects, objectsAtStart + 400, "Pooled objects don't grow without bound.");
        }

        [UnityTest]
        public IEnumerator Cluster_ManyIdenticalItemsTouching_MergesConserveValueAndScoreOnce()
        {
            GameplayEntryPoint entry = null;
            yield return LoadGameplay(e => entry = e);
            var ledger = new Ledger(entry);
            const int count = 24;
            var radius = entry.Config.Progression.Get(0).Radius;
            for (var i = 0; i < count; i++)
            {
                var column = i % 6;
                var row = i / 6;
                entry.Board.SpawnLocal(0, new Vector2((column - 2.5f) * radius * 1.9f, 0.5f + row * radius * 1.9f));
            }

            var lastChange = Time.realtimeSinceStartup;
            var lastCount = entry.Board.ActiveCount;
            var deadline = Time.realtimeSinceStartup + 12f;
            while (Time.realtimeSinceStartup < deadline && Time.realtimeSinceStartup - lastChange < 1.5f)
            {
                if (entry.Board.ActiveCount != lastCount)
                {
                    lastCount = entry.Board.ActiveCount;
                    lastChange = Time.realtimeSinceStartup;
                }

                yield return null;
            }

            var value = entry.Board.ActiveItems.Sum(item => 1 << item.Level);
            Assert.AreEqual(count, value, "Merging conserves value: no item merged twice and none lost.");
            Assert.AreEqual(count - entry.Board.ActiveCount, ledger.Merges, "Each merge removes exactly one item.");
            Assert.AreEqual(ledger.Merges, entry.Loop.Session.MergeCount);
            Assert.AreEqual(ledger.ExpectedScore, entry.Loop.Session.Score);
            Assert.Greater(ledger.Merges, 10, "The cluster actually merged.");
            AssertBoardSane(entry, "after the cluster");
            ledger.Dispose();
        }

        [UnityTest]
        public IEnumerator MergeAgainstAWall_BiggerResultStartsInsideTheContainer()
        {
            GameplayEntryPoint entry = null;
            yield return LoadGameplay(e => entry = e);
            var progression = entry.Config.Progression;
            var level = progression.FinalLevel - 2;
            var radius = progression.Get(level).Radius;
            var resultRadius = progression.Get(level + 1).Radius;
            var half = entry.Config.HalfWidth;
            Assert.Greater(resultRadius, radius);
            float? spawnedX = null;
            void OnMerged(int resultLevel, Vector2 position)
            {
                var created = entry.Board.ActiveItems.Last(item => item.Level == resultLevel);
                spawnedX = entry.Board.WorldToLocal(created.transform.position).x;
            }

            entry.Board.ItemMerged += OnMerged;
            entry.Board.SpawnLocal(level, new Vector2(half - radius, radius + 0.01f));
            entry.Board.SpawnLocal(level, new Vector2(half - radius, radius * 3f + 0.02f));
            var deadline = Time.realtimeSinceStartup + 3f;
            while (spawnedX == null && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            entry.Board.ItemMerged -= OnMerged;
            Assert.IsNotNull(spawnedX, "The two items merged.");
            Assert.LessOrEqual(spawnedX.Value, half - resultRadius + 0.01f, "The bigger item must not start inside the wall.");
            yield return new WaitForSeconds(2f);
            AssertBoardSane(entry, "after a merge against the wall");
        }

        [UnityTest]
        public IEnumerator FinalLevelPair_ResolvesOnce()
        {
            GameplayEntryPoint entry = null;
            yield return LoadGameplay(e => entry = e);
            var ledger = new Ledger(entry);
            var final = entry.Config.Progression.FinalLevel;
            var radius = entry.Config.Progression.Get(final).Radius;
            entry.Board.SpawnLocal(final, new Vector2(-radius * 0.98f, radius + 0.1f));
            entry.Board.SpawnLocal(final, new Vector2(radius * 0.98f, radius + 0.1f));
            yield return new WaitForSeconds(2f);

            Assert.AreEqual(1, ledger.Merges, "Two final-level items resolve exactly once.");
            Assert.AreEqual(ledger.ExpectedScore, entry.Loop.Session.Score);
            Assert.LessOrEqual(entry.Board.ActiveCount, 1);
            AssertBoardSane(entry, "after the final-level merge");
            ledger.Dispose();
        }

        [UnityTest]
        public IEnumerator PauseOnTheMergeFrame_FreezesIt_ThenItMergesOnceOnResume()
        {
            GameplayEntryPoint entry = null;
            yield return LoadGameplay(e => entry = e);
            var ledger = new Ledger(entry);
            entry.Board.SpawnLocal(1, new Vector2(-0.2f, 0.5f));
            entry.Board.SpawnLocal(1, new Vector2(0.2f, 0.5f));
            Assert.IsTrue(entry.Loop.Pause());
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.AreEqual(0, ledger.Merges, "Nothing merges while paused.");
            Assert.AreEqual(2, entry.Board.ActiveCount);

            Assert.IsTrue(entry.Loop.Resume());
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(1, ledger.Merges);
            Assert.AreEqual(1, entry.Board.ActiveCount);
            Assert.AreEqual(ledger.ExpectedScore, entry.Loop.Session.Score);
            ledger.Dispose();
        }

        [UnityTest]
        public IEnumerator ButtonMashing_PauseResumeRestartEndInOneFrame_LeavesAPlayableGame()
        {
            GameplayEntryPoint entry = null;
            yield return LoadGameplay(e => entry = e);
            var loop = entry.Loop;
            var hud = entry.Hud;
            entry.Board.SpawnLocal(0, new Vector2(0f, 1f));
            entry.Board.SpawnLocal(2, new Vector2(0.8f, 1f));
            yield return null;

            loop.Pause();
            loop.Pause();
            loop.Resume();
            loop.Resume();
            loop.UsePowerUp("hammer");
            loop.Pause();
            hud.PauseRestartButton.onClick.Invoke();
            hud.ConfirmRestartButton.onClick.Invoke();
            hud.ConfirmRestartButton.onClick.Invoke();
            loop.EndGame();
            loop.EndGame();
            loop.Restart();
            loop.Restart();
            loop.UsePowerUp("shuffle");
            loop.CancelPowerUp();
            yield return null;

            Assert.AreEqual(GameState.Playing, entry.Services.GameState.Current);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.AreEqual(0, loop.Session.Score);
            Assert.IsFalse(hud.TargetingVisible);
            Assert.IsTrue(loop.TryDrop(0f), "The game is still playable.");
            yield return new WaitForSeconds(entry.Config.DropCooldownSeconds + 0.1f);
            Assert.IsTrue(loop.TryDrop(0.5f));
        }

        [UnityTest]
        public IEnumerator Menu_ButtonAndBackSpam_NeverStacksPanelsOrSticks()
        {
            if (!Application.CanStreamedLevelBeLoaded(MenuScene))
            {
                Assert.Ignore("MainMenu is not in Build Settings.");
            }

            LogAssert.Expect(LogType.Warning, new Regex(@"\[MainMenu\] Started without Bootstrap"));
            yield return SceneManager.LoadSceneAsync(MenuScene, LoadSceneMode.Single);
            yield return SceneTestUtility.WaitForScene(MenuScene);
            var menu = SceneTestUtility.Find<MainMenuEntryPoint>(MenuScene);
            yield return new WaitForSecondsRealtime(0.8f);
            var buttons = new[] { menu.DailyRewardsButton, menu.MissionsButton, menu.RankButton, menu.CollectionButton, menu.AchievementsButton, menu.SettingsButton };
            var random = new System.Random(7);

            int OpenCount() => new[] { menu.DailyRewards.IsOpen, menu.Missions.IsOpen, menu.Ranks.IsOpen, menu.Collection.IsOpen, menu.Achievements.IsOpen, menu.Settings.IsOpen, menu.Rewards.IsShowing }.Count(open => open);

            for (var step = 0; step < 200; step++)
            {
                switch (random.Next(4))
                {
                    case 0:
                    case 1:
                        buttons[random.Next(buttons.Length)].onClick.Invoke();
                        break;
                    case 2:
                        menu.HandleBack();
                        break;
                    default:
                        if (menu.DailyRewards.IsOpen)
                        {
                            menu.DailyRewards.ClaimButton.onClick.Invoke();
                        }

                        break;
                }

                Assert.LessOrEqual(OpenCount(), 2, "At most one panel plus the reward popup above it.");
                if (step % 10 == 0)
                {
                    yield return null;
                }
            }

            for (var i = 0; i < 10 && menu.HandleBack(); i++)
            {
            }

            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsFalse(menu.AnyPanelOpen, "Back always gets the player back to the bare menu.");
            Assert.AreEqual(1, menu.Services.PowerUps.SavedCharges(menu.Services.DailyRewards.Days[0].rewards[0].powerUpId), "Mashing Claim paid exactly once.");
            menu.SettingsButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsTrue(menu.Settings.IsOpen, "The menu still responds.");
        }
    }
}
