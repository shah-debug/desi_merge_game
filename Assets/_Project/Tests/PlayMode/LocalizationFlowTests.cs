using System.Collections;
using System.Text.RegularExpressions;
using DesiMerge.Gameplay;
using DesiMerge.Localization;
using DesiMerge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DesiMerge.Tests.PlayMode
{
    /// <summary>
    /// The Settings language picker on the generated scenes: every visible label switches at once, Urdu uses the
    /// bundled Naskh font. Scenes run on in-memory services, so the player's real settings are never touched.
    /// </summary>
    public class LocalizationFlowTests
    {
        private const string MenuScene = "MainMenu";
        private const string GameplayScene = "Gameplay";

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Loc.SetLanguage("en");
            Time.timeScale = 1f;
            var empty = SceneManager.GetSceneByName("LocalizationEmpty");
            SceneManager.SetActiveScene(empty.isLoaded ? empty : SceneManager.CreateScene("LocalizationEmpty"));
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
        public IEnumerator SettingsPicker_SwitchesTheWholeMenu_ToUrduAndChinese()
        {
            if (!Application.CanStreamedLevelBeLoaded(MenuScene))
            {
                Assert.Ignore("MainMenu is not in Build Settings.");
            }

            LogAssert.Expect(LogType.Warning, new Regex(@"\[MainMenu\] Started without Bootstrap"));
            yield return SceneManager.LoadSceneAsync(MenuScene, LoadSceneMode.Single);
            yield return SceneTestUtility.WaitForScene(MenuScene);
            var menu = SceneTestUtility.Find<MainMenuEntryPoint>(MenuScene);
            Assert.IsTrue(menu != null && menu.IsInitialized, "Menu did not initialize.");
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.AreEqual("en", Loc.Current, "English is the default.");

            var playLabel = menu.PlayButton.GetComponentInChildren<Text>(true);
            var englishFont = playLabel.font;
            Assert.AreEqual("PLAY", playLabel.text);

            menu.SettingsButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.5f);
            var settings = menu.Settings;
            Assert.AreEqual("English", settings.LanguageLabel);

            settings.ToggleLanguageList();
            Assert.IsTrue(settings.LanguageListOpen, "The language box opens a dropdown.");
            Assert.AreEqual(Loc.Languages.Length, settings.LanguageOptionCount);
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(MenuScene), "settings-language-dropdown-9x16.png", 540, 960);
            settings.SelectLanguage("ur");
            yield return null;
            Assert.IsFalse(settings.LanguageListOpen, "Choosing a language closes the dropdown.");
            Assert.AreEqual("ur", Loc.Current);
            Assert.AreEqual(UrduShaper.Shape("اردو"), settings.LanguageLabel);
            Assert.AreEqual(Loc.T("PLAY"), playLabel.text, "Labels behind the panel switch too.");
            Assert.AreNotSame(englishFont, playLabel.font, "Urdu uses the Naskh font.");
            Assert.AreEqual(TextAnchor.MiddleCenter, playLabel.alignment);
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(MenuScene), "settings-urdu-9x16.png", 540, 960);
            settings.Close();
            yield return new WaitForSecondsRealtime(0.5f);
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(MenuScene), "menu-urdu-9x16.png", 540, 960);

            menu.SettingsButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.5f);
            settings.SelectLanguage("zh");
            yield return null;
            Assert.AreEqual("zh", Loc.Current);
            Assert.AreEqual("开始", playLabel.text);
            settings.Close();
            yield return new WaitForSecondsRealtime(0.5f);
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(MenuScene), "menu-chinese-9x16.png", 540, 960);

            settings.SelectLanguage("en");
            yield return null;
            Assert.AreEqual("en", Loc.Current);
            Assert.AreEqual("PLAY", playLabel.text);
            Assert.AreSame(englishFont, playLabel.font, "English gets its own font back.");
        }

        [UnityTest]
        public IEnumerator Gameplay_HudAndTutorial_ShowInUrdu()
        {
            if (!Application.CanStreamedLevelBeLoaded(GameplayScene))
            {
                Assert.Ignore("Gameplay is not in Build Settings.");
            }

            LogAssert.Expect(LogType.Warning, new Regex("Started without Bootstrap"));
            yield return SceneManager.LoadSceneAsync(GameplayScene, LoadSceneMode.Single);
            yield return null;
            var entry = SceneTestUtility.Find<GameplayEntryPoint>(GameplayScene);
            Assert.IsNotNull(entry?.Loop, "Gameplay did not start.");
            Loc.SetLanguage("ur");
            yield return new WaitForSecondsRealtime(0.5f);
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(GameplayScene), "tutorial-urdu-9x16.png", 540, 960);
            entry.Hud.CloseTutorial();
            yield return new WaitForSecondsRealtime(0.3f);
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(GameplayScene), "hud-urdu-9x16.png", 540, 960);
            Loc.SetLanguage("de");
            yield return null;
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(GameplayScene), "hud-german-9x16.png", 540, 960);
        }
    }
}
