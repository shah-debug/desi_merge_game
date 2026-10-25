using DesiMerge.Core;
using NUnit.Framework;
using UnityEngine;

namespace DesiMerge.Tests.PlayMode
{
    public class GameplayLoopTests
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

        [Test]
        public void Begin_EntersPlayingAndShowsInitialHud()
        {
            Assert.AreEqual(GameState.Playing, _rig.Services.GameState.Current);
            Assert.AreEqual(0, _rig.View.LastScore);
            Assert.AreEqual(0, _rig.View.LastBest);
            Assert.IsNotNull(_rig.View.LastNext);
            Assert.IsTrue(_rig.Drop.InputEnabled);
        }

        [Test]
        public void Drop_SpawnsAtDropHeightClampedInsideWalls()
        {
            var level = _rig.Loop.Queue.Current;

            Assert.IsTrue(_rig.Loop.TryDrop(100f));

            var item = _rig.Board.ActiveItems[0];
            var expectedX = _rig.Config.HalfWidth - _rig.Progression.Get(level).Radius;
            Assert.AreEqual(level, item.Level);
            Assert.AreEqual(expectedX, item.Body.position.x, 0.0001f);
            Assert.AreEqual(_rig.Config.DropHeight, item.Body.position.y, 0.0001f);
        }

        [Test]
        public void Drop_AdvancesQueue()
        {
            var next = _rig.Loop.Queue.Next;

            _rig.Loop.TryDrop(0f);

            Assert.AreEqual(next, _rig.Loop.Queue.Current);
            Assert.AreSame(_rig.Progression.Get(_rig.Loop.Queue.Next), _rig.View.LastNext);
        }

        [Test]
        public void Drop_RespectsCooldown()
        {
            Assert.IsTrue(_rig.Loop.TryDrop(0f));
            _rig.Now += 0.25f;
            Assert.IsFalse(_rig.Loop.TryDrop(1f));
            _rig.Now += 0.25f;
            Assert.IsTrue(_rig.Loop.TryDrop(1f));
        }

        [Test]
        public void Drop_RejectedWhilePaused()
        {
            _rig.Services.GameState.TryTransitionTo(GameState.Paused);

            Assert.IsFalse(_rig.Loop.TryDrop(0f));
            Assert.AreEqual(0, _rig.Board.ActiveCount);
        }

        [Test]
        public void Merge_AwardsScoreExactlyOnce()
        {
            _rig.SpawnOnFloor(0, -0.25f);
            _rig.SpawnOnFloor(0, 0.25f);

            _rig.Simulate(10);

            var expected = _rig.Progression.Get(1).ScoreValue;
            Assert.AreEqual(expected, _rig.Loop.Session.Score);
            Assert.AreEqual(expected, _rig.View.LastScore);
            Assert.AreEqual(1, _rig.Loop.Session.MergeCount);
        }

        [Test]
        public void FinalClear_AwardsBonus()
        {
            var final = _rig.Progression.FinalLevel;
            _rig.SpawnOnFloor(final, -0.5f);
            _rig.SpawnOnFloor(final, 0.5f);

            _rig.Simulate();

            Assert.AreEqual(_rig.Config.FinalMergeBonus, _rig.Loop.Session.Score);
        }

        [Test]
        public void EndGame_HappensOnceAndStopsDrops()
        {
            Assert.IsTrue(_rig.Loop.EndGame());
            Assert.IsFalse(_rig.Loop.EndGame());

            Assert.AreEqual(1, _rig.View.GameOverShown);
            Assert.AreEqual(GameState.GameOver, _rig.Services.GameState.Current);
            Assert.AreEqual(1, _rig.Services.Progress.GamesPlayed, "Final score is submitted once.");
            Assert.IsFalse(_rig.Drop.InputEnabled);
            _rig.Now += 10f;
            Assert.IsFalse(_rig.Loop.TryDrop(0f));
        }

        [Test]
        public void DangerLine_TriggersGameOverOnce()
        {
            _rig.Board.SpawnLocal(0, new Vector2(0f, _rig.Config.DangerLineHeight + 1f));

            // No physics stepping: the item stays above the line past grace + delay.
            for (var t = 0f; t <= 5f; t += 0.5f)
            {
                _rig.Board.Step(t);
            }

            Assert.IsTrue(_rig.Loop.Session.IsOver);
            Assert.AreEqual(1, _rig.View.GameOverShown);
        }

        [Test]
        public void EndGame_PersistsBestScore()
        {
            _rig.SpawnOnFloor(0, -0.25f);
            _rig.SpawnOnFloor(0, 0.25f);
            _rig.Simulate();

            _rig.Loop.EndGame();

            Assert.AreEqual(_rig.Loop.Session.Score, _rig.Services.Progress.HighScore);
            Assert.IsTrue(_rig.View.LastWasNewBest);
            Assert.IsTrue(_rig.Store.TryRead(DesiMerge.Persistence.SaveKey.PlayerProgress, out _), "Progress is flushed at game over.");
        }

        [Test]
        public void Restart_ClearsPriorSessionState()
        {
            _rig.Loop.TryDrop(0f);
            _rig.SpawnOnFloor(0, -2f);
            _rig.SpawnOnFloor(0, -1.5f);
            _rig.Simulate();
            _rig.Loop.EndGame();

            _rig.View.ClickRestart();

            Assert.AreEqual(0, _rig.Board.ActiveCount);
            Assert.AreEqual(0, _rig.Loop.Session.Score);
            Assert.IsFalse(_rig.Loop.Session.IsOver);
            Assert.AreEqual(GameState.Playing, _rig.Services.GameState.Current);
            Assert.AreEqual(0, _rig.View.LastScore);
            Assert.IsTrue(_rig.Drop.InputEnabled);
            Assert.IsTrue(_rig.Loop.TryDrop(0f), "Drops work again immediately after restart.");
        }

        [Test]
        public void Restart_DoesNotDuplicateSubscriptions()
        {
            _rig.Loop.Restart();
            _rig.Loop.Restart();

            _rig.SpawnOnFloor(0, -0.25f);
            _rig.SpawnOnFloor(0, 0.25f);
            _rig.Simulate(5);

            Assert.AreEqual(_rig.Progression.Get(1).ScoreValue, _rig.Loop.Session.Score, "One merge must award score once, not once per restart.");
        }

        [Test]
        public void Restart_AfterDangerGameOver_AllowsANewGameOver()
        {
            _rig.Board.SpawnLocal(0, new Vector2(0f, _rig.Config.DangerLineHeight + 1f));
            for (var t = 0f; t <= 5f; t += 0.5f)
            {
                _rig.Board.Step(t);
            }

            _rig.Loop.Restart();
            _rig.Board.SpawnLocal(0, new Vector2(0f, _rig.Config.DangerLineHeight + 1f));
            for (var t = 10f; t <= 15f; t += 0.5f)
            {
                _rig.Board.Step(t);
            }

            Assert.AreEqual(2, _rig.View.GameOverShown);
        }

        [Test]
        public void Dispose_UnsubscribesFromBoardAndView()
        {
            _rig.Loop.Dispose();

            _rig.SpawnOnFloor(0, -0.25f);
            _rig.SpawnOnFloor(0, 0.25f);
            _rig.Simulate();
            _rig.View.ClickRestart();

            Assert.AreEqual(0, _rig.Loop.Session.Score);
            Assert.AreEqual(1, _rig.Board.ActiveCount, "Restart via view must not run after dispose.");
        }
    }
}
