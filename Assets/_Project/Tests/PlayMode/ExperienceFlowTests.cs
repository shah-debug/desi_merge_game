using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DesiMerge.Audio;
using DesiMerge.Gameplay;
using DesiMerge.Progression;
using DesiMerge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DesiMerge.Tests.PlayMode
{
    /// <summary>
    /// Phase 6.6 screens on the generated scenes. Scenes are opened directly, so they use in-memory development
    /// services: nothing here reads or writes the player's real save files.
    /// </summary>
    public class ExperienceFlowTests
    {
        private const string MenuScene = "MainMenu";
        private const string GameplayScene = "Gameplay";

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            var empty = SceneManager.GetSceneByName("ExperienceTestEmpty");
            SceneManager.SetActiveScene(empty.isLoaded ? empty : SceneManager.CreateScene("ExperienceTestEmpty"));
            foreach (var name in new[] { MenuScene, GameplayScene })
            {
                var scene = SceneManager.GetSceneByName(name);
                if (scene.isLoaded)
                {
                    yield return SceneManager.UnloadSceneAsync(scene);
                }
            }
        }

        private static IEnumerator LoadMenu(System.Action<MainMenuEntryPoint> onLoaded)
        {
            if (!Application.CanStreamedLevelBeLoaded(MenuScene))
            {
                Assert.Ignore("MainMenu is not in Build Settings. Run Desi Merge > Setup > Create All Content.");
            }

            LogAssert.Expect(LogType.Warning, new Regex(@"\[MainMenu\] Started without Bootstrap"));
            yield return SceneManager.LoadSceneAsync(MenuScene, LoadSceneMode.Single);
            yield return SceneTestUtility.WaitForScene(MenuScene);
            var menu = SceneTestUtility.Find<MainMenuEntryPoint>(MenuScene);
            Assert.IsTrue(menu != null && menu.IsInitialized, "Menu did not initialize.");
            yield return new WaitForSecondsRealtime(0.8f);
            onLoaded(menu);
        }

        private static IEnumerator LoadGameplay(System.Action<GameplayEntryPoint> onLoaded)
        {
            if (!Application.CanStreamedLevelBeLoaded(GameplayScene))
            {
                Assert.Ignore("Gameplay is not in Build Settings. Run Desi Merge > Setup > Create All Content.");
            }

            LogAssert.Expect(LogType.Warning, new Regex("Started without Bootstrap"));
            yield return SceneManager.LoadSceneAsync(GameplayScene, LoadSceneMode.Single);
            yield return null;
            var entry = SceneTestUtility.Find<GameplayEntryPoint>(GameplayScene);
            Assert.IsNotNull(entry?.Loop, "Gameplay did not start.");
            onLoaded(entry);
        }

        private static IEnumerator Settle() => new WaitForSecondsRealtime(0.4f);

        [UnityTest]
        public IEnumerator Menu_EveryNewButtonOpensItsScreen_OneAtATime_AndBackClosesIt()
        {
            MainMenuEntryPoint menu = null;
            yield return LoadMenu(m => menu = m);
            var scene = SceneManager.GetSceneByName(MenuScene);
            Assert.IsTrue(menu.DailyRewardsDotVisible, "A fresh player has a reward waiting.");
            Assert.AreEqual("New Player", menu.RankTitle);

            menu.DailyRewardsButton.onClick.Invoke();
            yield return Settle();
            Assert.IsTrue(menu.DailyRewards.IsOpen);
            Assert.AreEqual(7, menu.DailyRewards.Tiles.Count(tile => tile.gameObject.activeSelf));
            Assert.AreEqual(DailyRewardDayState.Available, menu.DailyRewards.Tiles[0].State);
            yield return SceneCapture.Capture(scene, "daily-rewards-9x16.png", 540, 960);
            yield return SceneCapture.Capture(scene, "daily-rewards-3x4.png", 720, 960);

            menu.MissionsButton.onClick.Invoke();
            yield return Settle();
            Assert.IsFalse(menu.Missions.IsOpen, "Only one screen is open at a time.");
            Assert.IsTrue(menu.HandleBack());
            yield return Settle();
            Assert.IsFalse(menu.DailyRewards.IsOpen);

            menu.MissionsButton.onClick.Invoke();
            yield return Settle();
            Assert.IsTrue(menu.Missions.IsOpen);
            Assert.AreEqual(MissionTab.Daily, menu.Missions.Tab);
            Assert.IsTrue(menu.Missions.DailyChallengeCard.gameObject.activeSelf, "Today's challenge is on the Daily tab.");
            Assert.AreEqual(menu.Services.Missions.Daily.Count, menu.Missions.Cards.Count(card => card.gameObject.activeSelf));
            yield return SceneCapture.Capture(scene, "missions-daily-9x16.png", 540, 960);
            menu.Missions.SelectTab(MissionTab.Weekly);
            Assert.AreEqual(menu.Services.Missions.Weekly.Count, menu.Missions.Cards.Count(card => card.gameObject.activeSelf));
            Assert.IsFalse(menu.Missions.DailyChallengeCard.gameObject.activeSelf);
            menu.Missions.SelectTab(MissionTab.Completed);
            Assert.AreEqual(0, menu.Missions.Cards.Count(card => card.gameObject.activeSelf));
            StringAssert.Contains("Claimed missions", menu.Missions.EmptyText, "Empty state explains itself.");
            Assert.IsTrue(menu.HandleBack());
            yield return Settle();

            menu.RankButton.onClick.Invoke();
            yield return Settle();
            Assert.IsTrue(menu.Ranks.IsOpen);
            Assert.AreEqual(menu.Services.Milestones.Definitions.Count, menu.Ranks.Cards.Count(card => card.gameObject.activeSelf));
            yield return SceneCapture.Capture(scene, "ranks-9x16.png", 540, 960);
            Assert.IsTrue(menu.HandleBack());
            yield return Settle();
            Assert.IsFalse(menu.AnyPanelOpen);
            yield return SceneCapture.CaptureAspectRatios(scene, "menu66");

            menu.CollectionButton.onClick.Invoke();
            yield return Settle();
            Assert.IsTrue(menu.Collection.IsOpen);
            Assert.AreEqual("Drop it", menu.Collection.Slots[0].LabelText);
            Assert.AreEqual("???", menu.Collection.Slots[1].LabelText, "Nothing is revealed before the item that makes it is discovered.");
            Assert.IsTrue(menu.HandleBack());
        }

        [UnityTest]
        public IEnumerator DailyReward_ClaimFromTheScreen_AddsTheChargeOnce_AndShowsOnePopup()
        {
            MainMenuEntryPoint menu = null;
            yield return LoadMenu(m => menu = m);
            var powerUps = menu.Services.PowerUps;
            var dayOne = menu.Services.DailyRewards.Days[0].rewards[0];
            var before = powerUps.SavedCharges(dayOne.powerUpId);

            menu.DailyRewardsButton.onClick.Invoke();
            yield return Settle();
            menu.DailyRewards.ClaimButton.onClick.Invoke();
            menu.DailyRewards.ClaimButton.onClick.Invoke();
            Assert.AreEqual(before + dayOne.amount, powerUps.SavedCharges(dayOne.powerUpId), "Rapid taps claim once.");
            Assert.IsFalse(menu.DailyRewards.ClaimInteractable);
            Assert.AreEqual(DailyRewardDayState.Claimed, menu.DailyRewards.Tiles[0].State);
            Assert.IsTrue(menu.Rewards.IsShowing);
            Assert.AreEqual(0, menu.Rewards.QueuedCount);
            StringAssert.Contains("DAY 1", menu.Rewards.Title);
            Assert.IsFalse(menu.DailyRewardsDotVisible);
            yield return new WaitForSecondsRealtime(0.5f);
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(MenuScene), "reward-popup-9x16.png", 540, 960);

            Assert.IsTrue(menu.HandleBack(), "Back collects the popup first.");
            yield return Settle();
            Assert.IsFalse(menu.Rewards.IsShowing);
            Assert.IsTrue(menu.DailyRewards.IsOpen, "The calendar stays open underneath.");
        }

        [UnityTest]
        public IEnumerator MissionAndRank_ClaimedFromTheirCards_ThroughTheRealServices()
        {
            MainMenuEntryPoint menu = null;
            yield return LoadMenu(m => menu = m);
            var services = menu.Services;
            var items = services.ItemContent;
            for (var i = 0; i < 30; i++)
            {
                services.Progress.RecordMerge(items.Get(Mathf.Min(i / 4, items.Count - 1)));
            }

            for (var game = 0; game < 3; game++)
            {
                services.PowerUps.BeginGame();
                services.Progress.SubmitFinalScore(320 + game * 100);
            }

            Assert.AreEqual("Chai Champion", menu.RankTitle, "The rank card follows the best score (520).");
            Assert.IsTrue(menu.MissionsDotVisible);

            menu.MissionsButton.onClick.Invoke();
            yield return Settle();
            var claimable = menu.Missions.Cards.First(card => card.gameObject.activeSelf && card.ClaimVisible);
            var id = claimable.Id;
            claimable.ClaimButton.onClick.Invoke();
            Assert.IsTrue(services.Missions.Daily.Concat(services.Missions.Weekly).Single(state => state.Definition.id == id).Claimed);
            Assert.IsTrue(menu.Rewards.IsShowing);
            menu.Rewards.Collect();
            menu.Missions.SelectTab(MissionTab.Completed);
            Assert.IsTrue(menu.Missions.Cards.Any(card => card.gameObject.activeSelf && card.Id == id && card.ShowsDone));
            Assert.IsTrue(menu.HandleBack());
            yield return Settle();

            menu.RankButton.onClick.Invoke();
            yield return Settle();
            var rankCard = menu.Ranks.Cards[0];
            Assert.IsTrue(rankCard.ClaimVisible);
            rankCard.ClaimButton.onClick.Invoke();
            Assert.IsTrue(services.Milestones.IsClaimed(rankCard.Id));
            Assert.IsFalse(rankCard.ClaimVisible);
            Assert.IsTrue(rankCard.ShowsDone);
            yield return new WaitForSecondsRealtime(0.5f);
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(MenuScene), "ranks-claimed-9x16.png", 540, 960);
        }

        [UnityTest]
        public IEnumerator FirstGame_ShowsTheTutorial_ItBlocksDrops_AndSkipEndsItForGood()
        {
            GameplayEntryPoint entry = null;
            yield return LoadGameplay(e => entry = e);
            var hud = entry.Hud;
            Assert.IsTrue(hud.TutorialVisible, "A player with no finished games sees the tutorial.");
            Assert.IsTrue(hud.BlocksPointer(new Vector2(Screen.width * 0.5f, Screen.height * 0.4f)), "Drops are blocked behind it.");
            yield return Settle();
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(GameplayScene), "tutorial-9x16.png", 540, 960);

            hud.Tutorial.NextButton.onClick.Invoke();
            Assert.AreEqual(1, hud.Tutorial.Page);
            hud.Tutorial.SkipButton.onClick.Invoke();
            Assert.IsFalse(hud.TutorialVisible);
            Assert.IsTrue(entry.Services.Settings.TutorialCompleted);
            yield return Settle();
            Assert.IsFalse(hud.BlocksPointer(new Vector2(Screen.width * 0.5f, Screen.height * 0.4f)));
            Assert.IsTrue(entry.Loop.TryDrop(0f), "Dropping works once the tutorial is closed.");
        }

        [UnityTest]
        public IEnumerator GameOver_ShowsTheSessionSummary_WithRealCounts()
        {
            GameplayEntryPoint entry = null;
            yield return LoadGameplay(e => entry = e);
            entry.Hud.CloseTutorial();
            entry.Board.SpawnLocal(0, new Vector2(-0.25f, 0.35f));
            entry.Board.SpawnLocal(0, new Vector2(0.25f, 0.35f));
            var deadline = Time.realtimeSinceStartup + 3f;
            while (entry.Loop.Session.MergeCount == 0 && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.AreEqual(1, entry.Loop.Session.MergeCount);
            Assert.IsTrue(entry.Loop.EndGame());
            StringAssert.Contains("1 merge", entry.Hud.SummaryText);
            StringAssert.Contains(entry.Config.Progression.Get(1).DisplayName, entry.Hud.SummaryText, "The new discovery is listed.");
            yield return new WaitForSecondsRealtime(0.6f);
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(GameplayScene), "game-over-summary-9x16.png", 540, 960);
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(GameplayScene), "game-over-summary-3x4.png", 720, 960);
        }

        [UnityTest]
        public IEnumerator Scenes_HaveExactlyOneEnabledAudioListener_OnTheAudioHost()
        {
            MainMenuEntryPoint menu = null;
            yield return LoadMenu(m => menu = m);
            var listeners = SceneManager.GetSceneByName(MenuScene).GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<AudioListener>(true)).ToList();
            Assert.AreEqual(1, listeners.Count, "Without a listener Unity plays no sound; with two it warns.");
            Assert.IsTrue(listeners[0].enabled);
            var audio = (AudioService)menu.Services.Audio;
            Assert.AreSame(listeners[0], audio.Listener);
            Assert.AreEqual(MusicTrack.Menu, audio.CurrentTrack);
            Assert.IsNotNull(audio.MusicSource.clip, "Menu music is assigned and playing.");
        }

        [UnityTest]
        public IEnumerator Splash_FinishesItsIntroOnce_WithoutWaitingLonger()
        {
            var host = new GameObject("Splash");
            var splash = host.AddComponent<SplashScreen>();
            var calls = 0;
            var started = Time.realtimeSinceStartup;
            splash.Play(() => calls++);
            while (calls == 0 && Time.realtimeSinceStartup - started < 3f)
            {
                yield return null;
            }

            var elapsed = Time.realtimeSinceStartup - started;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreEqual(1, calls);
            Assert.Less(elapsed, splash.IntroSeconds + 0.5f);
            Object.Destroy(host);
        }
    }
}
