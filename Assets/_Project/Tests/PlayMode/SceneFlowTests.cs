using System.Collections;
using System.Text.RegularExpressions;
using DesiMerge.Core;
using DesiMerge.Gameplay;
using DesiMerge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DesiMerge.Tests.PlayMode
{
    /// <summary>
    /// End-to-end flow on the generated scenes: MainMenu → Play → Gameplay → Pause/Resume → Game over →
    /// Main Menu. Scenes are opened directly, so each uses in-memory development services (no real saves).
    /// </summary>
    public class SceneFlowTests
    {
        private const string MenuScene = "MainMenu";
        private const string GameplayScene = "Gameplay";

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            SafeAreaFitter.SimulatedSafeArea = null;
            var empty = SceneManager.CreateScene("FlowTestEmpty");
            SceneManager.SetActiveScene(empty);
            foreach (var name in new[] { MenuScene, GameplayScene })
            {
                var scene = SceneManager.GetSceneByName(name);
                if (scene.isLoaded)
                {
                    yield return SceneManager.UnloadSceneAsync(scene);
                }
            }
        }

        [UnityTest]
        public IEnumerator MenuToGameplayAndBack_WorksWithoutDuplicatesOrFrozenTime()
        {
            if (!Application.CanStreamedLevelBeLoaded(MenuScene) || !Application.CanStreamedLevelBeLoaded(GameplayScene))
            {
                Assert.Ignore("MainMenu/Gameplay are not in Build Settings. Run Desi Merge > Setup > Create All Content.");
            }

            // --- Main menu ---
            LogAssert.Expect(LogType.Warning, new Regex(@"\[MainMenu\] Started without Bootstrap"));
            yield return SceneManager.LoadSceneAsync(MenuScene, LoadSceneMode.Single);
            yield return SceneTestUtility.WaitForScene(MenuScene);
            var menu = SceneTestUtility.Find<MainMenuEntryPoint>(MenuScene);
            Assert.IsNotNull(menu);
            Assert.IsTrue(menu.IsInitialized);
            Assert.AreEqual(GameState.MainMenu, menu.Services.GameState.Current);
            yield return new WaitForSecondsRealtime(0.9f); // let entrance animations finish
            yield return SceneCapture.CaptureAspectRatios(SceneManager.GetSceneByName(MenuScene), "menu");

            menu.SettingsButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsTrue(menu.Settings.IsOpen);
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(MenuScene), "menu-settings-9x16.png", 540, 960);
            menu.Settings.Close();
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsFalse(menu.Settings.IsOpen);

            // --- Play (double tap) ---
            LogAssert.Expect(LogType.Warning, new Regex(@"\[Gameplay\] Started without Bootstrap"));
            menu.PlayButton.onClick.Invoke();
            Assert.IsTrue(menu.Services.Navigator.IsLoading);
            Assert.IsFalse(menu.Services.Navigator.LoadGameplay(), "A second Play request must be ignored while loading.");
            Assert.IsFalse(menu.PlayButton.interactable);
            yield return SceneTestUtility.WaitForScene(GameplayScene);

            var game = SceneTestUtility.Find<GameplayEntryPoint>(GameplayScene);
            Assert.IsNotNull(game);
            Assert.IsNotNull(game.Loop, "Play must open the real gameplay flow.");
            Assert.AreEqual(GameState.Playing, game.Services.GameState.Current);
            var musicPlayers = CountAudioSources();
            Assert.AreEqual(7, musicPlayers, "Exactly one audio host (1 music + 6 SFX voices) after the scene change.");

            foreach (var x in new[] { -2f, -0.8f, 0.4f, 1.6f, -1.4f, 2.2f })
            {
                Assert.IsTrue(game.Loop.TryDrop(x));
                yield return new WaitForSeconds(game.Config.DropCooldownSeconds + 0.05f);
            }

            yield return new WaitForSeconds(1.2f);
            yield return SceneCapture.CaptureAspectRatios(game.gameObject.scene, "gameplay");

            // --- Pause / Resume ---
            Assert.IsTrue(game.Loop.Pause());
            Assert.AreEqual(0f, Time.timeScale);
            var countBefore = game.Board.ActiveCount;
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.AreEqual(countBefore, game.Board.ActiveCount);
            yield return SceneCapture.Capture(game.gameObject.scene, "gameplay-pause-9x16.png", 540, 960);
            Assert.IsTrue(game.Loop.Resume());
            Assert.AreEqual(1f, Time.timeScale);

            // --- Game over shows the real final score ---
            var finalScore = game.Loop.Session.Score;
            Assert.IsTrue(game.Loop.EndGame());
            yield return new WaitForSecondsRealtime(0.6f);
            var finalScoreText = FindText(GameplayScene, "FinalScore");
            Assert.AreEqual(finalScore.ToString(), finalScoreText.text);
            yield return SceneCapture.CaptureAspectRatios(game.gameObject.scene, "gameplay-game-over");

            // --- Main menu (double tap) ---
            LogAssert.Expect(LogType.Warning, new Regex(@"\[MainMenu\] Started without Bootstrap"));
            Assert.IsTrue(game.Loop.ReturnToMainMenu());
            Assert.IsFalse(game.Loop.ReturnToMainMenu());
            yield return SceneTestUtility.WaitForScene(MenuScene);
            Assert.IsFalse(SceneManager.GetSceneByName(GameplayScene).isLoaded);
            Assert.AreEqual(1f, Time.timeScale);
            var menuAgain = SceneTestUtility.Find<MainMenuEntryPoint>(MenuScene);
            Assert.IsTrue(menuAgain.IsInitialized);
            Assert.IsTrue(menuAgain.PlayButton.interactable);
            Assert.AreEqual(musicPlayers, CountAudioSources(), "Returning to the menu must not add audio players.");
        }

        private static int CountAudioSources()
        {
            return Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include).Length;
        }

        private static Text FindText(string sceneName, string objectName)
        {
            foreach (var root in SceneManager.GetSceneByName(sceneName).GetRootGameObjects())
            {
                foreach (var text in root.GetComponentsInChildren<Text>(true))
                {
                    if (text.name == objectName)
                    {
                        return text;
                    }
                }
            }

            Assert.Fail($"No Text named '{objectName}' in {sceneName}.");
            return null;
        }
    }
}
