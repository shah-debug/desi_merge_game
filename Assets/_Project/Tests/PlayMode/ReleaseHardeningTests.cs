using System;
using System.Diagnostics;
using DesiMerge.Core;
using DesiMerge.Gameplay;
using NUnit.Framework;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace DesiMerge.Tests.PlayMode
{
    /// <summary>Phase 6 regressions: Android back button, app interruption, repeated restarts, and a measurement harness.</summary>
    public class ReleaseHardeningTests
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

        private GameplayLoop Loop => _rig.Loop;

        [Test]
        public void Back_TogglesPause_CancelsHammer_AndIsIgnoredAtGameOver()
        {
            Assert.IsTrue(Loop.HandleBack());
            Assert.IsTrue(Loop.IsPaused);
            Assert.AreEqual(0f, _rig.TimeScale);
            Assert.IsTrue(Loop.HandleBack());
            Assert.IsFalse(Loop.IsPaused);
            Assert.AreEqual(1f, _rig.TimeScale);

            _rig.SpawnOnFloor(0, 0f);
            _rig.Simulate(5);
            Loop.UsePowerUp("hammer");
            Assert.IsTrue(Loop.HandleBack(), "Back first leaves Hammer selection...");
            Assert.IsFalse(Loop.PowerUps.IsTargeting);
            Assert.IsFalse(Loop.IsPaused, "...without also pausing.");
            Assert.AreEqual(1, _rig.Services.PowerUps.Available("hammer"));

            Loop.EndGame();
            Assert.IsFalse(Loop.HandleBack());
            Assert.AreEqual(GameState.GameOver, _rig.Services.GameState.Current);
        }

        [Test]
        public void AppInterruption_PausesPlay_OnlyOnce_AndNotAfterGameOver()
        {
            Assert.IsTrue(Loop.HandleAppInterrupted());
            Assert.IsTrue(Loop.IsPaused);
            Assert.IsFalse(Loop.HandleAppInterrupted(), "A second interruption doesn't toggle back.");
            Assert.IsTrue(Loop.IsPaused);
            Assert.AreEqual(1, _rig.View.PauseShown);

            Loop.Resume();
            Loop.EndGame();
            Assert.IsFalse(Loop.HandleAppInterrupted());
            Assert.AreEqual(GameState.GameOver, _rig.Services.GameState.Current);
        }

        [Test]
        public void RepeatedRestarts_LeaveNoItemsAndNoDuplicateSubscriptions()
        {
            for (var i = 0; i < 5; i++)
            {
                _rig.SpawnOnFloor(0, -1f);
                _rig.SpawnOnFloor(2, 1.5f);
                _rig.Simulate(2);
                Assert.IsTrue(Loop.Restart());
                Assert.AreEqual(0, _rig.Board.ActiveCount, "Restart returns every item to the pool.");
            }

            _rig.SpawnOnFloor(0, -0.25f);
            _rig.SpawnOnFloor(0, 0.25f);
            _rig.Simulate(5);

            Assert.AreEqual(1, Loop.Session.MergeCount);
            Assert.AreEqual(1, _rig.Services.Progress.TotalMerges, "One merge recorded once, not once per restart.");
            Assert.AreEqual(1, _rig.View.MergeFeedbackCount);
        }

        [Test]
        public void MergesAfterGameOver_DoNotChangeTheFinalScore()
        {
            _rig.SpawnOnFloor(0, -0.25f);
            _rig.SpawnOnFloor(0, 0.25f);
            Loop.EndGame();
            var final = Loop.Session.Score;

            _rig.Simulate(10);

            Assert.AreEqual(final, Loop.Session.Score);
            Assert.AreEqual(2, _rig.Board.ActiveCount, "The board is frozen: no merge happens after game over.");
            Assert.AreEqual(final, _rig.Services.Progress.HighScore);
        }

        /// <summary>
        /// Measurement harness (not a pass/fail performance target): 40 items settling and merging for 500 fixed steps
        /// (10 s of game time) through the real board, physics and loop. Logs time per step and managed-heap growth (Profiler.GetMonoUsedSizeLong; meaningful only without a GC in between).
        /// Editor/Mono numbers on the development PC; they are not Android device measurements.
        /// </summary>
        [Test]
        public void Measure_BoardStepCostAndAllocations()
        {
            var random = new System.Random(3);
            for (var i = 0; i < 40; i++)
            {
                _rig.Board.SpawnLocal(random.Next(3), new Vector2(-2.6f + 1.3f * (i % 5), 0.6f + 1.0f * (i / 5)));
            }

            _rig.Simulate(50); // warm up: settle, JIT, pools

            // Calibrate: prove the allocation counter works on this runtime before trusting a 0.
            var probeStart = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
            var probe = new byte[64 * 1024];
            var counterWorks = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() - probeStart >= probe.Length;

            var allocatedBefore = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
            var collectionsBefore = GC.CollectionCount(0);
            var watch = Stopwatch.StartNew();
            _rig.Simulate(500);
            watch.Stop();
            var allocated = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() - allocatedBefore;

            var perStepMs = watch.Elapsed.TotalMilliseconds / 500.0;
            Debug.Log($"[Perf] items(end)={_rig.Board.ActiveCount} steps=500 totalMs={watch.Elapsed.TotalMilliseconds:F1} " +
                      $"perStepMs={perStepMs:F3} managedBytes={allocated} bytesPerStep={allocated / 500} gen0Collections={GC.CollectionCount(0) - collectionsBefore} allocationCounterWorks={counterWorks}");
            Assert.Greater(_rig.Board.ActiveCount, 0);
        }
    }
}
