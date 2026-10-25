using System;
using System.IO;
using DesiMerge.Persistence;
using DesiMerge.Progression;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DesiMerge.Tests.EditMode
{
    public class PersistenceTests
    {
        private string _directory;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "DesiMergeTests", Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        [Test]
        public void FileStore_MissingKey_ReturnsFalse()
        {
            var store = new FileSaveStore(_directory);

            Assert.IsFalse(store.TryRead("absent", out var contents));
            Assert.IsNull(contents);
        }

        [Test]
        public void FileStore_WriteThenRead_RoundTrips()
        {
            var store = new FileSaveStore(_directory);

            store.Write("slot", "{\"a\":1}");

            Assert.IsTrue(store.TryRead("slot", out var contents));
            Assert.AreEqual("{\"a\":1}", contents);
        }

        [Test]
        public void FileStore_Overwrite_ReplacesContentsAndLeavesNoTempFiles()
        {
            var store = new FileSaveStore(_directory);

            store.Write("slot", "first");
            store.Write("slot", "second");

            Assert.IsTrue(store.TryRead("slot", out var contents));
            Assert.AreEqual("second", contents);
            CollectionAssert.AreEquivalent(new[] { "slot.json" }, Array.ConvertAll(Directory.GetFiles(_directory), Path.GetFileName));
        }

        [Test]
        public void FileStore_Delete_RemovesKey()
        {
            var store = new FileSaveStore(_directory);
            store.Write("slot", "data");

            store.Delete("slot");

            Assert.IsFalse(store.TryRead("slot", out _));
        }

        [TestCase("")]
        [TestCase("../escape")]
        [TestCase("with space")]
        [TestCase("sub/dir")]
        public void SaveKey_RejectsUnsafeKeys(string key)
        {
            Assert.Throws<ArgumentException>(() => SaveKey.Validate(key));
        }

        [Test]
        public void Repository_MissingData_ReturnsDefaults()
        {
            var repository = new JsonRepository<PlayerProgressData>(new InMemorySaveStore(), "progress");

            var data = repository.Load();

            Assert.AreEqual(0, data.highScore);
            Assert.AreEqual(PlayerProgressData.CurrentSchemaVersion, data.schemaVersion);
        }

        [Test]
        public void Repository_SaveThenLoad_RoundTrips()
        {
            var store = new InMemorySaveStore();
            var repository = new JsonRepository<PlayerProgressData>(store, "progress");

            repository.Save(new PlayerProgressData { highScore = 1234, coins = 56, gamesPlayed = 7 });
            var loaded = new JsonRepository<PlayerProgressData>(store, "progress").Load();

            Assert.AreEqual(1234, loaded.highScore);
            Assert.AreEqual(56, loaded.coins);
            Assert.AreEqual(7, loaded.gamesPlayed);
        }

        [Test]
        public void Repository_CorruptData_ReturnsDefaultsAndQuarantinesPayload()
        {
            var store = new InMemorySaveStore();
            store.Write("progress", "{ this is not json");
            var repository = new JsonRepository<PlayerProgressData>(store, "progress");

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Corrupt data"));
            var data = repository.Load();

            Assert.AreEqual(0, data.highScore);
            Assert.IsTrue(store.TryRead("progress_corrupt", out var quarantined));
            Assert.AreEqual("{ this is not json", quarantined);
        }
    }
}
