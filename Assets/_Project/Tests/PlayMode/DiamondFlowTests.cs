using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using DesiMerge.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DesiMerge.Tests.PlayMode
{
    /// <summary>Buying power-ups with Diamonds and earning them per game, on the real Gameplay scene (in-memory saves).</summary>
    public class DiamondFlowTests
    {
        private const string GameplayScene = "Gameplay";

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            var empty = SceneManager.GetSceneByName("DiamondEmpty");
            SceneManager.SetActiveScene(empty.isLoaded ? empty : SceneManager.CreateScene("DiamondEmpty"));
            var scene = SceneManager.GetSceneByName(GameplayScene);
            if (scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        private static IEnumerator Load(System.Action<GameplayEntryPoint> onLoaded)
        {
            if (!Application.CanStreamedLevelBeLoaded(GameplayScene))
            {
                Assert.Ignore("Gameplay scene is not in Build Settings.");
            }

            LogAssert.Expect(LogType.Warning, new Regex("Started without Bootstrap"));
            yield return SceneManager.LoadSceneAsync(GameplayScene, LoadSceneMode.Single);
            yield return null;
            var entry = SceneTestUtility.Find<GameplayEntryPoint>(GameplayScene);
            Assert.IsNotNull(entry?.Loop);
            entry.Hud.CloseTutorial();
            yield return new WaitForSecondsRealtime(0.3f);
            onLoaded(entry);
        }

        private static IEnumerator Smash(GameplayEntryPoint entry, MergeItem item, PowerUpResult expected)
        {
            Assert.AreEqual(PowerUpResult.TargetingStarted, entry.Loop.UsePowerUp("hammer"));
            Assert.AreEqual(expected, entry.Loop.SelectPowerUpTarget(entry.Board.WorldToLocal(item.transform.position)));
            yield return new WaitForSecondsRealtime(0.6f);
        }

        [UnityTest]
        public IEnumerator Hammer_WhenUsesRunOut_ShowsItsPrice_AndCostsDiamondsOnlyWhenItSmashes()
        {
            GameplayEntryPoint entry = null;
            yield return Load(e => entry = e);
            var progress = entry.Services.Progress;
            progress.TryGrantCoins("test:seed", 45);
            var hammer = entry.Hud.PowerUpButtons.Single(b => b.PowerUpId == "hammer");
            Assert.AreEqual("45", entry.Hud.Diamonds.AmountLabel, "The counter shows the saved balance.");
            Assert.IsFalse(hammer.ShowsPrice, "A free use is left, so no price yet.");

            var first = entry.Board.SpawnLocal(2, new Vector2(-1.5f, 0.6f));
            var second = entry.Board.SpawnLocal(3, new Vector2(1.5f, 0.6f));
            var third = entry.Board.SpawnLocal(1, new Vector2(0f, 0.5f));
            yield return new WaitForSeconds(0.5f);
            yield return Smash(entry, first, PowerUpResult.Used);
            Assert.AreEqual(45, progress.Coins, "The free use costs nothing.");
            Assert.IsTrue(hammer.ShowsPrice);
            Assert.AreEqual("30", hammer.PriceLabel);
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(GameplayScene), "diamonds-price-9x16.png", 540, 960);

            Assert.AreEqual(PowerUpResult.TargetingStarted, entry.Loop.UsePowerUp("hammer"));
            Assert.AreEqual(PowerUpResult.MissedTarget, entry.Loop.SelectPowerUpTarget(new Vector2(0f, 8f)));
            Assert.AreEqual(45, progress.Coins, "A miss is free.");
            entry.Loop.CancelPowerUp();
            Assert.AreEqual(45, progress.Coins, "Cancelling is free.");

            yield return Smash(entry, second, PowerUpResult.Used);
            Assert.AreEqual(15, progress.Coins, "Bought: 30 diamonds, paid when the item was smashed.");
            Assert.AreEqual(2, entry.Services.Missions.PendingPowerUpUses.Count(id => id == "hammer"), "The bought Hammer counts as a use, like the free one.");
            Assert.IsTrue(entry.Services.Missions.PowerUpUsedThisGame);
            Assert.AreEqual("15", entry.Hud.Diamonds.AmountLabel);
            Assert.IsTrue(hammer.ShowsUnavailable, "15 diamonds can't buy a 30-diamond Hammer.");
            Assert.AreEqual(PowerUpResult.NoCharges, entry.Loop.UsePowerUp("hammer"));
            Assert.AreEqual(15, progress.Coins);
            Assert.IsTrue(third.isActiveAndEnabled);
        }

        [UnityTest]
        public IEnumerator FinishedGame_EarnsOneDiamondPer50Points_Once()
        {
            GameplayEntryPoint entry = null;
            yield return Load(e => entry = e);
            var level = entry.Config.Progression.FinalLevel - 1;
            var radius = entry.Config.Progression.Get(level).Radius * 2f * MergeItem.FittedRadius(entry.Config.Progression.Get(level).Sprite);
            entry.Board.SpawnLocal(level, new Vector2(-radius * 0.98f, radius + 0.1f));
            entry.Board.SpawnLocal(level, new Vector2(radius * 0.98f, radius + 0.1f));
            var deadline = Time.realtimeSinceStartup + 3f;
            while (entry.Loop.Session.Score == 0 && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            var score = entry.Loop.Session.Score;
            Assert.GreaterOrEqual(score, GameplayLoop.PointsPerDiamond);
            Assert.IsTrue(entry.Loop.EndGame());
            var expected = score / GameplayLoop.PointsPerDiamond;
            Assert.AreEqual(expected, entry.Services.Progress.Coins);
            StringAssert.Contains($"+{expected} diamonds", entry.Hud.SummaryText);
            Assert.IsFalse(entry.Loop.EndGame());
            Assert.AreEqual(expected, entry.Services.Progress.Coins, "Only one payout per game.");
            yield return new WaitForSecondsRealtime(0.6f);
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(GameplayScene), "diamonds-earned-9x16.png", 540, 960);
        }
    }
}
