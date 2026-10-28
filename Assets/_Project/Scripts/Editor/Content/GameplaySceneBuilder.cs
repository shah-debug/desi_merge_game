using DesiMerge.Configuration;
using DesiMerge.Gameplay;
using DesiMerge.UI;
using DesiMerge.UI.Animation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.Editor.Content
{
    /// <summary>
    /// Gameplay.unity: the Phase 2 board, drop controller and entry point (unchanged behaviour) with a themed
    /// backdrop, merge sparkles and the Phase 3 HUD (top bar, pause, game over, popups).
    /// </summary>
    internal static class GameplaySceneBuilder
    {
        public static void Populate()
        {
            var factory = new UiFactory(ContentSetup.LoadTheme());
            var theme = factory.Theme;
            var config = AssetDatabase.LoadAssetAtPath<GameplayConfig>(GameplaySetup.GameplayConfigPath);
            var itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ContentPaths.ItemPrefabPath).GetComponent<MergeItem>();
            var square = AssetDatabase.LoadAssetAtPath<Sprite>(GameplaySetup.SquareSpritePath);

            // Camera: 16 world units tall; the board's floor sits near the bottom and the HUD uses the top.
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = theme.background;
            cameraObject.AddComponent<CameraFit>();

            // Faint truck-art pattern behind everything; no collider, never covers items.
            var backdrop = new GameObject("Backdrop").AddComponent<SpriteRenderer>();
            backdrop.sprite = theme.pattern;
            backdrop.drawMode = SpriteDrawMode.Tiled;
            backdrop.size = new Vector2(16f, 20f);
            backdrop.color = new Color(1f, 1f, 1f, 0.14f);
            backdrop.sortingOrder = -20;

            var boardObject = new GameObject("Board");
            boardObject.transform.position = new Vector3(0f, -7f, 0f);
            var board = boardObject.AddComponent<MergeBoard>();
            var items = new GameObject("Items").transform;
            items.SetParent(boardObject.transform, false);
            EditorRefs.Set(board,
                ("itemPrefab", itemPrefab), ("itemRoot", items), ("blockSprite", square),
                ("wallColor", theme.secondary), ("backgroundColor", new Color(1f, 1f, 1f, 0.55f)),
                ("dangerLineColor", new Color(theme.danger.r, theme.danger.g, theme.danger.b, 0.85f)));

            var dropObject = new GameObject("DropController");
            var drop = dropObject.AddComponent<DropController>();
            var preview = new GameObject("Preview").AddComponent<SpriteRenderer>();
            preview.transform.SetParent(dropObject.transform, false);
            preview.sortingOrder = 3;
            preview.enabled = false;
            EditorRefs.Set(drop, ("worldCamera", camera), ("preview", preview));

            var effects = BuildMergeEffects();

            var hud = BuildHud(factory, camera, effects);
            UiFactory.EventSystem();

            var entry = new GameObject("GameplayEntryPoint").AddComponent<GameplayEntryPoint>();
            EditorRefs.Set(entry,
                ("gameplayConfig", config), ("board", board), ("dropController", drop), ("hud", hud),
                ("developmentConfig", AssetDatabase.LoadAssetAtPath<GameConfig>(FoundationSetup.GameConfigPath)));
        }

        private static MergeEffects BuildMergeEffects()
        {
            var effectsObject = new GameObject("MergeEffects");
            var effects = effectsObject.AddComponent<MergeEffects>();
            var material = AssetDatabase.LoadAssetAtPath<Material>(ContentPaths.SparkleMaterialPath);
            if (material == null)
            {
                // Reduced-effects fallback: no particle system, MergeEffects skips sparkles.
                return effects;
            }

            var particles = effectsObject.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.duration = 1f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4.5f);
            main.startSize = 0.25f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 0.5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 300;

            var emission = particles.emission;
            emission.enabled = false; // Emitted manually per merge.

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.25f;

            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            var color = particles.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;

            var renderer = effectsObject.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 6;

            EditorRefs.Set(effects, ("sparkles", particles));
            return effects;
        }

        private static GameplayHud BuildHud(UiFactory factory, Camera worldCamera, MergeEffects effects)
        {
            var theme = factory.Theme;
            var canvas = factory.Canvas("HUD", 10);
            var root = canvas.transform;
            var safe = factory.SafeArea(root);

            // Top bar: everything here is outside the play area and blocks drop input.
            var topBar = UiFactory.Rect("TopBar", safe);
            topBar.anchorMin = new Vector2(0f, 1f);
            topBar.anchorMax = new Vector2(1f, 1f);
            topBar.pivot = new Vector2(0.5f, 1f);
            topBar.sizeDelta = new Vector2(0f, 250f);
            topBar.anchoredPosition = Vector2.zero;

            var scoreBadge = factory.FramedPanel(topBar, "ScoreBadge", theme.primary, theme.accent).rectTransform;
            UiFactory.Place(scoreBadge, new Vector2(0f, 1f), new Vector2(40f, -24f), new Vector2(340f, 200f));
            var scoreCaption = factory.Text(scoreBadge, "Caption", "SCORE", 34, theme.accent);
            UiFactory.Place(scoreCaption.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(300f, 44f));
            var scoreText = factory.Text(scoreBadge, "ScoreText", "0", 92, theme.textLight, TextAnchor.MiddleCenter, outline: true);
            UiFactory.Place(scoreText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(320f, 110f));
            var scorePunch = UiFactory.Punch(scoreBadge.gameObject, 0.12f, 0.25f, false);

            var bestBadge = factory.FramedPanel(topBar, "BestBadge", theme.deep, theme.accent).rectTransform;
            UiFactory.Place(bestBadge, new Vector2(0f, 1f), new Vector2(400f, -24f), new Vector2(230f, 140f));
            var bestCaption = factory.Text(bestBadge, "Caption", "BEST", 30, theme.accent);
            UiFactory.Place(bestCaption.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(200f, 40f));
            var bestText = factory.Text(bestBadge, "BestText", "0", 54, theme.textLight);
            UiFactory.Place(bestText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(210f, 70f));

            var nextBadge = factory.FramedPanel(topBar, "NextBadge", theme.secondary, theme.accent).rectTransform;
            UiFactory.Place(nextBadge, new Vector2(1f, 1f), new Vector2(-210f, -24f), new Vector2(230f, 200f));
            var nextCaption = factory.Text(nextBadge, "Caption", "NEXT", 30, theme.textLight);
            UiFactory.Place(nextCaption.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(200f, 40f));
            var nextImage = factory.Image(nextBadge, "NextImage", theme.circle, Color.white);
            UiFactory.Place(nextImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -4f), new Vector2(92f, 92f));
            nextImage.preserveAspect = true;
            var nextPunch = UiFactory.Punch(nextImage.gameObject, 0.25f, 0.2f, false);
            var nextLabel = factory.Text(nextBadge, "NextLabel", string.Empty, 28, theme.textLight);
            UiFactory.Place(nextLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(220f, 36f));

            var pause = factory.Button(topBar, "PauseButton", string.Empty, theme.accent, new Vector2(150f, 150f));
            UiFactory.Place((RectTransform)pause.transform, new Vector2(1f, 1f), new Vector2(-40f, -24f), new Vector2(150f, 150f));
            UiFactory.Record(pause.transform);
            var pauseIcon = factory.Image(pause.transform, "Icon", theme.pauseIcon, theme.deep);
            UiFactory.Stretch(pauseIcon.rectTransform, 38f, 38f, 38f, 38f);

            // Hint, toast and Hammer banner share the left part of the band under the top bar; power-ups take the right.
            var instructions = factory.Image(safe, "Instructions", theme.roundedPanel, new Color(1f, 1f, 1f, 0.8f));
            UiFactory.Place(instructions.rectTransform, new Vector2(0.5f, 1f), new Vector2(-160f, -290f), new Vector2(660f, 96f));
            var instructionText = factory.Text(instructions.transform, "Text", "Drag to aim  •  Lift to drop", 36, theme.textDark);
            UiFactory.Stretch(instructionText.rectTransform);

            // Power-ups (Phase 5): presses on the bar never aim or drop.
            var powerBar = UiFactory.Place(UiFactory.Rect("PowerUpBar", safe), new Vector2(1f, 1f), new Vector2(-30f, -268f), new Vector2(345f, 130f));
            var hammer = BuildPowerUpButton(factory, powerBar, "HammerSlot", "HAMMER", theme.primary, 0f);
            var shuffle = BuildPowerUpButton(factory, powerBar, "ShuffleSlot", "SHUFFLE", theme.secondary, 185f);

            var banner = factory.Image(safe, "TargetingBanner", theme.roundedPanel, theme.primary);
            UiFactory.Place(banner.rectTransform, new Vector2(0.5f, 1f), new Vector2(-160f, -280f), new Vector2(660f, 116f));
            var bannerText = factory.Text(banner.transform, "Text", "Tap an item to smash it", 32, theme.textLight, TextAnchor.MiddleLeft);
            UiFactory.Place(bannerText.rectTransform, new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(440f, 80f));
            var cancelTargeting = factory.Button(banner.transform, "CancelButton", "CANCEL", theme.deep, new Vector2(176f, 88f), 34);
            UiFactory.Place((RectTransform)cancelTargeting.transform, new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(176f, 88f));
            UiFactory.Record(cancelTargeting.transform);
            banner.gameObject.SetActive(false);

            // On short screens (tablets) the top bar shrinks so it doesn't cover the board.
            EditorRefs.Set(safe.gameObject.AddComponent<ResponsiveScaler>(), ("targets", new Object[] { topBar, instructions.rectTransform, powerBar, banner.rectTransform }));

            // Toasts for discoveries / achievements / daily challenge / power-ups: below the top bar, never take input.
            var toastImage = factory.Image(safe, "Toast", theme.roundedPanel, theme.accent);
            UiFactory.Place(toastImage.rectTransform, new Vector2(0.5f, 1f), new Vector2(-160f, -420f), new Vector2(660f, 96f));
            var toastText = factory.Text(toastImage.transform, "Text", string.Empty, 34, theme.textDark);
            UiFactory.Stretch(toastText.rectTransform, 20f, 20f);
            toastImage.gameObject.AddComponent<CanvasGroup>();
            var toast = toastImage.gameObject.AddComponent<ToastPresenter>();
            EditorRefs.Set(toast, ("label", toastText));

            // Popups sit under the modal panels so a game-over card is never covered.
            var popupLayer = UiFactory.Stretch(UiFactory.Rect("ScorePopups", root)).gameObject.AddComponent<ScorePopupLayer>();
            var popupTemplate = factory.Text(popupLayer.transform, "PopupTemplate", "+0", 68, theme.textLight, TextAnchor.MiddleCenter, outline: true);
            UiFactory.Place(popupTemplate.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 90f));
            EditorRefs.Set(popupLayer, ("popupTemplate", popupTemplate), ("worldCamera", worldCamera));

            var (pausePanel, pauseCard) = factory.Modal(root, "PausePanel", new Vector2(820f, 720f), theme.deep);
            var pauseTitle = factory.Text(pauseCard, "Title", "Paused", 100, theme.accent, TextAnchor.MiddleCenter, outline: true);
            UiFactory.Place(pauseTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(700f, 130f));
            var resume = factory.Button(pauseCard, "ResumeButton", "RESUME", theme.primary, new Vector2(600f, 170f));
            UiFactory.Place((RectTransform)resume.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(600f, 170f));
            UiFactory.Record(resume.transform);
            var pauseMenu = factory.Button(pauseCard, "MainMenuButton", "MAIN MENU", theme.secondary, new Vector2(600f, 150f), 54);
            UiFactory.Place((RectTransform)pauseMenu.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -200f), new Vector2(600f, 150f));
            UiFactory.Record(pauseMenu.transform);

            var (gameOverPanel, gameOverCard) = factory.Modal(root, "GameOverPanel", new Vector2(880f, 1300f), theme.deep);
            var gameOverTitle = factory.Text(gameOverCard, "Title", "Game Over", 100, theme.accent, TextAnchor.MiddleCenter, outline: true);
            UiFactory.Place(gameOverTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(800f, 130f));
            var titlePunch = UiFactory.Punch(gameOverTitle.gameObject, 0.2f, 0.45f, true);
            var finalCaption = factory.Text(gameOverCard, "ScoreCaption", "SCORE", 40, theme.textLight);
            UiFactory.Place(finalCaption.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -210f), new Vector2(600f, 50f));
            var finalScore = factory.Text(gameOverCard, "FinalScore", "0", 150, theme.textLight, TextAnchor.MiddleCenter, outline: true);
            UiFactory.Place(finalScore.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -260f), new Vector2(800f, 170f));
            var newBest = factory.FramedPanel(gameOverCard, "NewBest", theme.accent, theme.primary).rectTransform;
            UiFactory.Place(newBest, new Vector2(0.5f, 1f), new Vector2(0f, -450f), new Vector2(440f, 96f));
            var newBestText = factory.Text(newBest, "Text", "NEW BEST!", 48, theme.textDark);
            UiFactory.Stretch(newBestText.rectTransform);
            var gameOverBest = factory.Text(gameOverCard, "BestText", "Best  0", 44, theme.textLight);
            UiFactory.Place(gameOverBest.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -570f), new Vector2(700f, 60f));
            // Revive offer (rewarded ad). Hidden unless the build has an ad provider; Restart never depends on it.
            var reviveStatus = factory.Text(gameOverCard, "ReviveStatus", string.Empty, 30, theme.textLight);
            reviveStatus.horizontalOverflow = HorizontalWrapMode.Wrap;
            reviveStatus.fontStyle = FontStyle.Normal;
            UiFactory.Place(reviveStatus.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 600f), new Vector2(800f, 56f));
            var continueButton = factory.Button(gameOverCard, "ContinueButton", "CONTINUE (AD)", theme.accent, new Vector2(620f, 150f), 52);
            UiFactory.Place((RectTransform)continueButton.transform, new Vector2(0.5f, 0f), new Vector2(0f, 440f), new Vector2(620f, 150f));
            UiFactory.Record(continueButton.transform);

            var restart = factory.Button(gameOverCard, "RestartButton", "RESTART", theme.primary, new Vector2(620f, 170f));
            UiFactory.Place((RectTransform)restart.transform, new Vector2(0.5f, 0f), new Vector2(0f, 240f), new Vector2(620f, 170f));
            UiFactory.Record(restart.transform);
            var gameOverMenu = factory.Button(gameOverCard, "MainMenuButton", "MAIN MENU", theme.secondary, new Vector2(620f, 150f), 54);
            UiFactory.Place((RectTransform)gameOverMenu.transform, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(620f, 150f));
            UiFactory.Record(gameOverMenu.transform);

            var errorText = factory.Text(safe, "ErrorText", string.Empty, 44, theme.danger);
            UiFactory.Place(errorText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 100f), new Vector2(960f, 200f));

            var hud = canvas.gameObject.AddComponent<GameplayHud>();
            EditorRefs.Set(hud,
                ("scoreText", scoreText), ("scorePunch", scorePunch), ("bestText", bestText),
                ("nextImage", nextImage), ("nextLabel", nextLabel), ("nextPunch", nextPunch),
                ("pauseButton", pause), ("instructions", instructions.gameObject),
                ("pausePanel", pausePanel), ("resumeButton", resume), ("pauseMenuButton", pauseMenu),
                ("gameOverPanel", gameOverPanel), ("gameOverScoreText", finalScore), ("gameOverBestText", gameOverBest),
                ("newBestBadge", newBest.gameObject), ("gameOverTitlePunch", titlePunch),
                ("restartButton", restart), ("gameOverMenuButton", gameOverMenu),
                ("popupLayer", popupLayer), ("mergeEffects", effects), ("toast", toast),
                ("continueButton", continueButton), ("reviveStatusText", reviveStatus),
                ("powerUpButtons", new Object[] { hammer, shuffle }), ("targetingBanner", banner.gameObject),
                ("targetingText", bannerText), ("targetingCancelButton", cancelTargeting),
                ("pointerBlockers", new Object[] { topBar, powerBar, banner.rectTransform }), ("errorText", errorText));
            return hud;
        }

        /// <summary>A power-up control: themed button, a "uses left" badge and a highlight shown while selecting a target.</summary>
        private static PowerUpButton BuildPowerUpButton(UiFactory factory, Transform parent, string name, string label, Color color, float x)
        {
            var theme = factory.Theme;
            var slot = UiFactory.Place(UiFactory.Rect(name, parent), new Vector2(0f, 1f), new Vector2(x, 0f), new Vector2(160f, 130f));
            var highlight = factory.Image(slot, "Selected", theme.roundedPanel, theme.accent);
            UiFactory.Stretch(highlight.rectTransform, -10f, -10f, -10f, -10f);
            highlight.gameObject.SetActive(false);

            var button = factory.Button(slot, "Button", label, color, new Vector2(160f, 130f), 30);
            UiFactory.Stretch((RectTransform)button.transform);
            UiFactory.Record(button.transform);
            var title = button.GetComponentInChildren<Text>(true);

            var badge = factory.Image(slot, "CountBadge", theme.circle, theme.deep);
            UiFactory.Place(badge.rectTransform, new Vector2(1f, 1f), new Vector2(16f, 16f), new Vector2(60f, 60f));
            var count = factory.Text(badge.transform, "Count", "0", 34, theme.textLight);
            UiFactory.Stretch(count.rectTransform);

            var component = slot.gameObject.AddComponent<PowerUpButton>();
            EditorRefs.Set(component, ("button", button), ("titleText", title), ("countText", count), ("selectedHighlight", highlight.gameObject));
            return component;
        }
    }
}
