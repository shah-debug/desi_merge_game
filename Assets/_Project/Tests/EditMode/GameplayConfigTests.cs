using DesiMerge.Configuration;
using NUnit.Framework;

namespace DesiMerge.Tests.EditMode
{
    public class GameplayConfigTests
    {
        private TestContent _content;

        [SetUp]
        public void SetUp() => _content = new TestContent();

        [TearDown]
        public void TearDown() => _content.DestroyAll();

        [Test]
        public void ValidConfig_HasNoErrors()
        {
            var config = _content.Config(_content.Progression(levels: 10, spawnable: 5));

            CollectionAssert.IsEmpty(config.Validate());
        }

        [Test]
        public void Progression_NavigatesLevels()
        {
            var progression = _content.Progression(levels: 3);

            Assert.IsTrue(progression.TryGetNextLevel(0, out var next));
            Assert.AreEqual(1, next);
            Assert.IsFalse(progression.TryGetNextLevel(2, out _));
            Assert.IsTrue(progression.IsFinalLevel(2));
            Assert.IsFalse(progression.TryGetNextLevel(-1, out _));
        }

        [Test]
        public void MissingProgression_IsReported()
        {
            var config = _content.Config(null);

            StringAssert.Contains("no merge progression", config.Validate()[0]);
        }

        [Test]
        public void TooFewLevels_IsReported()
        {
            var progression = _content.Progression(new[] { _content.Definition("only") }, 1);

            StringAssert.Contains("at least two levels", progression.Validate()[0]);
        }

        [Test]
        public void MissingDefinition_IsReported()
        {
            var progression = _content.Progression(new[] { _content.Definition("a"), null, _content.Definition("c") }, 1);

            Assert.That(progression.Validate(), Has.Some.Contains("Level 1 is missing"));
        }

        [Test]
        public void DuplicateIdsAndRepeatedDefinitions_AreReported()
        {
            var shared = _content.Definition("same");
            var progression = _content.Progression(new[] { shared, _content.Definition("same"), shared }, 1);

            var errors = progression.Validate();

            Assert.That(errors, Has.Some.Contains("reuses id 'same'"));
            Assert.That(errors, Has.Some.Contains("repeats the definition"));
        }

        [Test]
        public void MissingIdAndSprite_AreReported()
        {
            var noId = _content.Definition(string.Empty);
            var noSprite = _content.Definition("b");
            noSprite.Set("b", "b", null, UnityEngine.Color.white, 0.4f, 1f, 1);
            var progression = _content.Progression(new[] { noId, noSprite }, 1);

            var errors = progression.Validate();

            Assert.That(errors, Has.Some.Contains("has no id"));
            Assert.That(errors, Has.Some.Contains("has no sprite"));
        }

        [TestCase(0)]
        [TestCase(4)]
        public void SpawnableCountOutOfRange_IsReported(int spawnable)
        {
            var progression = _content.Progression(levels: 4, spawnable: spawnable);

            Assert.That(progression.Validate(), Has.Some.Contains("Spawnable level count"));
        }

        [Test]
        public void ItemWiderThanContainer_IsReported()
        {
            var progression = _content.Progression(new[] { _content.Definition("a", 0.4f), _content.Definition("huge", 4f) }, 1);

            Assert.That(_content.Config(progression).Validate(), Has.Some.Contains("wider than the container"));
        }
    }
}
