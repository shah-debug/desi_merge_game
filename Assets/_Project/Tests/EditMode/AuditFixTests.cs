using System;
using System.Collections.Generic;
using System.IO;
using DesiMerge.Configuration;
using DesiMerge.Core;
using DesiMerge.Cosmetics;
using DesiMerge.Gameplay;
using DesiMerge.Persistence;
using DesiMerge.Progression;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DesiMerge.Tests.EditMode
{
    /// <summary>
    /// Regression tests for the 2026-11-03 audit fixes (DM-01, DM-06, DM-07, DM-09). In-memory stores only; real
    /// player saves are never touched.
    /// </summary>
    public class AuditFixTests
    {
        private TestContent _content;
        private MergeProgression _items;
        private CosmeticCatalog _catalog;
        private ManualClock _clock;

        /// <summary>An in-memory store whose writes for chosen keys throw (simulates the app dying mid-purchase).</summary>
        private sealed class FailingWritesStore : ISaveStore
        {
            public readonly InMemorySaveStore Inner = new InMemorySaveStore();
            public readonly HashSet<string> FailingWrites = new HashSet<string>();

            public bool TryRead(string key, out string contents) => Inner.TryRead(key, out contents);
            public bool TryReadBackup(string key, out string contents) => Inner.TryReadBackup(key, out contents);
            public void Delete(string key) => Inner.Delete(key);

            public void Write(string key, string contents)
            {
                if (FailingWrites.Contains(key))
                {
                    throw new IOException("Injected write failure");
                }

                Inner.Write(key, contents);
            }
        }

        [SetUp]
        public void SetUp()
        {
            _content = new TestContent();
            _items = _content.Progression(levels: 5, spawnable: 2);
            _clock = new ManualClock(new DateTime(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc));
            _catalog = ScriptableObject.CreateInstance<CosmeticCatalog>();
            _catalog.Set(new[]
            {
                new CosmeticDefinition { id = "base", title = "Base", kind = CosmeticKind.ItemSkin, unlock = CosmeticUnlock.Default },
                new CosmeticDefinition { id = "sweet", title = "Sweet", kind = CosmeticKind.ItemSkin, unlock = CosmeticUnlock.Diamonds, price = 300 },
            });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_catalog);
            _content.DestroyAll();
        }

        [Test]
        public void SkinPurchase_InterruptedBeforeTheDiamondsAreSaved_KeepsTheSkinAndTheDiamonds()
        {
            var store = new FailingWritesStore();
            var wallet = new PlayerProgressService(store, _items, _clock);
            wallet.TryGrantCoins("seed", 350);
            var skins = new CosmeticService(store, _catalog, null);

            // The progress file can't be written (as if the app died right after the skin was saved).
            store.FailingWrites.Add(SaveKey.PlayerProgress);
            Assert.AreEqual(CosmeticPurchase.Bought, skins.TryBuy("sweet", wallet));

            var relaunchedWallet = new PlayerProgressService(store, _items, _clock);
            var relaunchedSkins = new CosmeticService(store, _catalog, null);
            Assert.IsTrue(relaunchedSkins.IsOwned("sweet"), "Ownership is saved first.");
            Assert.AreEqual(350, relaunchedWallet.Coins, "The player is never charged without getting the skin.");
            Assert.AreEqual(CosmeticPurchase.AlreadyOwned, relaunchedSkins.TryBuy("sweet", relaunchedWallet), "And never charged twice.");
        }

        [Test]
        public void SkinPurchase_NotEnoughDiamonds_DoesNotOwnTheSkin()
        {
            var store = new InMemorySaveStore();
            var wallet = new PlayerProgressService(store, _items, _clock);
            wallet.TryGrantCoins("seed", 100);
            var skins = new CosmeticService(store, _catalog, null);
            Assert.AreEqual(CosmeticPurchase.NotEnoughDiamonds, skins.TryBuy("sweet", wallet));
            Assert.IsFalse(skins.IsOwned("sweet"));
            Assert.AreEqual(100, wallet.Coins);
        }

        [Test]
        public void SkinDiscovery_RaisesDiscovered_NotChanged()
        {
            var skins = new CosmeticService(new InMemorySaveStore(), _catalog, null);
            var changed = 0;
            var discovered = new List<string>();
            skins.Changed += () => changed++;
            skins.Discovered += (skin, level) => discovered.Add(skin + ":" + level);
            skins.RecordDiscovery("sweet", 3);
            skins.RecordDiscovery("sweet", 3);
            Assert.AreEqual(0, changed, "A discovery is not a skin change, so the board isn't re-skinned.");
            CollectionAssert.AreEqual(new[] { "sweet:3" }, discovered, "Raised once per new level.");
        }

        [Test]
        public void ProgressV4Save_MovesGameGrantsToTheirOwnList_AndStillPaysEachOnce()
        {
            var store = new InMemorySaveStore();
            store.Write(SaveKey.PlayerProgress, "{\"schemaVersion\":4,\"coins\":12,\"appliedCoinGrants\":[\"game:abc:2\",\"dailyreward:5:0\"]}");
            var progress = new PlayerProgressService(store, _items, _clock);
            Assert.IsTrue(progress.HasAppliedCoinGrant("game:abc:2"));
            Assert.IsTrue(progress.HasAppliedCoinGrant("dailyreward:5:0"));
            Assert.IsTrue(progress.TryGrantCoins("game:abc:2", 2));
            Assert.IsTrue(progress.TryGrantCoins("dailyreward:5:0", 5));
            Assert.AreEqual(12, progress.Coins, "Neither is paid again after the migration.");
        }

        // ---------- DM-01: game in progress ----------

        private GameplayConfig Config() => _content.Config(_items);

        private static RunSnapshotData ValidSnapshot() => new RunSnapshotData
        {
            active = true,
            sessionId = "s1",
            score = 120,
            mergeCount = 7,
            queueCurrent = 1,
            queueNext = 0,
            hasDropped = true,
            items = new List<RunSnapshotItem> { new RunSnapshotItem { level = 2, x = 0.5f, y = 1f, rotation = 30f } },
        };

        [Test]
        public void RunSnapshot_RoundTripsThroughTheStore_AndClears()
        {
            var store = new RunSnapshotStore(new InMemorySaveStore());
            Assert.IsFalse(store.Load().active, "No saved game at first.");
            Assert.IsTrue(store.Save(ValidSnapshot()));
            var loaded = store.Load();
            Assert.IsTrue(loaded.IsValidFor(Config()));
            Assert.AreEqual(120, loaded.score);
            Assert.AreEqual(2, loaded.items[0].level);
            Assert.AreEqual(30f, loaded.items[0].rotation);
            Assert.IsTrue(store.Clear());
            Assert.IsFalse(store.Load().active);
        }

        [TestCase("level")]
        [TestCase("position")]
        [TestCase("nan")]
        [TestCase("queue")]
        [TestCase("score")]
        [TestCase("session")]
        [TestCase("schema")]
        public void RunSnapshot_WithImpossibleValues_IsRejected(string broken)
        {
            var snapshot = ValidSnapshot();
            switch (broken)
            {
                case "level": snapshot.items[0].level = 99; break;
                case "position": snapshot.items[0].x = 50f; break;
                case "nan": snapshot.items[0].y = float.NaN; break;
                case "queue": snapshot.queueNext = 5; break;
                case "score": snapshot.score = -1; break;
                case "session": snapshot.sessionId = string.Empty; break;
                case "schema": snapshot.schemaVersion = 99; break;
            }

            Assert.IsFalse(snapshot.IsValidFor(Config()), broken);
        }

        [Test]
        public void RunSnapshot_CorruptFile_LoadsAsNoSavedGame()
        {
            var inner = new InMemorySaveStore();
            inner.Write(SaveKey.RunSnapshot, "{not json");
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Corrupt data"));
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("unreadable"));
            Assert.IsFalse(new RunSnapshotStore(inner).Load().active);
        }
    }
}
