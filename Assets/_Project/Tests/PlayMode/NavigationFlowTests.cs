using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using DesiMerge.Core;
using DesiMerge.Gameplay;
using DesiMerge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DesiMerge.Tests.PlayMode
{
    /// <summary>In-game hamburger menu, Records and the player profile on the real scenes (in-memory saves only).</summary>
    public class NavigationFlowTests
    {
        private const string GameplayScene = "Gameplay";
        private const string MenuScene = "MainMenu";

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            var empty = SceneManager.GetSceneByName("NavigationEmpty");
            SceneManager.SetActiveScene(empty.isLoaded ? empty : SceneManager.CreateScene("NavigationEmpty"));
            foreach (var name in new[] { GameplayScene, MenuScene })
            {
                var scene = SceneManager.GetSceneByName(name);
                if (scene.isLoaded)
                {
                    yield return SceneManager.UnloadSceneAsync(scene);
                }
            }
        }

        private static IEnumerator LoadGameplay(System.Action<GameplayEntryPoint> onLoaded)
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

        private static IEnumerator Settle() => new WaitForSecondsRealtime(0.4f);

        [UnityTest]
        public IEnumerator HamburgerMenu_PausesTheGame_BlocksDrops_AndOutsideTapResumes()
        {
            GameplayEntryPoint entry = null;
            yield return LoadGameplay(e => entry = e);
            var hud = entry.Hud;
            entry.Board.SpawnLocal(0, new Vector2(0f, 1f));

            hud.MenuButton.onClick.Invoke();
            yield return Settle();
            Assert.IsTrue(hud.MenuVisible);
            Assert.IsTrue(entry.Loop.IsPaused);
            Assert.IsTrue(entry.Loop.MenuOpen);
            Assert.AreEqual(0f, Time.timeScale, "The game is frozen behind the menu.");
            Assert.IsFalse(entry.Loop.TryDrop(0f), "No drops while the menu is open.");
            hud.MenuButton.onClick.Invoke();
            Assert.IsTrue(hud.MenuVisible, "A second tap doesn't open a second menu.");
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(GameplayScene), "game-menu-9x16.png", 540, 960);
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(GameplayScene), "game-menu-notch.png", 540, 1170, simulateNotch: true);

            hud.Menu.OutsideButton.onClick.Invoke();
            yield return Settle();
            Assert.IsFalse(hud.MenuVisible);
            Assert.IsFalse(entry.Loop.IsPaused);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.AreEqual(1, entry.Board.ActiveCount, "The game state is preserved.");
            Assert.IsTrue(entry.Loop.TryDrop(0.5f));
        }

        [UnityTest]
        public IEnumerator Menu_OpensRealScreens_OneAtATime_AndBackClosesTopFirst()
        {
            GameplayEntryPoint entry = null;
            yield return LoadGameplay(e => entry = e);
            var hud = entry.Hud;
            var menu = hud.Menu;
            entry.Services.Progress.SubmitFinalScore(260);
            entry.Loop.OpenMenu();
            yield return Settle();

            menu.RecordsButton.onClick.Invoke();
            yield return Settle();
            Assert.IsTrue(menu.Records.IsOpen);
            Assert.AreEqual(RecordsTab.Score, menu.Records.Tab);
            Assert.AreEqual("260", menu.Records.BestLabel, "Score tab shows the saved best score.");
            Assert.AreEqual(1, menu.Records.RecentRows.Count(row => row.gameObject.activeSelf));
            Assert.IsTrue(menu.Records.ScorePageVisible);
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(GameplayScene), "records-score-9x16.png", 540, 960);

            menu.Records.SelectTab(RecordsTab.Achievements);
            Assert.IsTrue(menu.Records.AchievementsPageVisible);
            Assert.AreEqual(entry.Services.Achievements.Definitions.Count, menu.Records.AchievementRows.Count(row => row.gameObject.activeSelf));
            Assert.IsTrue(menu.Records.AchievementRows.All(row => row.LeadingIcon != null), "Every achievement has an icon.");
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(GameplayScene), "records-achievements-9x16.png", 540, 960);

            menu.CollectionButton.onClick.Invoke();
            Assert.IsFalse(menu.Collection.IsOpen, "Only one screen at a time.");

            Assert.IsTrue(hud.CloseMenuScreen(), "Back closes Records first...");
            yield return Settle();
            Assert.IsFalse(menu.Records.IsOpen);
            Assert.IsTrue(hud.MenuVisible, "...the menu stays.");
            Assert.IsTrue(entry.Loop.IsPaused);

            foreach (var open in new System.Action[] { menu.DailyRewardsButton.onClick.Invoke, menu.MissionsButton.onClick.Invoke, menu.SettingsButton.onClick.Invoke, menu.CollectionButton.onClick.Invoke, menu.AchievementsButton.onClick.Invoke })
            {
                open();
                yield return Settle();
                Assert.IsTrue(menu.AnyScreenOpen, "Every menu item opens a real screen.");
                Assert.IsTrue(hud.CloseMenuScreen());
                yield return Settle();
            }

            Assert.IsFalse(hud.CloseMenuScreen(), "Only the menu itself is open now.");
            Assert.IsTrue(entry.Loop.HandleBack(), "Back on the bare menu resumes the game.");
            yield return Settle();
            Assert.IsFalse(hud.MenuVisible);
            Assert.AreEqual(GameState.Playing, entry.Services.GameState.Current);
        }

        [UnityTest]
        public IEnumerator Menu_HowToPlay_ShowsTheTutorialOverThePausedGame()
        {
            GameplayEntryPoint entry = null;
            yield return LoadGameplay(e => entry = e);
            entry.Loop.OpenMenu();
            yield return Settle();
            entry.Hud.Menu.HelpButton.onClick.Invoke();
            yield return Settle();
            Assert.IsTrue(entry.Hud.TutorialVisible);
            Assert.IsTrue(entry.Loop.IsPaused);
            entry.Hud.Tutorial.SkipButton.onClick.Invoke();
            yield return Settle();
            Assert.IsFalse(entry.Hud.TutorialVisible);
            Assert.IsTrue(entry.Hud.MenuVisible, "Back to the menu after the help.");
        }

        [UnityTest]
        public IEnumerator Profile_NewPlayerThenReturningPlayer_ShowsRealProgress()
        {
            if (!Application.CanStreamedLevelBeLoaded(MenuScene))
            {
                Assert.Ignore("MainMenu is not in Build Settings.");
            }

            LogAssert.Expect(LogType.Warning, new Regex(@"\[MainMenu\] Started without Bootstrap"));
            yield return SceneManager.LoadSceneAsync(MenuScene, LoadSceneMode.Single);
            yield return SceneTestUtility.WaitForScene(MenuScene);
            var menu = SceneTestUtility.Find<MainMenuEntryPoint>(MenuScene);
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.AreEqual("New Player", menu.RankTitle);
            Assert.IsTrue(menu.NewPlayerBadgeVisible);
            Assert.AreEqual("Profile", menu.ProfileAvatar.name, "A new player has the default avatar.");
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(MenuScene), "profile-new-9x16.png", 540, 960);

            var items = menu.Services.ItemContent;
            menu.Services.Progress.RecordMerge(items.Get(3));
            menu.Services.Progress.SubmitFinalScore(140);
            yield return null;
            Assert.AreEqual("Naya Khiladi", menu.RankTitle, "The name line follows the real rank.");
            Assert.IsFalse(menu.NewPlayerBadgeVisible);
            Assert.AreSame(items.Get(3).Sprite, menu.ProfileAvatar, "The avatar is the player's biggest item.");
            Assert.AreEqual(Color.white, menu.ProfileAvatarColor, "Item art is shown in its own colours, not tinted.");
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(MenuScene), "profile-returning-9x16.png", 540, 960);

            menu.AchievementsButton.onClick.Invoke();
            yield return Settle();
            Assert.IsTrue(menu.Records.IsOpen);
            Assert.AreEqual(RecordsTab.Achievements, menu.Records.Tab);
            Assert.IsTrue(menu.HandleBack());
        }
    }
}
