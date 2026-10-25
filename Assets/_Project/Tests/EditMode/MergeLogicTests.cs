using System.Collections.Generic;
using DesiMerge.Configuration;
using DesiMerge.Gameplay;
using NUnit.Framework;

namespace DesiMerge.Tests.EditMode
{
    public class MergeLogicTests
    {
        private TestContent _content;
        private MergeProgression _progression;

        [SetUp]
        public void SetUp()
        {
            _content = new TestContent();
            _progression = _content.Progression(levels: 4);
        }

        [TearDown]
        public void TearDown()
        {
            _content.DestroyAll();
        }

        [Test]
        public void Rules_SameLevel_UpgradesToNextLevel()
        {
            var decision = MergeRules.Decide(_progression, 1, 1, FinalLevelMergeRule.ClearBoth);

            Assert.AreEqual(MergeKind.Upgrade, decision.Kind);
            Assert.AreEqual(2, decision.ResultLevel);
        }

        [TestCase(0, 1)]
        [TestCase(2, 0)]
        [TestCase(3, 2)]
        public void Rules_DifferentLevels_DoNotMerge(int a, int b)
        {
            Assert.AreEqual(MergeKind.None, MergeRules.Decide(_progression, a, b, FinalLevelMergeRule.ClearBoth).Kind);
        }

        [Test]
        public void Rules_FinalLevel_FollowsConfiguredRule()
        {
            Assert.AreEqual(MergeKind.ClearFinal, MergeRules.Decide(_progression, 3, 3, FinalLevelMergeRule.ClearBoth).Kind);
            Assert.AreEqual(MergeKind.None, MergeRules.Decide(_progression, 3, 3, FinalLevelMergeRule.NoMerge).Kind);
        }

        [TestCase(-1)]
        [TestCase(4)]
        public void Rules_InvalidLevel_DoesNotMerge(int level)
        {
            Assert.AreEqual(MergeKind.None, MergeRules.Decide(_progression, level, level, FinalLevelMergeRule.ClearBoth).Kind);
        }

        [Test]
        public void Resolver_DuplicateContactReports_MergeOnce()
        {
            var handler = new RecordingHandler();
            var resolver = new MergeResolver<FakeItem>(_progression, FinalLevelMergeRule.ClearBoth);
            var a = new FakeItem(0);
            var b = new FakeItem(0);

            // Both bodies report the same contact, as Unity does.
            resolver.Report(a, b);
            resolver.Report(b, a);
            var merges = resolver.ResolvePending(handler);

            Assert.AreEqual(1, merges);
            Assert.AreEqual(1, handler.Upgrades.Count);
            Assert.AreEqual(1, handler.Upgrades[0].Level);
            Assert.IsTrue(a.IsConsumed && b.IsConsumed);
        }

        [Test]
        public void Resolver_ItemTouchingTwoEquals_MergesWithOnlyOne()
        {
            var handler = new RecordingHandler();
            var resolver = new MergeResolver<FakeItem>(_progression, FinalLevelMergeRule.ClearBoth);
            var a = new FakeItem(1);
            var b = new FakeItem(1);
            var c = new FakeItem(1);

            resolver.Report(a, b);
            resolver.Report(a, c);
            resolver.Report(c, b);
            resolver.ResolvePending(handler);

            // a+b merge first (report order); c+b is skipped because b is consumed; c survives.
            Assert.AreEqual(1, handler.Upgrades.Count);
            Assert.IsFalse(c.IsConsumed);
        }

        [Test]
        public void Resolver_IgnoresDifferentLevelsSelfAndConsumed()
        {
            var handler = new RecordingHandler();
            var resolver = new MergeResolver<FakeItem>(_progression, FinalLevelMergeRule.ClearBoth);
            var a = new FakeItem(0);
            var consumed = new FakeItem(0);
            consumed.MarkConsumed();

            resolver.Report(a, new FakeItem(1));
            resolver.Report(a, a);
            resolver.Report(a, consumed);
            resolver.Report(a, null);

            Assert.AreEqual(0, resolver.PendingCount);
            Assert.AreEqual(0, resolver.ResolvePending(handler));
            Assert.IsFalse(a.IsConsumed);
        }

        [Test]
        public void Resolver_FinalLevel_ClearBothCallsFinalHandler()
        {
            var handler = new RecordingHandler();
            var resolver = new MergeResolver<FakeItem>(_progression, FinalLevelMergeRule.ClearBoth);

            resolver.Report(new FakeItem(3), new FakeItem(3));
            resolver.ResolvePending(handler);

            Assert.AreEqual(1, handler.FinalClears);
            Assert.AreEqual(0, handler.Upgrades.Count);
        }

        [Test]
        public void Resolver_FinalLevel_NoMergeLeavesItemsUntouched()
        {
            var handler = new RecordingHandler();
            var resolver = new MergeResolver<FakeItem>(_progression, FinalLevelMergeRule.NoMerge);
            var a = new FakeItem(3);
            var b = new FakeItem(3);

            resolver.Report(a, b);
            resolver.ResolvePending(handler);

            Assert.AreEqual(0, handler.FinalClears);
            Assert.IsFalse(a.IsConsumed || b.IsConsumed);
        }

        [Test]
        public void Resolver_ItemsCreatedDuringResolve_WaitForNextStep()
        {
            var resolver = new MergeResolver<FakeItem>(_progression, FinalLevelMergeRule.ClearBoth);
            var handler = new ChainingHandler(resolver);

            resolver.Report(new FakeItem(0), new FakeItem(0));

            Assert.AreEqual(1, resolver.ResolvePending(handler), "The chained report must not be resolved recursively.");
            Assert.AreEqual(1, resolver.PendingCount);
            Assert.AreEqual(1, resolver.ResolvePending(handler));
        }

        [Test]
        public void Resolver_Clear_DropsPendingContacts()
        {
            var resolver = new MergeResolver<FakeItem>(_progression, FinalLevelMergeRule.ClearBoth);
            resolver.Report(new FakeItem(0), new FakeItem(0));

            resolver.Clear();

            Assert.AreEqual(0, resolver.ResolvePending(new RecordingHandler()));
        }

        [Test]
        public void ScoreRules_UpgradeAwardsCreatedItemScore()
        {
            Assert.AreEqual(30, ScoreRules.ForUpgrade(_progression, 2));
        }

        private sealed class FakeItem : IMergeable
        {
            public FakeItem(int level) => Level = level;

            public int Level { get; }
            public bool IsConsumed { get; private set; }

            public void MarkConsumed() => IsConsumed = true;
        }

        private sealed class RecordingHandler : IMergeHandler<FakeItem>
        {
            public readonly List<FakeItem> Upgrades = new List<FakeItem>();
            public int FinalClears;

            public void OnUpgrade(FakeItem first, FakeItem second, int resultLevel) => Upgrades.Add(new FakeItem(resultLevel));

            public void OnFinalCleared(FakeItem first, FakeItem second) => FinalClears++;
        }

        /// <summary>Simulates a merge result immediately touching another equal item.</summary>
        private sealed class ChainingHandler : IMergeHandler<FakeItem>
        {
            private readonly MergeResolver<FakeItem> _resolver;

            public ChainingHandler(MergeResolver<FakeItem> resolver) => _resolver = resolver;

            public void OnUpgrade(FakeItem first, FakeItem second, int resultLevel)
            {
                if (resultLevel < 2)
                {
                    _resolver.Report(new FakeItem(resultLevel), new FakeItem(resultLevel));
                }
            }

            public void OnFinalCleared(FakeItem first, FakeItem second)
            {
            }
        }
    }
}
