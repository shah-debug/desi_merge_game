using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using DesiMerge.Gameplay;
using DesiMerge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DesiMerge.Tests.PlayMode
{
    /// <summary>Live best score, power-up effects, centred button labels and rounded progress fills (real Gameplay scene).</summary>
    public class PolishTests
    {
        private const string GameplayScene = "Gameplay";

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            var empty = SceneManager.GetSceneByName("PolishEmpty");
            SceneManager.SetActiveScene(empty.isLoaded ? empty : SceneManager.CreateScene("PolishEmpty"));
            var scene = SceneManager.GetSceneByName(GameplayScene);
            if (scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        private static IEnumerator Load(System.Action<GameplayEntryPoint> onLoaded, bool closeTutorial = true)
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
            if (closeTutorial)
            {
                entry.Hud.CloseTutorial();
            }

            yield return new WaitForSecondsRealtime(0.3f);
            onLoaded(entry);
        }

        [UnityTest]
        public IEnumerator BestScore_FollowsTheScoreLive_OnceItIsBeaten()
        {
            GameplayEntryPoint entry = null;
            yield return Load(e => entry = e);
            Assert.AreEqual("0", entry.Hud.BestLabel);
            entry.Board.SpawnLocal(1, new Vector2(-0.3f, 0.5f));
            entry.Board.SpawnLocal(1, new Vector2(0.3f, 0.5f));
            var deadline = Time.realtimeSinceStartup + 3f;
            while (entry.Loop.Session.Score == 0 && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.Greater(entry.Loop.Session.Score, 0);
            Assert.AreEqual(entry.Loop.Session.Score.ToString(), entry.Hud.BestLabel, "BEST rises with a new best score during play.");
            Assert.AreEqual(0, entry.Services.Progress.HighScore, "The saved best still changes only at game over.");
        }

        [UnityTest]
        public IEnumerator Colliders_FitTheDrawnItem_SoItemsTouch()
        {
            GameplayEntryPoint entry = null;
            yield return Load(e => entry = e);
            var tomato = entry.Board.SpawnLocal(3, new Vector2(-1f, 0.8f));
            var roti = entry.Board.SpawnLocal(4, new Vector2(1f, 0.8f));
            yield return null;
            foreach (var item in new[] { tomato, roti })
            {
                var radius = item.GetComponent<CircleCollider2D>().radius;
                Assert.Less(radius, 0.49f, $"{item.Definition.DisplayName}: the circle hugs the drawing, not the whole picture.");
                Assert.GreaterOrEqual(radius, 0.34f);
            }

            yield return new WaitForSeconds(1.5f);
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(GameplayScene), "items-fitted-9x16.png", 540, 960);
        }

        [UnityTest]
        public IEnumerator Hammer_ZoomsIn_ThenSwingsAndSmashes_ThenZoomsBack()
        {
            GameplayEntryPoint entry = null;
            yield return Load(e => entry = e);
            var effects = entry.Hud.Effects;
            Assert.IsNotNull(effects);
            var item = entry.Board.SpawnLocal(3, new Vector2(0f, 1.2f));
            yield return new WaitForSeconds(0.6f);
            var camera = Camera.main;
            var normalSize = camera.orthographicSize;

            entry.Loop.UsePowerUp("hammer");
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.Less(camera.orthographicSize, normalSize * 0.95f, "The camera zooms in while choosing a target.");
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(GameplayScene), "hammer-zoom-9x16.png", 540, 960);

            Assert.AreEqual(PowerUpResult.Used, entry.Loop.SelectPowerUpTarget(entry.Board.WorldToLocal(item.transform.position)));
            Assert.IsTrue(effects.HammerPlaying);
            yield return new WaitForSecondsRealtime(0.1f);
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(GameplayScene), "hammer-smash-9x16.png", 540, 960);
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.IsFalse(effects.HammerPlaying);
            Assert.AreEqual(normalSize, camera.orthographicSize, 0.001f, "The camera returns exactly to its normal size.");
            Assert.AreEqual(0, entry.Board.ActiveCount);
        }

        [UnityTest]
        public IEnumerator Shuffle_ZoomsIn_AndItemsGlideToTheirNewPlaces()
        {
            GameplayEntryPoint entry = null;
            yield return Load(e => entry = e);
            for (var i = 0; i < 5; i++)
            {
                entry.Board.SpawnLocal(i % 3, new Vector2(-2f + i, 0.6f + (i % 2) * 0.8f));
            }

            yield return new WaitForSeconds(1f);
            var camera = Camera.main;
            var normalSize = camera.orthographicSize;
            Assert.AreEqual(PowerUpResult.Used, entry.Loop.UsePowerUp("shuffle"));
            var gliding = entry.Board.ActiveItems.Count(item => item.GetComponentInChildren<SpriteRenderer>().transform.localPosition.sqrMagnitude > 0.0001f);
            Assert.Greater(gliding, 0, "The art starts at the old places and glides to the new ones.");
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.Less(camera.orthographicSize, normalSize * 0.97f, "The camera zooms in during the shuffle.");
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsTrue(entry.Board.ActiveItems.All(item => item.GetComponentInChildren<SpriteRenderer>().transform.localPosition.sqrMagnitude < 0.0001f), "Every item's art ends on its body.");
            Assert.AreEqual(normalSize, camera.orthographicSize, 0.001f);
        }

        [UnityTest]
        public IEnumerator ButtonLabels_StayCentred_WhenTheTextChanges()
        {
            GameplayEntryPoint entry = null;
            yield return Load(e => entry = e, closeTutorial: false);
            var tutorial = entry.Hud.Tutorial;
            while (tutorial.Page + 1 < tutorial.PageCount)
            {
                tutorial.NextButton.onClick.Invoke();
            }

            yield return null;
            AssertCentred(tutorial.NextButton, "LET'S PLAY");
            yield return SceneCapture.Capture(SceneManager.GetSceneByName(GameplayScene), "tutorial-last-9x16.png", 540, 960);
            tutorial.SkipButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.3f);

            entry.Loop.OpenMenu();
            yield return new WaitForSecondsRealtime(0.4f);
            AssertCentred(entry.Hud.Menu.HelpButton, "HOW TO PLAY");
            AssertCentred(entry.Hud.Menu.ResumeButton, "RETURN TO GAME");
        }

        private static void AssertCentred(Button button, string expected)
        {
            var layout = button.GetComponent<IconLabelLayout>();
            Assert.IsNotNull(layout, $"{button.name} uses the centring layout.");
            var label = button.transform.Find("Label").GetComponent<Text>();
            var icon = (RectTransform)button.transform.Find("Icon");
            Assert.AreEqual(expected, label.text);
            var buttonRect = ((RectTransform)button.transform).rect;
            var left = icon.anchoredPosition.x - icon.rect.width * 0.5f;
            var right = label.rectTransform.anchoredPosition.x + label.preferredWidth * 0.5f;
            Assert.AreEqual(0f, (left + right) * 0.5f, 6f, $"{expected}: icon + text are centred as one group.");
            Assert.LessOrEqual(right, buttonRect.width * 0.5f, $"{expected}: the text fits inside the button.");
        }

        [UnityTest]
        public IEnumerator ProgressFills_KeepTheirRoundedShape()
        {
            GameplayEntryPoint entry = null;
            yield return Load(e => entry = e);
            entry.Loop.OpenMenu();
            yield return new WaitForSecondsRealtime(0.3f);
            entry.Hud.Menu.MissionsButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.4f);
            var fills = entry.Hud.Menu.Missions.GetComponentsInChildren<SlicedFill>(true);
            Assert.IsNotEmpty(fills, "Mission progress bars use the rounded fill.");
            var fill = fills[0].GetComponent<Image>();
            Assert.AreEqual(Image.Type.Sliced, fill.type, "Sliced keeps the pill's round ends (Filled squashed them).");
            fill.fillAmount = 0.5f;
            yield return null;
            var track = ((RectTransform)fill.transform.parent).rect.width;
            Assert.AreEqual(track * 0.5f, fill.rectTransform.rect.width, 1f);
            fill.fillAmount = 0.02f;
            yield return null;
            Assert.GreaterOrEqual(fill.rectTransform.rect.width, fill.rectTransform.rect.height - 0.5f, "A small value still shows a full round cap.");
        }
    }
}
