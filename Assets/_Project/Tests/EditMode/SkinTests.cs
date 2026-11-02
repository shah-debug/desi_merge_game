using System;
using System.IO;
using System.Linq;
using DesiMerge.Configuration;
using DesiMerge.Core;
using DesiMerge.Cosmetics;
using DesiMerge.Persistence;
using DesiMerge.Progression;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DesiMerge.Tests.EditMode
{
    /// <summary>Item skins: buying with Diamonds once, selection, per-skin discoveries, and the shipped skin art.</summary>
    public class SkinTests
    {
        private const string CatalogPath = "Assets/_Project/ScriptableObjects/Progression/Cosmetics.asset";
        private TestContent _content;
        private MergeProgression _items;
        private CosmeticCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _content = new TestContent();
            _items = _content.Progression(levels: 4, spawnable: 2);
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
        public void Buying_SpendsTheDiamondsOnce_OwnsTheSkin_AndSurvivesRestart()
        {
            var store = new InMemorySaveStore();
            var clock = new ManualClock(new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc));
            var wallet = new PlayerProgressService(store, _items, clock);
            var skins = new CosmeticService(store, _catalog, null);
            wallet.TryGrantCoins("seed", 350);

            Assert.AreEqual("base", skins.Selected(CosmeticKind.ItemSkin).id);
            Assert.IsFalse(skins.TrySelect("sweet"), "Not owned yet.");
            Assert.AreEqual(CosmeticPurchase.Bought, skins.TryBuy("sweet", wallet));
            Assert.AreEqual(CosmeticPurchase.AlreadyOwned, skins.TryBuy("sweet", wallet), "A second tap doesn't charge again.");
            Assert.AreEqual(50, wallet.Coins);
            Assert.IsTrue(skins.TrySelect("sweet"));
            skins.Flush();

            var relaunchedWallet = new PlayerProgressService(store, _items, clock);
            var relaunched = new CosmeticService(store, _catalog, null);
            Assert.AreEqual(50, relaunchedWallet.Coins, "The purchase was saved at once.");
            Assert.IsTrue(relaunched.IsOwned("sweet"));
            Assert.AreEqual("sweet", relaunched.Selected(CosmeticKind.ItemSkin).id);
        }

        [Test]
        public void NotEnoughDiamonds_ChangesNothing()
        {
            var store = new InMemorySaveStore();
            var wallet = new PlayerProgressService(store, _items, new ManualClock(DateTime.UtcNow));
            var skins = new CosmeticService(store, _catalog, null);
            wallet.TryGrantCoins("seed", 120);
            Assert.AreEqual(CosmeticPurchase.NotEnoughDiamonds, skins.TryBuy("sweet", wallet));
            Assert.AreEqual(120, wallet.Coins);
            Assert.IsFalse(skins.IsOwned("sweet"));
            Assert.AreEqual(CosmeticPurchase.AlreadyOwned, skins.TryBuy("base", null), "The default skin is always owned.");
        }

        [Test]
        public void SkinDiscoveries_AreRecordedPerSkin_AndSaved()
        {
            var store = new InMemorySaveStore();
            var skins = new CosmeticService(store, _catalog, null);
            skins.RecordDiscovery("sweet", 2);
            skins.RecordDiscovery("sweet", 2);
            Assert.IsTrue(skins.IsDiscovered("sweet", 2));
            Assert.IsFalse(skins.IsDiscovered("base", 2), "Each skin has its own collection.");
            skins.Flush();
            Assert.IsTrue(new CosmeticService(store, _catalog, null).IsDiscovered("sweet", 2));
        }

        [Test]
        public void ShippedSkins_HaveTenArtworksEach_InsideTheColliderCircle()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CosmeticCatalog>(CatalogPath);
            Assert.IsNotNull(catalog);
            var skins = catalog.Cosmetics.Where(c => c.kind == CosmeticKind.ItemSkin).ToList();
            CollectionAssert.AreEquivalent(new[] { "desi_bazaar", "numbers", "mithai", "street_food", "truck_art" }, skins.Select(s => s.id).ToArray());
            Assert.IsTrue(skins.Where(s => s.id != "desi_bazaar").All(s => s.unlock == CosmeticUnlock.Diamonds && s.price > 0));
            var failures = 0;
            foreach (var skin in skins.Where(s => s.id != "desi_bazaar"))
            {
                Assert.AreEqual(10, skin.itemSprites.Length, skin.id);
                Assert.AreEqual(10, skin.itemNames.Length, skin.id);
                for (var level = 0; level < 10; level++)
                {
                    var sprite = skin.itemSprites[level];
                    Assert.IsNotNull(sprite, $"{skin.id} level {level}");
                    var texture = new Texture2D(2, 2);
                    texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));
                    var half = texture.width * 0.5f;
                    for (var y = 0; y < texture.height; y += 2)
                    {
                        for (var x = 0; x < texture.width; x += 2)
                        {
                            if (texture.GetPixel(x, y).a > 0.2f && new Vector2(x + 0.5f - half, y + 0.5f - half).magnitude > half)
                            {
                                failures++;
                                Debug.Log($"{skin.id} level {level} has art outside the collider circle at {x},{y}");
                                x = texture.width;
                                y = texture.height;
                            }
                        }
                    }

                    Object.DestroyImmediate(texture);
                }
            }

            Assert.AreEqual(0, failures, "Every skin item stays inside its physics circle.");
        }
    }
}
