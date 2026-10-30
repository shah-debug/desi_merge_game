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
    /// Gameplay.unity: the Phase 2 board, drop controller and entry point (unchanged behaviour) with the Phase 6.5
    /// visual design: illustrated playfield art, top bar, icon power-ups, pause and game-over cards, merge effects.
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

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = theme.background;
            cameraObject.AddComponent<CameraFit>();

            var backdrop = new GameObject("Backdrop").AddComponent<SpriteRenderer>();
            backdrop.sprite = theme.backgroundGradient;
            backdrop.sortingOrder = -30;
            if (theme.backgroundGradient != null)
            {
                var size = theme.backgroundGradient.bounds.size;
                backdrop.transform.localScale = new Vector3(16f / size.x, 30f / size.y, 1f);
            }

            AddWorldOrnament("OrnamentLowerLeft", theme, new Vector2(-4.7f, -6.2f), 4.2f, 0.16f);
            AddWorldOrnament("OrnamentLowerRight", theme, new Vector2(4.7f, -1.4f), 3.2f, 0.12f);
            AddWorldOrnament("OrnamentUpperLeft", theme, new Vector2(-4.9f, 3.4f), 3.0f, 0.1f);

            var boardObject = new GameObject("Board");
            boardObject.transform.position = new Vector3(0f, -7f, 0f);
            var board = boardObject.AddComponent<MergeBoard>();
            var items = new GameObject("Items").transform;
            items.SetParent(boardObject.transform, false);
            EditorRefs.Set(board,
                ("itemPrefab", itemPrefab), ("itemRoot", items), ("blockSprite", square),
                ("wallSprite", GameArtGenerator.Load(GameArtGenerator.WallTilePath)), ("fieldSprite", theme.panel),
                ("dangerLineSprite", GameArtGenerator.Load(GameArtGenerator.DangerDashPath)), ("capSprite", theme.ornament),
                ("wallColor", Color.white), ("backgroundColor", new Color(1f, 1f, 1f, 0.6f)),
                ("dangerLineColor", new Color(theme.danger.r, theme.danger.g, theme.danger.b, 0.9f)), ("dangerLineThickness", 0.12f));

            var dropObject = new GameObject("DropController");
            var drop = dropObject.AddComponent<DropController>();
            var preview = new GameObject("Preview").AddComponent<SpriteRenderer>();
            preview.transform.SetParent(dropObject.transform, false);
            preview.sortingOrder = 8;
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

        private static void AddWorldOrnament(string name, UITheme theme, Vector2 position, float size, float alpha)
        {
            if (theme.ornament == null)
            {
                return;
            }

            var ornament = new GameObject(name).AddComponent<SpriteRenderer>();
            ornament.sprite = theme.ornament;
            ornament.color = new Color(1f, 1f, 1f, alpha);
            ornament.sortingOrder = -25;
            ornament.transform.position = position;
            ornament.transform.localScale = Vector3.one * (size / theme.ornament.bounds.size.x);
        }

        private static MergeEffects BuildMergeEffects()
        {
            var effectsObject = new GameObject("MergeEffects");
            var effects = effectsObject.AddComponent<MergeEffects>();
            var material = AssetDatabase.LoadAssetAtPath<Material>(ContentPaths.SparkleMaterialPath);
            if (material == null)
            {
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
            emission.enabled = false;

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.25f;

            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            var color = particles.colorOverLifetime;
            color.enabled = true;
            color.color = Fade(1f, 0.6f);

            var renderer = effectsObject.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 10;

            ParticleSystem ring = null;
            var ringMaterial = AssetDatabase.LoadAssetAtPath<Material>(ContentPaths.RingMaterialPath);
            if (ringMaterial != null)
            {
                var ringObject = new GameObject("Ring");
                ringObject.transform.SetParent(effectsObject.transform, false);
                ring = ringObject.AddComponent<ParticleSystem>();
                ring.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var ringMain = ring.main;
                ringMain.duration = 1f;
                ringMain.loop = false;
                ringMain.playOnAwake = false;
                ringMain.startLifetime = 0.32f;
                ringMain.startSpeed = 0f;
                ringMain.simulationSpace = ParticleSystemSimulationSpace.World;
                ringMain.maxParticles = 24;
                var ringEmission = ring.emission;
                ringEmission.enabled = false;
                var ringShape = ring.shape;
                ringShape.enabled = false;
                var ringSize = ring.sizeOverLifetime;
                ringSize.enabled = true;
                ringSize.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.45f, 0f, 2.5f), new Keyframe(1f, 1f, 0f, 0f)));
                var ringColor = ring.colorOverLifetime;
                ringColor.enabled = true;
                ringColor.color = Fade(0.9f, 0.3f);
                var ringRenderer = ringObject.GetComponent<ParticleSystemRenderer>();
                ringRenderer.sharedMaterial = ringMaterial;
                ringRenderer.renderMode = ParticleSystemRenderMode.Billboard;
                ringRenderer.sortingOrder = 9;
            }

            EditorRefs.Set(effects, ("sparkles", particles), ("ring", ring));
            return effects;
        }

        private static Gradient Fade(float startAlpha, float holdUntil)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(startAlpha, 0f), new GradientAlphaKey(startAlpha, holdUntil), new GradientAlphaKey(0f, 1f) });
            return gradient;
        }

        private static GameplayHud BuildHud(UiFactory factory, Camera worldCamera, MergeEffects effects)
        {
            var theme = factory.Theme;
            var canvas = factory.Canvas("HUD", 10);
            var root = canvas.transform;
            var safe = factory.SafeArea(root);

            var topHud = UiFactory.Rect("TopHud", safe);
            topHud.anchorMin = new Vector2(0f, 1f);
            topHud.anchorMax = new Vector2(1f, 1f);
            topHud.pivot = new Vector2(0.5f, 1f);
            topHud.sizeDelta = new Vector2(0f, 480f);
            topHud.anchoredPosition = Vector2.zero;

            var topBar = UiFactory.Rect("TopBar", topHud);
            topBar.anchorMin = new Vector2(0f, 1f);
            topBar.anchorMax = new Vector2(1f, 1f);
            topBar.pivot = new Vector2(0.5f, 1f);
            topBar.sizeDelta = new Vector2(0f, 236f);
            topBar.anchoredPosition = Vector2.zero;

            var bar = factory.Panel(topBar, "Bar", theme.deep, theme.accent, 6f).rectTransform;
            UiFactory.Stretch(bar, 24f, 24f, 18f, 26f);
            var strip = factory.Strip(topBar, "Strip", 28f).rectTransform;
            strip.anchorMin = new Vector2(0f, 0f);
            strip.anchorMax = new Vector2(1f, 0f);
            strip.pivot = new Vector2(0.5f, 0.5f);
            strip.offsetMin = new Vector2(130f, 12f);
            strip.offsetMax = new Vector2(-130f, 40f);

            var scoreCaption = factory.Text(bar, "ScoreCaption", "SCORE", 34, theme.accent, TextAnchor.MiddleLeft);
            UiFactory.Place(scoreCaption.rectTransform, new Vector2(0f, 1f), new Vector2(44f, -22f), new Vector2(300f, 40f));
            var scoreText = factory.Text(bar, "ScoreText", "0", 96, theme.textLight, TextAnchor.MiddleLeft);
            AddShadow(scoreText.gameObject, theme, 0.45f, 5f);
            UiFactory.Place(scoreText.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -60f), new Vector2(360f, 112f));
            var scorePunch = UiFactory.Punch(scoreText.gameObject, 0.12f, theme.punchDuration, false);

            var crown = factory.Image(bar, "BestIcon", GameArtGenerator.Icon("Crown"), theme.accent);
            UiFactory.Place(crown.rectTransform, new Vector2(0f, 1f), new Vector2(410f, -22f), new Vector2(46f, 46f));
            var bestCaption = factory.Text(bar, "BestCaption", "BEST", 32, theme.accent, TextAnchor.MiddleLeft);
            UiFactory.Place(bestCaption.rectTransform, new Vector2(0f, 1f), new Vector2(466f, -24f), new Vector2(160f, 42f));
            var bestText = factory.Text(bar, "BestText", "0", 60, theme.textLight, TextAnchor.MiddleLeft);
            UiFactory.Place(bestText.rectTransform, new Vector2(0f, 1f), new Vector2(410f, -76f), new Vector2(250f, 80f));

            var well = UiFactory.Place(UiFactory.Rect("NextWell", bar), new Vector2(1f, 0.5f), new Vector2(-198f, 8f), new Vector2(160f, 160f));
            var glow = factory.Image(well, "Glow", theme.glow, new Color(theme.accent.r, theme.accent.g, theme.accent.b, 0.45f));
            UiFactory.Stretch(glow.rectTransform, -26f, -26f, -26f, -26f);
            var disc = factory.Image(well, "Disc", theme.disc, theme.card);
            UiFactory.Stretch(disc.rectTransform, 6f, 6f, 6f, 6f);
            var nextImage = factory.Image(well, "NextImage", theme.circle, Color.white);
            UiFactory.Place(nextImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(112f, 112f));
            nextImage.preserveAspect = true;
            var nextPunch = UiFactory.Punch(nextImage.gameObject, 0.25f, 0.2f, false);
            var nextTag = factory.Image(well, "NextTag", theme.pill, theme.accent);
            UiFactory.Place(nextTag.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -14f), new Vector2(108f, 42f));
            var nextCaption = factory.Text(nextTag.transform, "Caption", "NEXT", 28, theme.deep);
            UiFactory.Stretch(nextCaption.rectTransform, 0f, 0f, 0f, 2f);

            var pause = factory.Button(bar, "PauseButton", string.Empty, theme.accent, new Vector2(124f, 124f), icon: theme.pauseIcon, iconColor: theme.deep);
            UiFactory.Place((RectTransform)pause.transform, new Vector2(1f, 0.5f), new Vector2(-30f, 4f), new Vector2(124f, 124f));
            UiFactory.Record(pause.transform);

            var instructions = factory.Image(topHud, "Instructions", theme.pill, new Color(1f, 1f, 1f, 0.94f));
            UiFactory.Place(instructions.rectTransform, new Vector2(0.5f, 1f), new Vector2(-170f, -262f), new Vector2(640f, 92f));
            AddShadow(instructions.gameObject, theme, 0.18f, 6f);
            var instructionText = factory.Text(instructions.transform, "Text", "Drag to aim  •  Lift to drop", 36, theme.deep, body: true);
            UiFactory.Stretch(instructionText.rectTransform);

            var powerBar = UiFactory.Place(UiFactory.Rect("PowerUpBar", topHud), new Vector2(1f, 1f), new Vector2(-24f, -250f), new Vector2(338f, 156f));
            var hammer = BuildPowerUpButton(factory, powerBar, "HammerSlot", theme.primary, 0f, "Hammer");
            var shuffle = BuildPowerUpButton(factory, powerBar, "ShuffleSlot", theme.secondary, 182f, "Shuffle");

            var banner = factory.Image(topHud, "TargetingBanner", theme.pill, theme.primary);
            UiFactory.Place(banner.rectTransform, new Vector2(0.5f, 1f), new Vector2(-170f, -256f), new Vector2(640f, 108f));
            AddShadow(banner.gameObject, theme, 0.25f, 6f);
            var bannerIcon = factory.Image(banner.transform, "Icon", GameArtGenerator.Icon("Hammer"), Color.white);
            UiFactory.Place(bannerIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(56f, 56f));
            var bannerText = factory.Text(banner.transform, "Text", "Tap an item to smash it", 32, theme.textLight, TextAnchor.MiddleLeft, body: true);
            UiFactory.Place(bannerText.rectTransform, new Vector2(0f, 0.5f), new Vector2(100f, 0f), new Vector2(340f, 80f));
            var cancelTargeting = factory.Button(banner.transform, "CancelButton", "CANCEL", theme.deep, new Vector2(176f, 84f), 32);
            UiFactory.Place((RectTransform)cancelTargeting.transform, new Vector2(1f, 0.5f), new Vector2(-12f, 2f), new Vector2(176f, 84f));
            UiFactory.Record(cancelTargeting.transform);
            banner.gameObject.SetActive(false);

            safe.gameObject.AddComponent<ResponsiveScaler>();
            var responsive = new System.Collections.Generic.List<Object> { topHud };

            var toastImage = factory.Image(topHud, "Toast", theme.pill, theme.deep);
            UiFactory.Place(toastImage.rectTransform, new Vector2(0.5f, 1f), new Vector2(-170f, -376f), new Vector2(640f, 88f));
            var toastText = factory.Text(toastImage.transform, "Text", string.Empty, 34, theme.accent);
            UiFactory.Stretch(toastText.rectTransform, 24f, 24f, 0f, 2f);
            var toastIcon = factory.Image(toastImage.transform, "Icon", null, Color.white);
            toastIcon.preserveAspect = true;
            UiFactory.Place(toastIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(10f, 4f), new Vector2(104f, 104f));
            toastIcon.gameObject.SetActive(false);
            toastImage.gameObject.AddComponent<CanvasGroup>();
            var toast = toastImage.gameObject.AddComponent<ToastPresenter>();
            EditorRefs.Set(toast, ("label", toastText), ("icon", toastIcon));

            var popupLayer = UiFactory.Stretch(UiFactory.Rect("ScorePopups", root)).gameObject.AddComponent<ScorePopupLayer>();
            var popupTemplate = factory.Text(popupLayer.transform, "PopupTemplate", "+0", 72, theme.textLight, TextAnchor.MiddleCenter, outline: true);
            UiFactory.Place(popupTemplate.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 90f));
            EditorRefs.Set(popupLayer, ("popupTemplate", popupTemplate), ("worldCamera", worldCamera));

            var (pausePanel, pauseCard) = factory.Modal(root, "PausePanel", new Vector2(840f, 880f), "PAUSED");
            var pauseActions = UiFactory.Stretch(UiFactory.Rect("Actions", pauseCard), 0f, 0f, 170f, 0f);
            var resume = factory.Button(pauseActions, "ResumeButton", "RESUME", theme.primary, new Vector2(600f, 160f), icon: GameArtGenerator.Icon("Play"));
            UiFactory.Place((RectTransform)resume.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(600f, 160f));
            UiFactory.Record(resume.transform);
            var pauseRestart = factory.Button(pauseActions, "RestartButton", "RESTART", theme.secondary, new Vector2(600f, 150f), 56, GameArtGenerator.Icon("Restart"));
            UiFactory.Place((RectTransform)pauseRestart.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(600f, 150f));
            UiFactory.Record(pauseRestart.transform);
            var pauseMenu = factory.Button(pauseActions, "MainMenuButton", "MAIN MENU", theme.deep, new Vector2(600f, 150f), 56, GameArtGenerator.Icon("Home"));
            UiFactory.Place((RectTransform)pauseMenu.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -185f), new Vector2(600f, 150f));
            UiFactory.Record(pauseMenu.transform);

            var confirm = UiFactory.Stretch(UiFactory.Rect("RestartConfirm", pauseCard), 0f, 0f, 170f, 0f);
            var confirmTitle = factory.Text(confirm, "Question", "Restart this game?", 64, theme.textDark);
            UiFactory.Place(confirmTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 230f), new Vector2(760f, 90f));
            var confirmBody = factory.Text(confirm, "Detail", "Your current score will be lost.", 36, theme.mutedText, body: true);
            UiFactory.Place(confirmBody.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(760f, 60f));
            var confirmRestart = factory.Button(confirm, "ConfirmRestartButton", "YES, RESTART", theme.primary, new Vector2(600f, 150f), 54, GameArtGenerator.Icon("Restart"));
            UiFactory.Place((RectTransform)confirmRestart.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(600f, 150f));
            UiFactory.Record(confirmRestart.transform);
            var cancelRestart = factory.Button(confirm, "CancelRestartButton", "KEEP PLAYING", theme.secondary, new Vector2(600f, 150f), 54, GameArtGenerator.Icon("Play"));
            UiFactory.Place((RectTransform)cancelRestart.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -190f), new Vector2(600f, 150f));
            UiFactory.Record(cancelRestart.transform);
            confirm.gameObject.SetActive(false);

            var (gameOverPanel, gameOverCard) = factory.Modal(root, "GameOverPanel", new Vector2(900f, 1580f), "GAME OVER");
            var gameOverTitle = gameOverCard.Find("Header/Title").gameObject;
            var titlePunch = UiFactory.Punch(gameOverTitle, 0.2f, 0.45f, true);
            var finalCaption = factory.Text(gameOverCard, "ScoreCaption", "YOUR SCORE", 40, theme.mutedText);
            UiFactory.Place(finalCaption.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -196f), new Vector2(600f, 50f));
            var finalScore = factory.Text(gameOverCard, "FinalScore", "0", 170, theme.primary);
            AddShadow(finalScore.gameObject, theme, 0.3f, 8f);
            UiFactory.Place(finalScore.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -236f), new Vector2(800f, 176f));
            var improvement = factory.Text(gameOverCard, "Improvement", string.Empty, 36, theme.secondary);
            UiFactory.Place(improvement.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -410f), new Vector2(800f, 46f));
            improvement.gameObject.SetActive(false);
            var newBest = factory.Image(gameOverCard, "NewBest", theme.pill, theme.accent).rectTransform;
            UiFactory.Place(newBest, new Vector2(0.5f, 1f), new Vector2(0f, -462f), new Vector2(420f, 88f));
            var newBestIcon = factory.Image(newBest, "Icon", GameArtGenerator.Icon("Star"), theme.deep);
            UiFactory.Place(newBestIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(34f, 0f), new Vector2(54f, 54f));
            var newBestText = factory.Text(newBest, "Text", "NEW BEST!", 48, theme.deep);
            UiFactory.Stretch(newBestText.rectTransform, 70f, 10f, 0f, 2f);
            var gameOverBest = factory.Text(gameOverCard, "BestText", "Best  0", 46, theme.textDark, TextAnchor.MiddleLeft);
            UiFactory.Place(gameOverBest.rectTransform, new Vector2(0.5f, 1f), new Vector2(110f, -560f), new Vector2(380f, 64f));
            var bestIcon = factory.Image(gameOverCard, "BestIcon", GameArtGenerator.Icon("Crown"), theme.accent);
            UiFactory.Place(bestIcon.rectTransform, new Vector2(0.5f, 1f), new Vector2(-114f, -566f), new Vector2(50f, 50f));

            var summaryPanel = factory.Panel(gameOverCard, "Summary", new Color(theme.deep.r, theme.deep.g, theme.deep.b, 0.06f)).rectTransform;
            UiFactory.Place(summaryPanel, new Vector2(0.5f, 1f), new Vector2(0f, -650f), new Vector2(780f, 180f));
            var summaryIcon = factory.Image(summaryPanel, "Icon", GameArtGenerator.Icon("Missions"), theme.secondary);
            UiFactory.Place(summaryIcon.rectTransform, new Vector2(0f, 1f), new Vector2(30f, -26f), new Vector2(54f, 54f));
            var summary = factory.Text(summaryPanel, "Text", string.Empty, 32, theme.textDark, TextAnchor.UpperLeft, body: true);
            summary.horizontalOverflow = HorizontalWrapMode.Wrap;
            summary.lineSpacing = 1.15f;
            UiFactory.Stretch(summary.rectTransform, 104f, 24f, 24f, 16f);

            var reviveStatus = factory.Text(gameOverCard, "ReviveStatus", string.Empty, 30, theme.mutedText, body: true);
            reviveStatus.horizontalOverflow = HorizontalWrapMode.Wrap;
            reviveStatus.fontStyle = FontStyle.Normal;
            UiFactory.Place(reviveStatus.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 606f), new Vector2(800f, 56f));
            var continueButton = factory.Button(gameOverCard, "ContinueButton", "CONTINUE", theme.accent, new Vector2(640f, 150f), 54, GameArtGenerator.Icon("Video"), theme.deep);
            SetLabelColor(continueButton, theme.deep);
            UiFactory.Place((RectTransform)continueButton.transform, new Vector2(0.5f, 0f), new Vector2(0f, 440f), new Vector2(640f, 150f));
            UiFactory.Record(continueButton.transform);

            var restart = factory.Button(gameOverCard, "RestartButton", "PLAY AGAIN", theme.primary, new Vector2(640f, 165f), 60, GameArtGenerator.Icon("Restart"));
            UiFactory.Place((RectTransform)restart.transform, new Vector2(0.5f, 0f), new Vector2(0f, 246f), new Vector2(640f, 165f));
            UiFactory.Record(restart.transform);
            var gameOverMenu = factory.Button(gameOverCard, "MainMenuButton", "MAIN MENU", theme.secondary, new Vector2(640f, 150f), 54, GameArtGenerator.Icon("Home"));
            UiFactory.Place((RectTransform)gameOverMenu.transform, new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(640f, 150f));
            UiFactory.Record(gameOverMenu.transform);

            var errorText = factory.Text(safe, "ErrorText", string.Empty, 44, theme.danger, body: true);
            UiFactory.Place(errorText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 100f), new Vector2(960f, 200f));

            var hud = canvas.gameObject.AddComponent<GameplayHud>();
            EditorRefs.Set(hud,
                ("scoreText", scoreText), ("scorePunch", scorePunch), ("bestText", bestText),
                ("nextImage", nextImage), ("nextLabel", null), ("nextPunch", nextPunch),
                ("pauseButton", pause), ("instructions", instructions.gameObject),
                ("pausePanel", pausePanel), ("resumeButton", resume), ("pauseMenuButton", pauseMenu),
                ("pauseRestartButton", pauseRestart), ("pauseActions", pauseActions.gameObject), ("restartConfirm", confirm.gameObject),
                ("confirmRestartButton", confirmRestart), ("cancelRestartButton", cancelRestart),
                ("gameOverPanel", gameOverPanel), ("gameOverScoreText", finalScore), ("gameOverBestText", gameOverBest),
                ("newBestBadge", newBest.gameObject), ("gameOverTitlePunch", titlePunch),
                ("restartButton", restart), ("gameOverMenuButton", gameOverMenu),
                ("popupLayer", popupLayer), ("mergeEffects", effects), ("toast", toast),
                ("continueButton", continueButton), ("reviveStatusText", reviveStatus),
                ("powerUpButtons", new Object[] { hammer, shuffle }), ("targetingBanner", banner.gameObject),
                ("targetingText", bannerText), ("targetingCancelButton", cancelTargeting),
                ("pointerBlockers", new Object[] { topBar, powerBar, banner.rectTransform }), ("errorText", errorText),
                ("summaryText", summary), ("improvementText", improvement),
                ("tutorial", TutorialBuilder.Build(factory, root)));
            responsive.Add(gameOverCard);
            responsive.Add(pausePanel.transform.Find("Card"));
            EditorRefs.Set(safe.GetComponent<ResponsiveScaler>(), ("targets", responsive.ToArray()));
            return hud;
        }

        private static PowerUpButton BuildPowerUpButton(UiFactory factory, Transform parent, string name, Color color, float x, string iconName)
        {
            var theme = factory.Theme;
            var size = new Vector2(156f, 150f);
            var slot = UiFactory.Place(UiFactory.Rect(name, parent), new Vector2(0f, 1f), new Vector2(x, 0f), size);
            var group = slot.gameObject.AddComponent<CanvasGroup>();
            var highlight = factory.Image(slot, "Selected", theme.glow, theme.accent);
            UiFactory.Stretch(highlight.rectTransform, -34f, -34f, -34f, -34f);
            var ring = factory.Image(highlight.transform, "Ring", theme.panel, theme.accent);
            UiFactory.Stretch(ring.rectTransform, 24f, 24f, 24f, 24f);
            highlight.gameObject.SetActive(false);

            var button = factory.Button(slot, "Button", string.Empty, color, size, 28, GameArtGenerator.Icon(iconName));
            UiFactory.Stretch((RectTransform)button.transform);
            var icon = button.transform.Find("Icon").GetComponent<Image>();
            UiFactory.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 24f), new Vector2(68f, 68f));
            var title = button.transform.Find("Label").GetComponent<Text>();
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 12f + theme.buttonLip), new Vector2(150f, 38f));
            title.fontSize = 28;
            UiFactory.Record(button.transform, icon.rectTransform, title, title.rectTransform);

            var badge = factory.Image(slot, "CountBadge", theme.disc, theme.deep);
            UiFactory.Place(badge.rectTransform, new Vector2(1f, 1f), new Vector2(16f, 16f), new Vector2(64f, 64f));
            var badgeFill = factory.Image(badge.transform, "Fill", theme.disc, theme.accent);
            UiFactory.Stretch(badgeFill.rectTransform, 5f, 5f, 5f, 5f);
            var count = factory.Text(badge.transform, "Count", "0", 34, theme.deep);
            UiFactory.Stretch(count.rectTransform, 0f, 0f, 0f, 2f);

            var component = slot.gameObject.AddComponent<PowerUpButton>();
            EditorRefs.Set(component, ("button", button), ("titleText", title), ("countText", count), ("selectedHighlight", highlight.gameObject), ("contentGroup", group));
            return component;
        }

        internal static void AddShadow(GameObject target, UITheme theme, float alpha, float distance)
        {
            var shadow = target.AddComponent<Shadow>();
            shadow.effectColor = new Color(theme.textDark.r, theme.textDark.g, theme.textDark.b, alpha);
            shadow.effectDistance = new Vector2(0f, -distance);
        }

        internal static void SetLabelColor(Button button, Color color)
        {
            var label = button.transform.Find("Label").GetComponent<Text>();
            label.color = color;
            var shadow = label.GetComponent<Shadow>();
            shadow.enabled = false;
            UiFactory.Record(label, shadow);
        }
    }
}
