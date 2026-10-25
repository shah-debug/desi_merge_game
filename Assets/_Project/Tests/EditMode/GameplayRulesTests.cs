using System;
using System.Collections.Generic;
using DesiMerge.Configuration;
using DesiMerge.Core;
using DesiMerge.Gameplay;
using NUnit.Framework;

namespace DesiMerge.Tests.EditMode
{
    public class GameplayRulesTests
    {
        [Test]
        public void Session_AddsScoreAndRaisesEvent()
        {
            var session = new GameSession();
            var reported = new List<long>();
            session.ScoreChanged += reported.Add;

            session.AddMergeScore(10);
            session.AddMergeScore(5);

            Assert.AreEqual(15, session.Score);
            Assert.AreEqual(2, session.MergeCount);
            CollectionAssert.AreEqual(new long[] { 10, 15 }, reported);
        }

        [Test]
        public void Session_EndsExactlyOnce()
        {
            var session = new GameSession();
            var ended = 0;
            session.Ended += () => ended++;

            Assert.IsTrue(session.TryEnd());
            Assert.IsFalse(session.TryEnd());
            Assert.AreEqual(1, ended);
        }

        [Test]
        public void Session_IgnoresScoreAfterEnd()
        {
            var session = new GameSession();
            session.AddMergeScore(10);
            session.TryEnd();

            session.AddMergeScore(50);

            Assert.AreEqual(10, session.Score);
        }

        [Test]
        public void Session_RestartClearsState()
        {
            var session = new GameSession();
            session.AddMergeScore(10);
            session.TryEnd();

            session.Restart();

            Assert.AreEqual(0, session.Score);
            Assert.AreEqual(0, session.MergeCount);
            Assert.IsFalse(session.IsOver);
            Assert.IsTrue(session.TryEnd(), "A restarted session can end again.");
        }

        [Test]
        public void Session_RejectsNegativePoints()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameSession().AddMergeScore(-1));
        }

        [Test]
        public void SpawnQueue_SameSeedGivesSameSequence()
        {
            var first = new SpawnQueue(5, 1234);
            var second = new SpawnQueue(5, 1234);

            for (var i = 0; i < 50; i++)
            {
                Assert.AreEqual(first.Take(), second.Take());
            }
        }

        [Test]
        public void SpawnQueue_StaysWithinSpawnableLevels_AndAdvances()
        {
            var queue = new SpawnQueue(3, 42);
            for (var i = 0; i < 200; i++)
            {
                var expected = queue.Current;
                var next = queue.Next;
                Assert.AreEqual(expected, queue.Take());
                Assert.AreEqual(next, queue.Current);
                Assert.That(queue.Current, Is.InRange(0, 2));
            }
        }

        [Test]
        public void SpawnQueue_ResetWithSameSeedRestartsSequence()
        {
            var queue = new SpawnQueue(5, 7);
            var firstRun = new[] { queue.Take(), queue.Take(), queue.Take() };

            queue.Reset(7);

            CollectionAssert.AreEqual(firstRun, new[] { queue.Take(), queue.Take(), queue.Take() });
        }

        [TestCase(0f, 0f)]
        [TestCase(2f, 2f)]
        [TestCase(10f, 2.75f)]
        [TestCase(-10f, -2.75f)]
        public void DropRules_ClampsInsideWalls(float requested, float expected)
        {
            // Half width 3.25, radius 0.5 -> allowed range [-2.75, 2.75].
            Assert.AreEqual(expected, DropRules.ClampX(requested, 3.25f, 0.5f), 0.0001f);
        }

        [Test]
        public void DropRules_ItemWiderThanContainer_ClampsToCentre()
        {
            Assert.AreEqual(0f, DropRules.ClampX(5f, 1f, 2f));
        }

        [TestCase(false, GameState.Playing, 1f, 1f, true)]
        [TestCase(true, GameState.Playing, 1f, 0f, false)]
        [TestCase(false, GameState.Paused, 1f, 0f, false)]
        [TestCase(false, GameState.GameOver, 1f, 0f, false)]
        [TestCase(false, GameState.Playing, 0.4f, 0.5f, false)]
        public void DropRules_CanDrop(bool sessionOver, GameState state, float now, float nextAllowed, bool expected)
        {
            Assert.AreEqual(expected, DropRules.CanDrop(sessionOver, state, now, nextAllowed));
        }

        [Test]
        public void GameOverRule_TriggersAfterContinuousDelay()
        {
            var aboveSince = GameOverRule.NotAbove;

            Assert.IsFalse(GameOverRule.Evaluate(10f, 5f, 4f, 0f, 1f, 2f, ref aboveSince));
            Assert.IsFalse(GameOverRule.Evaluate(11.9f, 5f, 4f, 0f, 1f, 2f, ref aboveSince));
            Assert.IsTrue(GameOverRule.Evaluate(12f, 5f, 4f, 0f, 1f, 2f, ref aboveSince));
        }

        [Test]
        public void GameOverRule_DroppingBelowLineResetsTimer()
        {
            var aboveSince = GameOverRule.NotAbove;

            GameOverRule.Evaluate(10f, 5f, 4f, 0f, 1f, 2f, ref aboveSince);
            GameOverRule.Evaluate(11f, 3f, 4f, 0f, 1f, 2f, ref aboveSince);

            Assert.AreEqual(GameOverRule.NotAbove, aboveSince);
            Assert.IsFalse(GameOverRule.Evaluate(12.5f, 5f, 4f, 0f, 1f, 2f, ref aboveSince));
        }

        [Test]
        public void GameOverRule_IgnoresItemsInSpawnGrace()
        {
            var aboveSince = GameOverRule.NotAbove;

            // Spawned at t=10 with 1.5 s grace: above the line, but still in grace.
            Assert.IsFalse(GameOverRule.Evaluate(11f, 5f, 4f, 10f, 1.5f, 0.1f, ref aboveSince));
            Assert.AreEqual(GameOverRule.NotAbove, aboveSince);
        }
    }
}
