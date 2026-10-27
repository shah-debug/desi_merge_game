using DesiMerge.Configuration;
using DesiMerge.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.Editor.Content
{
    /// <summary>
    /// MainMenu.unity: truck-art background, title plate, today's challenge card, Play, Collection, Awards and
    /// Settings, best score and a save-recovery notice. No buttons for features that don't exist (e.g. a shop).
    /// </summary>
    internal static class MainMenuSceneBuilder
    {
        public static void Populate()
        {
            var factory = new UiFactory(ContentSetup.LoadTheme());
            var theme = factory.Theme;

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = theme.background;

            var canvas = factory.Canvas("MenuCanvas");
            var root = canvas.transform;
            BuildBackground(factory, root);

            var safe = factory.SafeArea(root);

            // Everything is laid out in a 1080 x 1920 frame centred in the safe area; the scaler shrinks the frame
            // uniformly on shorter screens (tablets) so nothing is cut off.
            var content = UiFactory.Place(UiFactory.Rect("Content", safe), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1080f, 1920f));
            EditorRefs.Set(safe.gameObject.AddComponent<ResponsiveScaler>(), ("targets", new Object[] { content }));

            // Title plate with flower motifs at its corners.
            var plate = factory.FramedPanel(content, "TitlePlate", theme.deep, theme.accent).rectTransform;
            UiFactory.Place(plate, new Vector2(0.5f, 0.5f), new Vector2(0f, 620f), new Vector2(900f, 310f));
            var title = factory.Text(plate, "Title", "<color=#FFB300>Desi</color> Merge", theme.titleSize, theme.textLight, TextAnchor.MiddleCenter, outline: true);
            UiFactory.Stretch(title.rectTransform, 20f, 20f, 24f, 84f);
            var tagline = factory.Text(plate, "Tagline", "Milao  •  Barhao  •  Jeeto", theme.bodySize + 4, theme.accent);
            UiFactory.Place(tagline.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(820f, 60f));
            AddFlower(factory, plate, new Vector2(0f, 1f), new Vector2(-30f, 30f), theme.primary);
            AddFlower(factory, plate, new Vector2(1f, 1f), new Vector2(30f, 30f), theme.secondary);
            AddFlower(factory, plate, new Vector2(0f, 0f), new Vector2(-30f, -30f), theme.secondary);
            AddFlower(factory, plate, new Vector2(1f, 0f), new Vector2(30f, -30f), theme.primary);
            UiFactory.Entrance(plate.gameObject, 0f, new Vector2(0f, 220f));

            var daily = BuildDailyChallengeCard(factory, content);
            UiFactory.Entrance(daily.gameObject, 0.12f, new Vector2(0f, -60f));

            var play = factory.Button(content, "PlayButton", "PLAY", theme.primary, new Vector2(640f, 200f), 100);
            UiFactory.Place((RectTransform)play.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(640f, 200f));
            UiFactory.Record(play.transform);
            UiFactory.Entrance(play.gameObject, 0.22f, new Vector2(0f, -80f));

            var collection = factory.Button(content, "CollectionButton", "COLLECTION", theme.deep, new Vector2(440f, 140f), 46);
            UiFactory.Place((RectTransform)collection.transform, new Vector2(0.5f, 0.5f), new Vector2(-235f, -270f), new Vector2(440f, 140f));
            UiFactory.Record(collection.transform);
            UiFactory.Entrance(collection.gameObject, 0.3f, new Vector2(0f, -80f));

            var achievements = factory.Button(content, "AchievementsButton", "AWARDS", theme.deep, new Vector2(440f, 140f), 46);
            UiFactory.Place((RectTransform)achievements.transform, new Vector2(0.5f, 0.5f), new Vector2(235f, -270f), new Vector2(440f, 140f));
            UiFactory.Record(achievements.transform);
            UiFactory.Entrance(achievements.gameObject, 0.34f, new Vector2(0f, -80f));

            var settings = factory.Button(content, "SettingsButton", "SETTINGS", theme.secondary, new Vector2(440f, 130f), 46);
            UiFactory.Place((RectTransform)settings.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -445f), new Vector2(440f, 130f));
            UiFactory.Record(settings.transform);
            UiFactory.Entrance(settings.gameObject, 0.4f, new Vector2(0f, -80f));

            var best = factory.Text(content, "BestScore", string.Empty, theme.bodySize + 8, theme.textDark);
            UiFactory.Place(best.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -575f), new Vector2(800f, 70f));
            UiFactory.Entrance(best.gameObject, 0.46f, new Vector2(0f, -40f));

            var notice = factory.Text(content, "SaveNotice", string.Empty, 32, theme.danger);
            notice.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.Place(notice.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -680f), new Vector2(940f, 120f));
            notice.gameObject.SetActive(false);

            var settingsPanel = InstantiatePanel(ContentPaths.SettingsPanelPrefabPath, root);
            var collectionPanel = InstantiatePanel(ContentPaths.CollectionPanelPrefabPath, root);
            var achievementsPanel = InstantiatePanel(ContentPaths.AchievementsPanelPrefabPath, root);

            UiFactory.EventSystem();

            var entry = new GameObject("MainMenuEntryPoint").AddComponent<MainMenuEntryPoint>();
            EditorRefs.Set(entry,
                ("playButton", play), ("settingsButton", settings), ("collectionButton", collection),
                ("achievementsButton", achievements), ("bestScoreText", best), ("saveNoticeText", notice),
                ("settingsPanel", settingsPanel.GetComponent<SettingsPanel>()),
                ("collectionPanel", collectionPanel.GetComponent<CollectionPanel>()),
                ("achievementsPanel", achievementsPanel.GetComponent<AchievementsPanel>()),
                ("dailyChallengeCard", daily),
                ("developmentConfig", AssetDatabase.LoadAssetAtPath<GameConfig>(FoundationSetup.GameConfigPath)));
        }

        /// <summary>Cream base, a faint full-screen pattern, and patterned bands top and bottom (outside the safe area).</summary>
        internal static void BuildBackground(UiFactory factory, Transform root)
        {
            var theme = factory.Theme;
            UiFactory.Stretch(factory.Image(root, "Background", null, theme.background).rectTransform);
            var pattern = factory.Image(root, "Pattern", theme.pattern, new Color(1f, 1f, 1f, 0.18f));
            pattern.type = Image.Type.Tiled;
            UiFactory.Stretch(pattern.rectTransform);

            foreach (var (bandName, anchor) in new[] { ("TopBand", 1f), ("BottomBand", 0f) })
            {
                var band = factory.Image(root, bandName, null, theme.deep).rectTransform;
                band.anchorMin = new Vector2(0f, anchor);
                band.anchorMax = new Vector2(1f, anchor);
                band.pivot = new Vector2(0.5f, anchor);
                band.sizeDelta = new Vector2(0f, 120f);
                band.anchoredPosition = Vector2.zero;
                var bandPattern = factory.Image(band, "Pattern", theme.pattern, Color.white);
                bandPattern.type = Image.Type.Tiled;
                UiFactory.Stretch(bandPattern.rectTransform);
                var trim = factory.Image(band, "Trim", null, theme.accent).rectTransform;
                trim.anchorMin = new Vector2(0f, 1f - anchor);
                trim.anchorMax = new Vector2(1f, 1f - anchor);
                trim.pivot = new Vector2(0.5f, 1f - anchor);
                trim.sizeDelta = new Vector2(0f, 10f);
                trim.anchoredPosition = Vector2.zero;
            }
        }

        private static void AddFlower(UiFactory factory, Transform parent, Vector2 anchor, Vector2 offset, Color color)
        {
            var flower = factory.Image(parent, "Flower", factory.Theme.flower, color);
            UiFactory.Place(flower.rectTransform, anchor, offset, new Vector2(110f, 110f));
        }

        private static GameObject InstantiatePanel(string prefabPath, Transform parent)
        {
            var panel = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), parent);
            UiFactory.Stretch((RectTransform)panel.transform);
            UiFactory.Record(panel.transform);
            return panel;
        }

        /// <summary>Today's challenge: objective, progress bar, status and streak. Data comes from the daily service at runtime.</summary>
        private static DailyChallengeCard BuildDailyChallengeCard(UiFactory factory, Transform parent)
        {
            var theme = factory.Theme;
            var card = factory.FramedPanel(parent, "DailyChallengeCard", theme.secondary, theme.accent).rectTransform;
            UiFactory.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(920f, 340f));

            var caption = factory.Text(card, "Caption", "TODAY'S CHALLENGE", 32, theme.accent);
            UiFactory.Place(caption.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(700f, 44f));
            var title = factory.Text(card, "Title", string.Empty, 48, theme.textLight, TextAnchor.MiddleCenter, outline: true);
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(820f, 60f));
            var description = factory.Text(card, "Description", string.Empty, 34, theme.textLight);
            description.fontStyle = FontStyle.Normal;
            UiFactory.Place(description.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -132f), new Vector2(820f, 46f));

            var bar = factory.Image(card, "ProgressBar", theme.roundedPanel, new Color(0f, 0f, 0f, 0.3f));
            UiFactory.Place(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(-60f, -196f), new Vector2(620f, 40f));
            var fill = factory.Image(bar.transform, "Fill", theme.roundedPanel, theme.accent);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            UiFactory.Stretch(fill.rectTransform);
            var progress = factory.Text(card, "Progress", string.Empty, 34, theme.textLight, TextAnchor.MiddleLeft);
            UiFactory.Place(progress.rectTransform, new Vector2(0.5f, 1f), new Vector2(330f, -196f), new Vector2(200f, 46f));

            var status = factory.Text(card, "Status", string.Empty, 32, theme.accent, TextAnchor.MiddleLeft);
            UiFactory.Place(status.rectTransform, new Vector2(0f, 0f), new Vector2(50f, 34f), new Vector2(400f, 46f));
            var streak = factory.Text(card, "Streak", string.Empty, 28, theme.textLight, TextAnchor.MiddleRight);
            UiFactory.Place(streak.rectTransform, new Vector2(1f, 0f), new Vector2(-50f, 34f), new Vector2(440f, 46f));

            var mark = factory.Image(card, "CompletedMark", theme.flower, theme.accent);
            UiFactory.Place(mark.rectTransform, new Vector2(1f, 1f), new Vector2(20f, 20f), new Vector2(90f, 90f));
            mark.gameObject.SetActive(false);

            var claim = factory.Button(card, "ClaimButton", "CLAIM", theme.primary, new Vector2(260f, 90f), 38);
            UiFactory.Place((RectTransform)claim.transform, new Vector2(1f, 0f), new Vector2(-30f, 20f), new Vector2(260f, 90f));
            UiFactory.Record(claim.transform);
            claim.gameObject.SetActive(false);

            var component = card.gameObject.AddComponent<DailyChallengeCard>();
            EditorRefs.Set(component,
                ("titleText", title), ("descriptionText", description), ("progressText", progress), ("progressFill", fill),
                ("statusText", status), ("streakText", streak), ("completedMark", mark.gameObject), ("claimButton", claim));
            return component;
        }
    }
}
