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
            EditorRefs.Set(hud, ("powerUpEffects", BuildPowerUpEffects(camera, board, effects)));
            EditorRefs.Set(hud.FocusShade, ("board", board));
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

        /// <summary>Hammer swing, smash stand-in and camera zoom (visual only, no colliders).</summary>
        private static PowerUpEffects BuildPowerUpEffects(Camera camera, MergeBoard board, MergeEffects effects)
        {
            var root = new GameObject("PowerUpEffects");
            SpriteRenderer Renderer(string name, Sprite sprite, Color color, int order)
            {
                var renderer = new GameObject(name).AddComponent<SpriteRenderer>();
                renderer.transform.SetParent(root.transform, false);
                renderer.sprite = sprite;
                renderer.color = color;
                renderer.sortingOrder = order;
                renderer.enabled = false;
                return renderer;
            }

            var ghost = Renderer("SmashGhost", null, Color.white, 9);
            var hammer = Renderer("Hammer", GameArtGenerator.Load(GameArtGenerator.IconPath("HammerArt")) ?? GameArtGenerator.Icon("Hammer"), Color.white, 12);
            var fx = root.AddComponent<PowerUpEffects>();
            EditorRefs.Set(fx, ("worldCamera", camera), ("board", board), ("mergeEffects", effects), ("hammer", hammer), ("ghost", ghost));
            return fx;
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
            UiFactory.Place(scoreCaption.rectTransform, new Vector2(0f, 1f), new Vector2(176f, -22f), new Vector2(260f, 40f));
            var scoreText = factory.Text(bar, "ScoreText", "0", 96, theme.textLight, TextAnchor.MiddleLeft);
            AddShadow(scoreText.gameObject, theme, 0.45f, 5f);
            UiFactory.Place(scoreText.rectTransform, new Vector2(0f, 1f), new Vector2(172f, -60f), new Vector2(300f, 112f));
            var scorePunch = UiFactory.Punch(scoreText.gameObject, 0.12f, theme.punchDuration, false);

            var crown = factory.Image(bar, "BestIcon", GameArtGenerator.Icon("Crown"), theme.accent);
            UiFactory.Place(crown.rectTransform, new Vector2(0f, 1f), new Vector2(482f, -22f), new Vector2(46f, 46f));
            var bestCaption = factory.Text(bar, "BestCaption", "BEST", 32, theme.accent, TextAnchor.MiddleLeft);
            UiFactory.Place(bestCaption.rectTransform, new Vector2(0f, 1f), new Vector2(538f, -24f), new Vector2(140f, 42f));
            var bestText = factory.Text(bar, "BestText", "0", 60, theme.textLight, TextAnchor.MiddleLeft);
            UiFactory.Place(bestText.rectTransform, new Vector2(0f, 1f), new Vector2(482f, -76f), new Vector2(190f, 80f));

            var well = UiFactory.Place(UiFactory.Rect("NextWell", bar), new Vector2(1f, 0.5f), new Vector2(-40f, 8f), new Vector2(160f, 160f));
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

            var menuButton = factory.Button(bar, "MenuButton", string.Empty, theme.secondary, new Vector2(124f, 124f), icon: GameArtGenerator.Icon("Menu"), iconColor: Color.white);
            UiFactory.Place((RectTransform)menuButton.transform, new Vector2(0f, 0.5f), new Vector2(26f, 4f), new Vector2(124f, 124f));
            UiFactory.Record(menuButton.transform);

            var instructions = factory.Image(topHud, "Instructions", theme.pill, new Color(1f, 1f, 1f, 0.94f));
            UiFactory.Place(instructions.rectTransform, new Vector2(0.5f, 1f), new Vector2(150f, -372f), new Vector2(640f, 92f));
            AddShadow(instructions.gameObject, theme, 0.18f, 6f);
            var instructionText = factory.Text(instructions.transform, "Text", "Drag to aim  •  Lift to drop", 36, theme.deep, body: true);
            UiFactory.Stretch(instructionText.rectTransform);

            var diamondCounter = MainMenuSceneBuilder.DiamondPill(factory, topHud, new Vector2(0f, 1f), new Vector2(30f, -244f), new Vector2(236f, 72f));
            var amountRect = (RectTransform)diamondCounter.transform.Find("Amount");
            amountRect.offsetMax = new Vector2(-62f, amountRect.offsetMax.y);
            var plus = factory.Button(diamondCounter.transform, "GetDiamonds", "+", theme.secondary, new Vector2(56f, 56f), 44);
            UiFactory.Place((RectTransform)plus.transform, new Vector2(1f, 0.5f), new Vector2(-8f, 2f), new Vector2(56f, 56f));
            UiFactory.Record(plus.transform);
            // Skin tag hanging from the NEXT circle on a short string (like a price tag).
            var tagString = factory.Image(topHud, "SkinString", null, theme.deep);
            UiFactory.Place(tagString.rectTransform, new Vector2(1f, 1f), new Vector2(-141f, -206f), new Vector2(6f, 50f));
            var skinTag = factory.Button(topHud, "SkinTag", string.Empty, theme.accent, new Vector2(112f, 128f));
            UiFactory.Place((RectTransform)skinTag.transform, new Vector2(1f, 1f), new Vector2(-88f, -248f), new Vector2(112f, 128f));
            UiFactory.Record(skinTag.transform);
            var hole = factory.Image(skinTag.transform, "Hole", theme.disc, theme.deep);
            UiFactory.Place(hole.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(14f, 14f));
            var skinPreview = factory.Image(skinTag.transform, "Preview", null, Color.white);
            skinPreview.preserveAspect = true;
            UiFactory.Place(skinPreview.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(66f, 66f));
            var skinLabel = factory.Text(skinTag.transform, "TagLabel", "SKIN", 28, theme.deep);
            UiFactory.Place(skinLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(108f, 34f));
            var powerBar = UiFactory.Place(UiFactory.Rect("PowerUpBar", topHud), new Vector2(0f, 1f), new Vector2(30f, -332f), new Vector2(338f, 156f));
            var hammer = BuildPowerUpButton(factory, powerBar, "HammerSlot", theme.primary, 0f, "Hammer");
            var shuffle = BuildPowerUpButton(factory, powerBar, "ShuffleSlot", theme.secondary, 182f, "Shuffle");

            safe.gameObject.AddComponent<ResponsiveScaler>();
            var responsive = new System.Collections.Generic.List<Object> { topHud };

            var toastImage = factory.Image(topHud, "Toast", theme.pill, theme.deep);
            UiFactory.Place(toastImage.rectTransform, new Vector2(0.5f, 1f), new Vector2(150f, -366f), new Vector2(640f, 104f));
            var toastText = factory.Text(toastImage.transform, "Text", string.Empty, 34, theme.accent, body: true);
            toastText.horizontalOverflow = HorizontalWrapMode.Wrap;
            toastText.verticalOverflow = VerticalWrapMode.Truncate;
            toastText.resizeTextForBestFit = true;
            toastText.resizeTextMinSize = 20;
            toastText.resizeTextMaxSize = 34;
            toastText.lineSpacing = 0.95f;
            UiFactory.Stretch(toastText.rectTransform, 32f, 32f, 8f, 8f);
            var toastIcon = factory.Image(toastImage.transform, "Icon", null, Color.white);
            toastIcon.preserveAspect = true;
            UiFactory.Place(toastIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(10f, 4f), new Vector2(104f, 104f));
            toastIcon.gameObject.SetActive(false);
            toastImage.gameObject.AddComponent<CanvasGroup>();
            var toast = toastImage.gameObject.AddComponent<ToastPresenter>();
            EditorRefs.Set(toast, ("label", toastText), ("icon", toastIcon));

            var (focus, cancelTargeting) = BuildHammerFocus(factory, (RectTransform)root, worldCamera);

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
                ("pauseButton", null), ("instructions", instructions.gameObject),
                ("pausePanel", pausePanel), ("resumeButton", resume), ("pauseMenuButton", pauseMenu),
                ("pauseRestartButton", pauseRestart), ("pauseActions", pauseActions.gameObject), ("restartConfirm", confirm.gameObject),
                ("confirmRestartButton", confirmRestart), ("cancelRestartButton", cancelRestart),
                ("gameOverPanel", gameOverPanel), ("gameOverScoreText", finalScore), ("gameOverBestText", gameOverBest),
                ("newBestBadge", newBest.gameObject), ("gameOverTitlePunch", titlePunch),
                ("restartButton", restart), ("gameOverMenuButton", gameOverMenu),
                ("popupLayer", popupLayer), ("mergeEffects", effects), ("toast", toast),
                ("continueButton", continueButton), ("reviveStatusText", reviveStatus),
                ("powerUpButtons", new Object[] { hammer, shuffle }), ("targetingBanner", null), ("focusShade", focus),
                ("targetingText", null), ("targetingCancelButton", cancelTargeting),
                ("pointerBlockers", new Object[] { topBar, powerBar, (RectTransform)cancelTargeting.transform, (RectTransform)skinTag.transform, (RectTransform)diamondCounter.transform }), ("errorText", errorText),
                ("summaryText", summary), ("improvementText", improvement),
                ("menuButton", menuButton), ("diamondCounter", diamondCounter), ("skinTagButton", skinTag),
                ("skinTagPreview", skinPreview), ("getDiamondsButton", plus), ("gameMenu", NavigationScreensBuilder.BuildGameplayMenu(factory, root, safe, responsive)),
                ("tutorial", TutorialBuilder.Build(factory, root)));
            responsive.Add(gameOverCard);
            responsive.Add(pausePanel.transform.Find("Card"));
            EditorRefs.Set(safe.GetComponent<ResponsiveScaler>(), ("targets", responsive.ToArray()));
            return hud;
        }

        /// <summary>
        /// Hammer focus: dark bands that slide in above and below the jar (sized at runtime from the camera), with a
        /// round X to cancel. Covers the HUD so only the jar stays bright.
        /// </summary>
        private static (FocusShade Focus, Button Cancel) BuildHammerFocus(UiFactory factory, RectTransform root, Camera worldCamera)
        {
            var theme = factory.Theme;
            var area = UiFactory.Stretch(UiFactory.Rect("HammerFocus", root));
            var group = area.gameObject.AddComponent<CanvasGroup>();
            var shade = new Color(0.02f, 0.02f, 0.06f, 0.78f);
            RectTransform Band(string name, float anchorY)
            {
                var band = factory.Image(area, name, null, shade, raycast: true).rectTransform;
                band.anchorMin = new Vector2(0f, anchorY);
                band.anchorMax = new Vector2(1f, anchorY);
                band.pivot = new Vector2(0.5f, anchorY);
                band.sizeDelta = new Vector2(0f, 0f);
                band.anchoredPosition = Vector2.zero;
                return band;
            }

            var top = Band("TopBand", 1f);
            var bottom = Band("BottomBand", 0f);
            var cancel = factory.Button(top, "CancelButton", string.Empty, theme.danger, new Vector2(112f, 112f), icon: GameArtGenerator.Icon("Close"));
            UiFactory.Place((RectTransform)cancel.transform, new Vector2(1f, 0f), new Vector2(-36f, 22f), new Vector2(112f, 112f));
            UiFactory.Record(cancel.transform);
            var focus = area.gameObject.AddComponent<FocusShade>();
            EditorRefs.Set(focus, ("area", area), ("topBand", top), ("bottomBand", bottom), ("group", group), ("worldCamera", worldCamera),
                ("jarTop", 10.7f), ("jarBottom", -0.9f));
            area.gameObject.SetActive(false);
            return (focus, cancel);
        }

        /// <summary>
        /// One power-up: a glass tile with an animated 3D icon (no text). Free uses show as a count badge; when they
        /// run out the diamond price sits inside the tile, at the bottom.
        /// </summary>
        private static PowerUpButton BuildPowerUpButton(UiFactory factory, Transform parent, string name, Color color, float x, string iconName)
        {
            var theme = factory.Theme;
            var size = new Vector2(156f, 156f);
            var slot = UiFactory.Place(UiFactory.Rect(name, parent), new Vector2(0f, 1f), new Vector2(x, 0f), size);
            var group = slot.gameObject.AddComponent<CanvasGroup>();
            var highlight = factory.Image(slot, "Selected", theme.glow, theme.accent);
            UiFactory.Stretch(highlight.rectTransform, -34f, -34f, -34f, -34f);
            var ring = factory.Image(highlight.transform, "Ring", theme.panel, theme.accent);
            UiFactory.Stretch(ring.rectTransform, 24f, 24f, 24f, 24f);
            highlight.gameObject.SetActive(false);

            var button = factory.Button(slot, "Button", string.Empty, new Color(1f, 1f, 1f, 0.9f), size);
            UiFactory.Stretch((RectTransform)button.transform);
            button.transform.Find("Label").gameObject.SetActive(false);
            var rim = button.gameObject.AddComponent<Outline>();
            rim.effectColor = new Color(color.r, color.g, color.b, 0.75f);
            rim.effectDistance = new Vector2(4f, -4f);
            UiFactory.Record(button.transform, button.transform.Find("Label").gameObject);

            var hammer = iconName == "Hammer";
            var art = factory.Image(button.transform, "Art", GameArtGenerator.Load(GameArtGenerator.IconPath(hammer ? "HammerArt" : "ShuffleBoxArt")), Color.white);
            art.preserveAspect = true;
            var artSize = new Vector2(112f, 112f);
            var pivot = hammer ? new Vector2(0.32f, 0.18f) : new Vector2(0.5f, 0.12f);
            UiFactory.Place(art.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 22f), artSize);
            art.rectTransform.pivot = pivot;
            art.rectTransform.anchoredPosition = new Vector2(0f, 22f) + Vector2.Scale(pivot - new Vector2(0.5f, 0.5f), artSize);
            EditorRefs.Set(art.gameObject.AddComponent<IconBeat>(), ("style", hammer ? 0 : 1), ("interval", 2f), ("duration", hammer ? 0.6f : 0.7f));

            var badge = factory.Image(slot, "CountBadge", theme.disc, theme.deep);
            UiFactory.Place(badge.rectTransform, new Vector2(1f, 1f), new Vector2(16f, 16f), new Vector2(64f, 64f));
            var badgeFill = factory.Image(badge.transform, "Fill", theme.disc, theme.accent);
            UiFactory.Stretch(badgeFill.rectTransform, 5f, 5f, 5f, 5f);
            var count = factory.Text(badge.transform, "Count", "0", 34, theme.deep);
            UiFactory.Stretch(count.rectTransform, 0f, 0f, 0f, 2f);

            var price = factory.Image(button.transform, "PriceBadge", theme.pill, color);
            UiFactory.Place(price.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 8f + theme.buttonLip), new Vector2(128f, 44f));
            var gem = factory.Image(price.transform, "Gem", GameArtGenerator.Load(GameArtGenerator.IconPath("DiamondArt")), Color.white);
            gem.preserveAspect = true;
            UiFactory.Place(gem.rectTransform, new Vector2(0f, 0.5f), new Vector2(4f, 1f), new Vector2(42f, 42f));
            var priceText = factory.Text(price.transform, "Price", "0", 30, Color.white);
            UiFactory.Stretch(priceText.rectTransform, 44f, 8f, 0f, 2f);
            price.gameObject.SetActive(false);
            var component = slot.gameObject.AddComponent<PowerUpButton>();
            EditorRefs.Set(component, ("button", button), ("titleText", null), ("countText", count), ("selectedHighlight", highlight.gameObject), ("contentGroup", group),
                ("countBadge", badge.gameObject), ("priceBadge", price.gameObject), ("priceText", priceText));
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
