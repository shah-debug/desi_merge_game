using DesiMerge.Configuration;
using DesiMerge.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.Editor.Content
{
    /// <summary>
    /// MainMenu.unity: illustrated title, today's challenge card, Play, Collection, Awards and Settings, best score
    /// and a save-recovery notice. No buttons for features that don't exist (e.g. a shop).
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
            var content = UiFactory.Place(UiFactory.Rect("Content", safe), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1080f, 1920f));
            EditorRefs.Set(safe.gameObject.AddComponent<ResponsiveScaler>(), ("targets", new Object[] { content }));

            var progression = AssetDatabase.LoadAssetAtPath<MergeProgression>(ContentPaths.MergeProgressionPath);
            AddItemDecor(factory, content, progression, "aam", new Vector2(-392f, 846f), 160f, 14f);
            AddItemDecor(factory, content, progression, "chai", new Vector2(398f, 842f), 170f, -10f);
            AddItemDecor(factory, content, progression, "rickshaw", new Vector2(-370f, -790f), 190f, -8f);
            AddItemDecor(factory, content, progression, "mango_shake", new Vector2(378f, -800f), 170f, 9f);
            AddItemDecor(factory, content, progression, "tamatar", new Vector2(-150f, -830f), 96f, -14f);
            AddItemDecor(factory, content, progression, "nimbu", new Vector2(160f, -840f), 90f, 18f);

            var plate = factory.Panel(content, "TitlePlate", theme.deep, theme.accent, 8f).rectTransform;
            UiFactory.Place(plate, new Vector2(0.5f, 0.5f), new Vector2(0f, 600f), new Vector2(900f, 320f));
            var plateStrip = factory.Strip(plate, "Strip", 28f).rectTransform;
            plateStrip.anchorMin = new Vector2(0f, 0f);
            plateStrip.anchorMax = new Vector2(1f, 0f);
            plateStrip.pivot = new Vector2(0.5f, 0.5f);
            plateStrip.offsetMin = new Vector2(70f, -14f);
            plateStrip.offsetMax = new Vector2(-70f, 14f);
            var title = factory.Text(plate, "Title", "<color=#FFB627>Desi</color> Merge", theme.titleSize + 10, theme.textLight, TextAnchor.MiddleCenter, outline: true);
            GameplaySceneBuilder.AddShadow(title.gameObject, theme, 0.5f, 10f);
            UiFactory.Stretch(title.rectTransform, 20f, 20f, 18f, 92f);
            var tagline = factory.Text(plate, "Tagline", "Milao  •  Barhao  •  Jeeto", theme.bodySize + 4, theme.accent, body: true);
            UiFactory.Place(tagline.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(820f, 60f));
            factory.Ornament(plate, "OrnamentLeft", new Vector2(0f, 0.5f), new Vector2(-46f, 0f), 130f);
            factory.Ornament(plate, "OrnamentRight", new Vector2(1f, 0.5f), new Vector2(46f, 0f), 130f);
            UiFactory.Entrance(plate.gameObject, 0f, new Vector2(0f, 220f));

            var daily = BuildDailyChallengeCard(factory, content);
            UiFactory.Entrance(daily.gameObject, 0.12f, new Vector2(0f, -60f));

            var play = factory.Button(content, "PlayButton", "PLAY", theme.primary, new Vector2(660f, 210f), 112, GameArtGenerator.Icon("Play"));
            UiFactory.Place((RectTransform)play.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(660f, 210f));
            UiFactory.Record(play.transform);
            UiFactory.Entrance(play.gameObject, 0.22f, new Vector2(0f, -80f));

            var collection = IconTile(factory, content, "CollectionButton", "COLLECTION", "Grid", theme.secondary, Color.white, -300f, 0.3f);
            var achievements = IconTile(factory, content, "AchievementsButton", "AWARDS", "Trophy", theme.accent, theme.deep, 0f, 0.34f);
            var settings = IconTile(factory, content, "SettingsButton", "SETTINGS", "Gear", theme.deep, Color.white, 300f, 0.38f);

            var bestBadge = factory.Image(content, "BestBadge", theme.pill, theme.deep).rectTransform;
            UiFactory.Place(bestBadge, new Vector2(0.5f, 0.5f), new Vector2(0f, -600f), new Vector2(500f, 96f));
            var bestIcon = factory.Image(bestBadge, "Icon", GameArtGenerator.Icon("Crown"), theme.accent);
            UiFactory.Place(bestIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(40f, 2f), new Vector2(54f, 54f));
            var best = factory.Text(bestBadge, "BestScore", string.Empty, 46, theme.textLight);
            UiFactory.Stretch(best.rectTransform, 90f, 40f, 0f, 2f);
            UiFactory.Entrance(bestBadge.gameObject, 0.46f, new Vector2(0f, -40f));

            var notice = factory.Text(content, "SaveNotice", string.Empty, 32, theme.danger, body: true);
            notice.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.Place(notice.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -700f), new Vector2(940f, 110f));
            notice.gameObject.SetActive(false);

            var settingsPanel = InstantiatePanel(ContentPaths.SettingsPanelPrefabPath, root);
            var collectionPanel = InstantiatePanel(ContentPaths.CollectionPanelPrefabPath, root);
            var achievementsPanel = InstantiatePanel(ContentPaths.AchievementsPanelPrefabPath, root);

            UiFactory.EventSystem();

            var entry = new GameObject("MainMenuEntryPoint").AddComponent<MainMenuEntryPoint>();
            EditorRefs.Set(entry,
                ("playButton", play), ("settingsButton", settings), ("collectionButton", collection),
                ("achievementsButton", achievements), ("bestScoreText", best), ("bestScoreBadge", bestBadge.gameObject),
                ("saveNoticeText", notice),
                ("settingsPanel", settingsPanel.GetComponent<SettingsPanel>()),
                ("collectionPanel", collectionPanel.GetComponent<CollectionPanel>()),
                ("achievementsPanel", achievementsPanel.GetComponent<AchievementsPanel>()),
                ("dailyChallengeCard", daily),
                ("developmentConfig", AssetDatabase.LoadAssetAtPath<GameConfig>(FoundationSetup.GameConfigPath)));
        }

        internal static void BuildBackground(UiFactory factory, Transform root)
        {
            var theme = factory.Theme;
            UiFactory.Stretch(factory.Image(root, "Background", theme.backgroundGradient, Color.white).rectTransform);
            factory.Ornament(root, "OrnamentTopLeft", new Vector2(0f, 1f), new Vector2(-150f, 150f), 560f, 0.2f);
            factory.Ornament(root, "OrnamentBottomRight", new Vector2(1f, 0f), new Vector2(170f, -170f), 620f, 0.2f);
            factory.Ornament(root, "OrnamentMidRight", new Vector2(1f, 0.5f), new Vector2(120f, 60f), 300f, 0.12f);
            factory.Ornament(root, "OrnamentMidLeft", new Vector2(0f, 0.5f), new Vector2(-110f, -240f), 260f, 0.12f);

            foreach (var (stripName, anchor) in new[] { ("TopStrip", 1f), ("BottomStrip", 0f) })
            {
                var strip = factory.Strip(root, stripName, 40f).rectTransform;
                strip.anchorMin = new Vector2(0f, anchor);
                strip.anchorMax = new Vector2(1f, anchor);
                strip.pivot = new Vector2(0.5f, anchor);
                strip.anchoredPosition = Vector2.zero;
            }
        }

        private static void AddItemDecor(UiFactory factory, Transform parent, MergeProgression progression, string id, Vector2 position, float size, float rotation)
        {
            Sprite sprite = null;
            for (var level = 0; progression != null && level < progression.Count; level++)
            {
                if (progression.Get(level)?.Id == id)
                {
                    sprite = progression.Get(level).Sprite;
                }
            }

            if (sprite == null)
            {
                return;
            }

            var image = factory.Image(parent, "Decor_" + id, sprite, Color.white);
            image.preserveAspect = true;
            UiFactory.Place(image.rectTransform, new Vector2(0.5f, 0.5f), position, new Vector2(size, size));
            image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        private static Button IconTile(UiFactory factory, Transform parent, string name, string caption, string icon, Color color, Color iconColor, float x, float delay)
        {
            var theme = factory.Theme;
            var tile = UiFactory.Place(UiFactory.Rect(name + "Tile", parent), new Vector2(0.5f, 0.5f), new Vector2(x, -350f), new Vector2(240f, 270f));
            var button = factory.Button(tile, name, string.Empty, color, new Vector2(190f, 190f), icon: GameArtGenerator.Icon(icon), iconColor: iconColor);
            UiFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(190f, 190f));
            UiFactory.Record(button.transform);
            var label = factory.Text(tile, "Caption", caption, 36, theme.deep);
            UiFactory.Place(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(260f, 56f));
            UiFactory.Entrance(tile.gameObject, delay, new Vector2(0f, -80f));
            return button;
        }

        private static GameObject InstantiatePanel(string prefabPath, Transform parent)
        {
            var panel = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), parent);
            UiFactory.Stretch((RectTransform)panel.transform);
            UiFactory.Record(panel.transform);
            return panel;
        }

        private static DailyChallengeCard BuildDailyChallengeCard(UiFactory factory, Transform parent)
        {
            var theme = factory.Theme;
            var card = factory.Panel(parent, "DailyChallengeCard", theme.card, theme.secondary, 6f).rectTransform;
            UiFactory.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0f, 236f), new Vector2(920f, 370f));
            GameplaySceneBuilder.AddShadow(card.Find("Rim").gameObject, theme, 0.18f, 10f);

            var header = factory.Panel(card, "Header", theme.secondary).rectTransform;
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.offsetMin = new Vector2(12f, -84f);
            header.offsetMax = new Vector2(-12f, -12f);
            var caption = factory.Text(header, "Caption", "TODAY'S CHALLENGE", 36, theme.textLight);
            UiFactory.Stretch(caption.rectTransform, 0f, 0f, 0f, 2f);
            var star = factory.Image(header, "Star", GameArtGenerator.Icon("Star"), theme.accent);
            UiFactory.Place(star.rectTransform, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(44f, 44f));

            var title = factory.Text(card, "Title", string.Empty, 50, theme.textDark);
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(820f, 62f));
            var description = factory.Text(card, "Description", string.Empty, 32, theme.mutedText, body: true);
            description.fontStyle = FontStyle.Normal;
            UiFactory.Place(description.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -164f), new Vector2(820f, 46f));

            var bar = factory.Image(card, "ProgressBar", theme.pill, new Color(theme.deep.r, theme.deep.g, theme.deep.b, 0.12f));
            UiFactory.Place(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(-70f, -226f), new Vector2(620f, 40f));
            var fill = factory.Image(bar.transform, "Fill", theme.pill, theme.accent);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            UiFactory.Stretch(fill.rectTransform);
            var progress = factory.Text(card, "Progress", string.Empty, 36, theme.textDark, TextAnchor.MiddleLeft);
            UiFactory.Place(progress.rectTransform, new Vector2(0.5f, 1f), new Vector2(320f, -222f), new Vector2(200f, 50f));

            var status = factory.Text(card, "Status", string.Empty, 32, theme.primary, TextAnchor.MiddleLeft);
            UiFactory.Place(status.rectTransform, new Vector2(0f, 0f), new Vector2(46f, 30f), new Vector2(420f, 48f));
            var streak = factory.Text(card, "Streak", string.Empty, 28, theme.mutedText, TextAnchor.MiddleRight, body: true);
            UiFactory.Place(streak.rectTransform, new Vector2(1f, 0f), new Vector2(-46f, 32f), new Vector2(440f, 46f));

            var mark = factory.Image(card, "CompletedMark", theme.disc, theme.secondary);
            UiFactory.Place(mark.rectTransform, new Vector2(1f, 1f), new Vector2(22f, 22f), new Vector2(92f, 92f));
            var check = factory.Image(mark.transform, "Check", GameArtGenerator.Icon("Check"), Color.white);
            UiFactory.Stretch(check.rectTransform, 18f, 18f, 18f, 18f);
            mark.gameObject.SetActive(false);

            var claim = factory.Button(card, "ClaimButton", "CLAIM", theme.primary, new Vector2(260f, 96f), 40);
            UiFactory.Place((RectTransform)claim.transform, new Vector2(1f, 0f), new Vector2(-28f, 18f), new Vector2(260f, 96f));
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
