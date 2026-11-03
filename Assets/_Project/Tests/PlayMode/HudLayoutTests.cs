using System.Collections;
using System.Text.RegularExpressions;
using DesiMerge.Gameplay;
using DesiMerge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DesiMerge.Tests.PlayMode
{
    /// <summary>Left-column HUD (diamonds +, power-ups), Skin tag on NEXT, daily countdown and Records tab icons.</summary>
    public class HudLayoutTests
    {
        private const string GameplayScene = "Gameplay";

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            var empty = SceneManager.GetSceneByName("HudEmpty");
            SceneManager.SetActiveScene(empty.isLoaded ? empty : SceneManager.CreateScene("HudEmpty"));
            var scene = SceneManager.GetSceneByName(GameplayScene);
            if (scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        [UnityTest]
        public IEnumerator DiamondPlus_OpensDailyRewards_WithACountdownAfterClaiming_AndTabsKeepTheirIcons()
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
            Assert.IsNotNull(entry.Hud.SkinTagPreview, "The Skin tag shows the skin in use.");
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(GameplayScene), "hud-layout-9x16.png", 540, 960);
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(GameplayScene), "hud-layout-notch.png", 540, 1170, simulateNotch: true);

            entry.Hud.GetDiamondsButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.4f);
            var daily = entry.Hud.Menu.DailyRewards;
            Assert.IsTrue(daily.IsOpen, "The diamonds + opens Daily Rewards.");
            Assert.IsTrue(entry.Loop.IsPaused);
            daily.ClaimButton.onClick.Invoke();
            entry.Hud.Menu.Rewards.Collect();
            yield return new WaitForSecondsRealtime(1.2f);
            StringAssert.IsMatch(@"^NEXT IN \d\d:\d\d:\d\d$", daily.ClaimLabel, "The claimed button counts down to the next reward.");
            var first = daily.ClaimLabel;
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.AreNotEqual(first, daily.ClaimLabel, "The countdown ticks.");
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(GameplayScene), "daily-countdown-9x16.png", 540, 960);
            daily.Close();
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsFalse(entry.Loop.IsPaused, "Closing it returns to the game.");

            entry.Loop.OpenMenu();
            yield return new WaitForSecondsRealtime(0.3f);
            entry.Hud.Menu.RecordsButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.4f);
            var records = entry.Hud.Menu.Records;
            foreach (var tab in new[] { RecordsTab.Score, RecordsTab.Achievements })
            {
                records.SelectTab(tab);
                Assert.AreNotEqual(records.TabColor(tab), records.TabIconColor(tab), $"{tab}: the active tab's icon stays visible on its background.");
            }
        }
        [UnityTest]
        public IEnumerator Hammer_DarkensAboveAndBelowTheJar_WithoutHints_AndTilesAreAnimatedIcons()
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
            Assert.IsNull(GameObject.Find("PauseButton"), "The pause button is gone (the menu button pauses).");

            foreach (var tile in entry.Hud.PowerUpButtons)
            {
                var label = tile.transform.Find("Button/Label");
                Assert.IsTrue(label == null || !label.gameObject.activeInHierarchy, tile.PowerUpId + ": no text on the tile.");
                Assert.IsNotNull(tile.GetComponentInChildren<DesiMerge.UI.Animation.IconBeat>(), tile.PowerUpId + ": animated icon.");
            }

            var hammerArt = entry.Hud.PowerUpButtons[0].GetComponentInChildren<DesiMerge.UI.Animation.IconBeat>();
            var rest = hammerArt.transform.localRotation;
            var moved = false;
            for (var t = 0f; t < 2.5f && !moved; t += Time.unscaledDeltaTime)
            {
                moved |= Quaternion.Angle(rest, hammerArt.transform.localRotation) > 5f;
                yield return null;
            }

            Assert.IsTrue(moved, "The hammer icon swings within a 2 s beat.");

            entry.Board.SpawnLocal(0, new Vector2(0f, 0.4f));
            entry.Board.SpawnLocal(1, new Vector2(1.2f, 0.5f));
            yield return new WaitForSeconds(0.3f);
            entry.Loop.UsePowerUp("hammer");
            yield return new WaitForSecondsRealtime(0.6f);
            var shade = entry.Hud.FocusShade;
            Assert.IsTrue(shade.Visible && shade.Amount >= 1f, "The focus shade slid in.");
            // The top band runs from the screen top down to the jar's top edge, the bottom band up to its floor.
            var corners = new Vector3[4];
            shade.TopBand.GetWorldCorners(corners);
            var cam = Camera.main;
            Assert.AreEqual(cam.WorldToScreenPoint(shade.JarTopWorld).y, corners[0].y, Screen.height * 0.02f, "Top band ends at the jar top.");
            Assert.AreEqual(Screen.height, corners[1].y, 2f, "Top band starts at the screen top.");
            shade.BottomBand.GetWorldCorners(corners);
            Assert.AreEqual(Mathf.Max(0f, cam.WorldToScreenPoint(shade.JarBottomWorld).y), corners[1].y, Screen.height * 0.02f, "Bottom band ends at the jar floor.");
            Assert.IsNull(GameObject.Find("TargetingBanner"), "No hint banner.");
            Assert.IsFalse(entry.Hud.Toast.IsShowing, "No hint toast while picking.");
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(GameplayScene), "hammer-focus-9x16.png", 540, 960);
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(GameplayScene), "hammer-focus-notch.png", 540, 1170, simulateNotch: true);

            entry.Loop.CancelPowerUp();
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsFalse(shade.gameObject.activeSelf, "The shade slides away when cancelled.");

            entry.Loop.UsePowerUp("shuffle");
            yield return null;
            Assert.IsFalse(entry.Hud.Toast.IsShowing, "Shuffle shows no hint toast.");
        }
    }
}
