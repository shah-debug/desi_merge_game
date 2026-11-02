using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using DesiMerge.Cosmetics;
using DesiMerge.Gameplay;
using DesiMerge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DesiMerge.Tests.PlayMode
{
    /// <summary>The Skin tag, buying and using a skin, and the per-skin collection, on the real Gameplay scene.</summary>
    public class SkinFlowTests
    {
        private const string GameplayScene = "Gameplay";

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            var empty = SceneManager.GetSceneByName("SkinEmpty");
            SceneManager.SetActiveScene(empty.isLoaded ? empty : SceneManager.CreateScene("SkinEmpty"));
            var scene = SceneManager.GetSceneByName(GameplayScene);
            if (scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        [UnityTest]
        public IEnumerator SkinTag_OpensSkins_BuyingAndUsingASkin_ReskinsTheBoardAtOnce()
        {
            if (!Application.CanStreamedLevelBeLoaded(GameplayScene))
            {
                Assert.Ignore("Gameplay scene is not in Build Settings.");
            }

            LogAssert.Expect(LogType.Warning, new Regex("Started without Bootstrap"));
            yield return SceneManager.LoadSceneAsync(GameplayScene, LoadSceneMode.Single);
            yield return null;
            var entry = SceneTestUtility.Find<GameplayEntryPoint>(GameplayScene);
            entry.Hud.CloseTutorial();
            yield return new WaitForSecondsRealtime(0.3f);
            var services = entry.Services;
            var item = entry.Board.SpawnLocal(2, new Vector2(0f, 0.6f));
            yield return new WaitForSeconds(0.3f);
            var numbers = services.Cosmetics.Definitions.Single(c => c.id == "numbers");
            services.Progress.TryGrantCoins("test:seed", numbers.price + 10);

            entry.Hud.SkinTagButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.4f);
            var skins = entry.Hud.Menu.Skins;
            Assert.IsTrue(skins.IsOpen, "The Skin tag opens the Skins screen.");
            Assert.IsTrue(entry.Loop.IsPaused, "The game waits behind it.");
            Assert.AreEqual(5, skins.Rows.Count(r => r.gameObject.activeSelf));
            Assert.AreEqual(SkinRowState.InUse, skins.Rows.Single(r => r.Id == "desi_bazaar").State);
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(GameplayScene), "skins-9x16.png", 540, 960);

            var row = skins.Rows.Single(r => r.Id == "numbers");
            Assert.AreEqual(SkinRowState.ForSale, row.State);
            row.BuyButton.onClick.Invoke();
            row.BuyButton.onClick.Invoke();
            Assert.AreEqual(10, services.Progress.Coins, "Bought once.");
            Assert.AreEqual(SkinRowState.InUse, row.State, "A bought skin is used at once.");
            var sprite = item.GetComponentInChildren<SpriteRenderer>().sprite;
            Assert.AreSame(numbers.SpriteFor(2), sprite, "Items already on the board switch to the new skin.");
            Assert.AreSame(numbers.SpriteFor(entry.Loop.Queue.Next), entry.Hud.NextItemSprite, "NEXT shows the new skin.");
            yield return new WaitForSecondsRealtime(0.3f);
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(GameplayScene), "skins-bought-9x16.png", 540, 960);

            skins.Close();
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsFalse(entry.Loop.IsPaused, "Closing Skins goes straight back to the game.");
            Assert.IsTrue(entry.Loop.TryDrop(0f));
            Assert.IsTrue(services.Cosmetics.IsDiscovered("numbers", entry.Board.ActiveItems.Last().Level), "Dropping with a skin collects that skin's item.");

            entry.Loop.OpenMenu();
            yield return new WaitForSecondsRealtime(0.3f);
            entry.Hud.Menu.CollectionButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.4f);
            var collection = entry.Hud.Menu.Collection;
            Assert.AreEqual(5, collection.PageCount, "One collection page per skin.");
            collection.ShowPage(collection.PageCount - 1);
            collection.ShowPage(0);
            for (var i = 0; i < collection.PageCount && collection.Page.id != "numbers"; i++)
            {
                collection.ShowPage(i);
            }

            Assert.AreEqual("NUMBERS", collection.SkinTitle);
            Assert.IsTrue(collection.Slots.Any(slot => slot.ShowsDiscovered));
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(GameplayScene), "collection-skin-9x16.png", 540, 960);
        }
    }
}
