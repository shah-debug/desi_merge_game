using DesiMerge.Configuration;
using NUnit.Framework;

namespace DesiMerge.Tests.PlayMode
{
    /// <summary>Real Rigidbody2D/Collider2D contacts, stepped manually for determinism.</summary>
    public class MergeBoardTests
    {
        private GameplayTestRig _rig;
        private int _merges;
        private int _finalClears;

        [SetUp]
        public void SetUp()
        {
            _rig = new GameplayTestRig();
            _rig.Board.ItemMerged += (_, __) => _merges++;
            _rig.Board.FinalLevelCleared += _ => _finalClears++;
        }

        [TearDown]
        public void TearDown()
        {
            _rig.Dispose();
            _merges = 0;
            _finalClears = 0;
        }

        /// <summary>
        /// Regression (Phase 6): A–B and C–D merge in one step, and a stale contact (A–C or B–D) was reported in the same step.
        /// The pool hands two of those objects straight back as the new items, so the stale pair used to merge two items
        /// that never touched (a phantom third merge). Released items must stay consumed until the step is resolved.
        /// </summary>
        [Test]
        public void StaleContactBetweenReusedPoolObjects_DoesNotMergeInTheSameStep()
        {
            var a = _rig.Board.SpawnLocal(0, new UnityEngine.Vector2(-2.5f, 5f));
            var b = _rig.Board.SpawnLocal(0, new UnityEngine.Vector2(-1.5f, 5f));
            var c = _rig.Board.SpawnLocal(0, new UnityEngine.Vector2(1.5f, 5f));
            var d = _rig.Board.SpawnLocal(0, new UnityEngine.Vector2(2.5f, 5f));
            _rig.Board.ReportContact(a, b);
            _rig.Board.ReportContact(c, d);
            // Stale cross contacts; which object the pool hands back first is an implementation detail, so cover both.
            _rig.Board.ReportContact(a, c);
            _rig.Board.ReportContact(b, d);

            _rig.Board.Step(0f);

            Assert.AreEqual(2, _merges, "Exactly the two real merges.");
            Assert.AreEqual(2, _rig.Board.ActiveCount);
            foreach (var item in _rig.Board.ActiveItems)
            {
                Assert.AreEqual(1, item.Level);
            }
        }

        [Test]
        public void SameLevelContact_MergesIntoNextLevelExactlyOnce()
        {
            _rig.SpawnOnFloor(0, -0.25f);
            _rig.SpawnOnFloor(0, 0.25f);

            _rig.Simulate();

            Assert.AreEqual(1, _merges);
            Assert.AreEqual(1, _rig.Board.ActiveCount);
            Assert.AreEqual(1, _rig.Board.ActiveItems[0].Level);

            _rig.Simulate(20);
            Assert.AreEqual(1, _merges, "Later steps must not merge the consumed pair again.");
        }

        [Test]
        public void DifferentLevelContact_DoesNotMerge()
        {
            _rig.SpawnOnFloor(0, -0.3f);
            _rig.SpawnOnFloor(1, 0.3f);

            _rig.Simulate(10);

            Assert.AreEqual(0, _merges);
            Assert.AreEqual(2, _rig.Board.ActiveCount);
        }

        [Test]
        public void ThreeTouchingEquals_ProduceOneMergeAndOneSurvivor()
        {
            _rig.SpawnOnFloor(0, -0.5f);
            _rig.SpawnOnFloor(0, 0f);
            _rig.SpawnOnFloor(0, 0.5f);

            _rig.Simulate();

            Assert.AreEqual(1, _merges);
            Assert.AreEqual(2, _rig.Board.ActiveCount);
        }

        [Test]
        public void MergeResult_TouchingAnEqual_ChainsOnALaterStep()
        {
            _rig.SpawnOnFloor(0, -0.25f);
            _rig.SpawnOnFloor(0, 0.25f);
            _rig.Simulate();
            Assert.AreEqual(1, _merges);

            // A second level-1 item overlapping the merge result: they merge on a later step, not recursively.
            var result = _rig.Board.ActiveItems[0];
            _rig.SpawnOnFloor(1, result.Body.position.x + 0.5f);
            _rig.Simulate();

            Assert.AreEqual(2, _merges);
            Assert.AreEqual(1, _rig.Board.ActiveCount);
            Assert.AreEqual(2, _rig.Board.ActiveItems[0].Level);
        }

        [Test]
        public void FinalLevel_ClearBoth_RemovesBothItems()
        {
            var final = _rig.Progression.FinalLevel;
            _rig.SpawnOnFloor(final, -0.5f);
            _rig.SpawnOnFloor(final, 0.5f);

            _rig.Simulate();

            Assert.AreEqual(1, _finalClears);
            Assert.AreEqual(0, _merges);
            Assert.AreEqual(0, _rig.Board.ActiveCount);
        }

        [Test]
        public void DangerLine_RaisesOnceAfterDelay()
        {
            _rig.Dispose();
            _rig = new GameplayTestRig(grace: 0f, gameOverDelay: 0.5f);
            var raised = 0;
            _rig.Board.DangerTimeExceeded += () => raised++;

            // Placed above the danger line; physics is not stepped, so it stays there.
            _rig.Board.SpawnLocal(0, new UnityEngine.Vector2(0f, _rig.Config.DangerLineHeight + 1f));

            _rig.Board.Step(0f);
            _rig.Board.Step(0.4f);
            Assert.AreEqual(0, raised);

            _rig.Board.Step(0.5f);
            _rig.Board.Step(1f);
            Assert.AreEqual(1, raised);
        }

        [Test]
        public void Clear_ReturnsItemsToPoolAndResetsState()
        {
            _rig.SpawnOnFloor(0, -1f);
            _rig.SpawnOnFloor(1, 1f);

            _rig.Board.Clear();

            Assert.AreEqual(0, _rig.Board.ActiveCount);

            // Reused pooled items still merge correctly.
            _rig.SpawnOnFloor(0, -0.25f);
            _rig.SpawnOnFloor(0, 0.25f);
            _rig.Simulate();
            Assert.AreEqual(1, _merges);
        }

        [Test]
        public void Frozen_BoardDoesNotMerge()
        {
            _rig.SpawnOnFloor(0, -0.25f);
            _rig.SpawnOnFloor(0, 0.25f);

            _rig.Board.SetFrozen(true);
            _rig.Simulate(5);

            Assert.AreEqual(0, _merges);
            Assert.AreEqual(2, _rig.Board.ActiveCount);
        }
    }

    public class MergeBoardFinalRuleTests
    {
        [Test]
        public void FinalLevel_NoMerge_KeepsBothItems()
        {
            using (var rig = new GameplayTestRig(finalRule: FinalLevelMergeRule.NoMerge))
            {
                var final = rig.Progression.FinalLevel;
                rig.SpawnOnFloor(final, -0.5f);
                rig.SpawnOnFloor(final, 0.5f);

                rig.Simulate(5);

                Assert.AreEqual(2, rig.Board.ActiveCount);
            }
        }
    }
}
