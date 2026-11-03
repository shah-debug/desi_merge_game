using DesiMerge.Configuration;
using DesiMerge.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.Editor.Content
{
    /// <summary>
    /// MainMenu.unity: rank card, title, best score, Play, Daily Rewards, Missions (with today's challenge), Collection,
    /// Awards and Settings, plus the reward popup. No buttons for features that don't exist (e.g. a shop).
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
            var scaler = safe.gameObject.AddComponent<ResponsiveScaler>();
            var topRow = UiFactory.Place(UiFactory.Rect("TopRow", safe), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(1080f, 172f));

            var progression = AssetDatabase.LoadAssetAtPath<MergeProgression>(ContentPaths.MergeProgressionPath);
            AddItemDecor(factory, content, progression, "aam", new Vector2(-410f, 520f), 150f, 14f);
            AddItemDecor(factory, content, progression, "chai", new Vector2(414f, 506f), 160f, -10f);
            AddItemDecor(factory, content, progression, "rickshaw", new Vector2(-380f, -820f), 180f, -8f);
            AddItemDecor(factory, content, progression, "mango_shake", new Vector2(390f, -826f), 160f, 9f);
            AddItemDecor(factory, content, progression, "tamatar", new Vector2(-150f, -858f), 92f, -14f);
            AddItemDecor(factory, content, progression, "nimbu", new Vector2(160f, -866f), 86f, 18f);

            var (rank, rankTitle, rankProgress, rankFill, rankDot) = BuildRankCard(factory, topRow, out var avatar, out var newPlayerBadge);
            UiFactory.Entrance(rank.gameObject, 0f, new Vector2(0f, 120f));
            var settings = factory.Button(topRow, "SettingsButton", string.Empty, theme.deep, new Vector2(140f, 160f), icon: GameArtGenerator.Icon("Gear"));
            UiFactory.Place((RectTransform)settings.transform, new Vector2(0.5f, 0.5f), new Vector2(440f, 0f), new Vector2(140f, 160f));
            UiFactory.Record(settings.transform);
            UiFactory.Entrance(settings.gameObject, 0.04f, new Vector2(0f, 120f));
            var diamonds = DiamondPill(factory, topRow, new Vector2(0.5f, 0.5f), new Vector2(277f, 0f), new Vector2(150f, 160f));
            UiFactory.Entrance(diamonds.gameObject, 0.06f, new Vector2(0f, 120f));

            var plate = factory.Panel(content, "TitlePlate", theme.deep, theme.accent, 8f).rectTransform;
            UiFactory.Place(plate, new Vector2(0.5f, 0.5f), new Vector2(0f, 560f), new Vector2(880f, 300f));
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
            UiFactory.Place(tagline.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(800f, 60f));
            factory.Ornament(plate, "OrnamentLeft", new Vector2(0f, 0.5f), new Vector2(-40f, 0f), 120f);
            factory.Ornament(plate, "OrnamentRight", new Vector2(1f, 0.5f), new Vector2(40f, 0f), 120f);
            UiFactory.Entrance(plate.gameObject, 0.06f, new Vector2(0f, 220f));

            var bestBadge = factory.Image(content, "BestBadge", theme.pill, theme.deep).rectTransform;
            UiFactory.Place(bestBadge, new Vector2(0.5f, 0.5f), new Vector2(0f, 340f), new Vector2(480f, 92f));
            var bestIcon = factory.Image(bestBadge, "Icon", GameArtGenerator.Icon("Crown"), theme.accent);
            UiFactory.Place(bestIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(40f, 2f), new Vector2(52f, 52f));
            var best = factory.Text(bestBadge, "BestScore", string.Empty, 44, theme.textLight);
            UiFactory.Stretch(best.rectTransform, 90f, 40f, 0f, 2f);
            UiFactory.Entrance(bestBadge.gameObject, 0.1f, new Vector2(0f, 60f));

            var play = factory.Button(content, "PlayButton", "PLAY", theme.primary, new Vector2(680f, 210f), 112, GameArtGenerator.Icon("Play"));
            UiFactory.Place((RectTransform)play.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(680f, 210f));
            UiFactory.Record(play.transform);
            UiFactory.Entrance(play.gameObject, 0.16f, new Vector2(0f, -80f));
            UiFactory.Punch(play.gameObject, 0.06f, 0.5f, true);

            var (daily, dailyDot) = FeatureTile(factory, content, "DailyRewardsButton", "DAILY REWARDS", "Gift", theme.accent, theme.deep, new Vector2(-226f, -150f), 0.22f);
            var (missions, missionsDot) = FeatureTile(factory, content, "MissionsButton", "MISSIONS", "Missions", theme.secondary, Color.white, new Vector2(226f, -150f), 0.26f);
            var (collection, _) = FeatureTile(factory, content, "CollectionButton", "COLLECTION", "Grid", theme.deep, Color.white, new Vector2(-226f, -440f), 0.3f, false);
            var (achievements, _) = FeatureTile(factory, content, "AchievementsButton", "RECORDS", "Podium", theme.primary, Color.white, new Vector2(226f, -440f), 0.34f, false);

            var notice = factory.Text(content, "SaveNotice", string.Empty, 32, theme.danger, body: true);
            notice.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.Place(notice.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -660f), new Vector2(940f, 110f));
            notice.gameObject.SetActive(false);

            var settingsPanel = InstantiatePanel(ContentPaths.SettingsPanelPrefabPath, root);
            var collectionPanel = InstantiatePanel(ContentPaths.CollectionPanelPrefabPath, root);
            var challenge = BuildDailyChallengeCard(factory, content);
            var dailyPanel = RewardScreensBuilder.BuildDailyRewardsPanel(factory, root);
            var missionsPanel = RewardScreensBuilder.BuildMissionsPanel(factory, root, challenge);
            var ranksPanel = RewardScreensBuilder.BuildRanksPanel(factory, root);
            var recordsPanel = NavigationScreensBuilder.BuildRecordsPanel(factory, root);
            var presenter = RewardScreensBuilder.BuildRewardPresenter(factory, root);

            EditorRefs.Set(scaler, ("targets", new Object[]
            {
                content, topRow, Card(dailyPanel.transform), Card(missionsPanel.transform), Card(ranksPanel.transform), Card(presenter.transform),
                Card(settingsPanel.transform), Card(collectionPanel.transform), Card(recordsPanel.transform),
            }));

            UiFactory.EventSystem();

            var entry = new GameObject("MainMenuEntryPoint").AddComponent<MainMenuEntryPoint>();
            EditorRefs.Set(entry,
                ("playButton", play), ("settingsButton", settings), ("collectionButton", collection),
                ("achievementsButton", achievements), ("bestScoreText", best), ("bestScoreBadge", bestBadge.gameObject),
                ("saveNoticeText", notice),
                ("settingsPanel", settingsPanel.GetComponent<SettingsPanel>()),
                ("collectionPanel", collectionPanel.GetComponent<CollectionPanel>()),
                ("achievementsPanel", null), ("recordsPanel", recordsPanel),
                ("profileAvatar", avatar), ("defaultAvatar", GameArtGenerator.Icon("Profile")), ("newPlayerBadge", newPlayerBadge), ("diamondCounter", diamonds),
                ("dailyChallengeCard", challenge),
                ("dailyRewardsButton", daily), ("missionsButton", missions), ("rankButton", rank),
                ("dailyRewardsDot", dailyDot), ("missionsDot", missionsDot), ("rankDot", rankDot),
                ("rankTitleText", rankTitle), ("rankProgressText", rankProgress), ("rankProgressFill", rankFill),
                ("dailyRewardsPanel", dailyPanel), ("missionsPanel", missionsPanel), ("ranksPanel", ranksPanel), ("rewardPresenter", presenter),
                ("developmentConfig", AssetDatabase.LoadAssetAtPath<GameConfig>(FoundationSetup.GameConfigPath)));
        }

        private static RectTransform Card(Transform panel) => (RectTransform)panel.Find("Card");

        /// <summary>Player profile card: avatar (the player's biggest item, or a default face), rank as the name line, progress to the next rank.</summary>
        private static (Button, Text, Text, Image, GameObject) BuildRankCard(UiFactory factory, Transform parent, out Image avatar, out GameObject newPlayerBadge)
        {
            var theme = factory.Theme;
            var button = factory.Button(parent, "RankButton", string.Empty, theme.deep, new Vector2(694f, 160f));
            var rect = (RectTransform)button.transform;
            UiFactory.Place(rect, new Vector2(0.5f, 0.5f), new Vector2(-163f, 0f), new Vector2(694f, 160f));
            UiFactory.Record(rect);
            var disc = factory.Image(rect, "AvatarRing", theme.disc, theme.accent);
            UiFactory.Place(disc.rectTransform, new Vector2(0f, 0.5f), new Vector2(22f, 14f), new Vector2(108f, 108f));
            var inner = factory.Image(disc.transform, "AvatarDisc", theme.disc, theme.card);
            UiFactory.Stretch(inner.rectTransform, 7f, 7f, 7f, 7f);
            avatar = factory.Image(inner.transform, "Avatar", GameArtGenerator.Icon("Profile"), theme.deep);
            avatar.preserveAspect = true;
            UiFactory.Stretch(avatar.rectTransform, 12f, 12f, 10f, 14f);
            var badge = factory.Image(disc.transform, "NewPlayerBadge", theme.pill, theme.primary);
            UiFactory.Place(badge.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -12f), new Vector2(92f, 32f));
            var badgeText = factory.Text(badge.transform, "Text", "NEW", 22, Color.white);
            UiFactory.Stretch(badgeText.rectTransform, 0f, 0f, 0f, 2f);
            newPlayerBadge = badge.gameObject;
            var title = factory.Text(rect, "RankTitle", "New Player", 44, theme.accent, TextAnchor.MiddleLeft);
            UiFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(156f, -20f), new Vector2(500f, 52f));
            var track = factory.Image(rect, "Track", theme.pill, new Color(theme.deep.r, theme.deep.g, theme.deep.b, 0.12f));
            UiFactory.Place(track.rectTransform, new Vector2(0f, 0.5f), new Vector2(156f, 8f), new Vector2(480f, 22f));
            var fill = factory.Image(track.transform, "Fill", theme.pill, theme.accent);
            UiFactory.ProgressFill(fill);
            UiFactory.Stretch(fill.rectTransform);
            var next = factory.Text(rect, "Next", string.Empty, 26, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleLeft, body: true);
            next.fontStyle = FontStyle.Normal;
            UiFactory.Place(next.rectTransform, new Vector2(0f, 0f), new Vector2(156f, 30f), new Vector2(500f, 36f));
            var dot = Dot(factory, rect, new Vector2(1f, 1f), new Vector2(-10f, -10f));
            return (button, title, next, fill, dot);
        }

        private static (Button, GameObject) FeatureTile(UiFactory factory, Transform parent, string name, string caption, string icon, Color color, Color iconColor,
            Vector2 position, float delay, bool withDot = true)
        {
            var theme = factory.Theme;
            var size = withDot ? new Vector2(420f, 250f) : new Vector2(420f, 220f);
            var button = factory.Button(parent, name, string.Empty, color, size);
            var rect = (RectTransform)button.transform;
            UiFactory.Place(rect, new Vector2(0.5f, 0.5f), position, size);
            UiFactory.Record(rect);
            var iconImage = factory.Image(rect, "TileIcon", GameArtGenerator.Icon(icon), iconColor);
            UiFactory.Place(iconImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 34f), new Vector2(110f, 110f));
            var label = factory.Text(rect, "Caption", caption, 44, iconColor);
            UiFactory.Place(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(size.x - 20f, 56f));
            GameObject dot = null;
            if (withDot)
            {
                dot = Dot(factory, rect, new Vector2(1f, 1f), new Vector2(-14f, -14f));
            }

            UiFactory.Entrance(rect.gameObject, delay, new Vector2(0f, -80f));
            return (button, dot);
        }

        /// <summary>Diamonds counter pill (navy, saffron rim, gem + amount).</summary>
        internal static DiamondCounter DiamondPill(UiFactory factory, RectTransform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var theme = factory.Theme;
            var pill = factory.Image(parent, "Diamonds", theme.panel, theme.deep).rectTransform;
            UiFactory.Place(pill, anchor, position, size);
            GameplaySceneBuilder.AddShadow(pill.gameObject, theme, 0.25f, 5f);
            var gem = factory.Image(pill, "Gem", GameArtGenerator.Load(GameArtGenerator.IconPath("DiamondArt")), Color.white);
            gem.preserveAspect = true;
            var tall = size.y > size.x * 0.7f;
            Text amount;
            if (tall)
            {
                // Same height (and corner radius) as the buttons beside it: gem above, number below.
                UiFactory.Place(gem.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(size.y * 0.42f, size.y * 0.42f));
                amount = factory.Text(pill, "Amount", "0", Mathf.RoundToInt(size.y * 0.3f), Color.white, TextAnchor.MiddleCenter);
                UiFactory.Place(amount.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(size.x - 16f, size.y * 0.38f));
            }
            else
            {
                UiFactory.Place(gem.rectTransform, new Vector2(0f, 0.5f), new Vector2(10f, 2f), new Vector2(size.y - 8f, size.y - 8f));
                amount = factory.Text(pill, "Amount", "0", Mathf.RoundToInt(size.y * 0.55f), Color.white, TextAnchor.MiddleCenter);
                UiFactory.Stretch(amount.rectTransform, size.y + 4f, 14f, 0f, 2f);
            }
            var punch = UiFactory.Punch(pill.gameObject, 0.15f, 0.3f, true);
            var counter = pill.gameObject.AddComponent<DiamondCounter>();
            EditorRefs.Set(counter, ("amountText", amount), ("punch", punch));
            return counter;
        }

        internal static GameObject Dot(UiFactory factory, RectTransform parent, Vector2 anchor, Vector2 position)
        {
            var dot = factory.Image(parent, "Dot", factory.Theme.disc, factory.Theme.danger);
            UiFactory.Place(dot.rectTransform, anchor, position, new Vector2(52f, 52f));
            var mark = factory.Text(dot.transform, "Mark", "!", 38, Color.white);
            UiFactory.Stretch(mark.rectTransform, 0f, 0f, 0f, 2f);
            UiFactory.Punch(dot.gameObject, 0.2f, 0.4f, true);
            dot.gameObject.SetActive(false);
            return dot.gameObject;
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

        internal static GameObject InstantiatePanel(string prefabPath, Transform parent)
        {
            var panel = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), parent);
            UiFactory.Stretch((RectTransform)panel.transform);
            UiFactory.Record(panel.transform);
            return panel;
        }

        internal static DailyChallengeCard BuildDailyChallengeCard(UiFactory factory, Transform parent)
        {
            var theme = factory.Theme;
            var card = factory.Panel(parent, "DailyChallengeCard", theme.card, theme.secondary, 6f).rectTransform;
            UiFactory.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0f, 236f), new Vector2(920f, 400f));
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
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(820f, 62f));
            var description = factory.Text(card, "Description", string.Empty, 32, theme.mutedText, body: true);
            description.fontStyle = FontStyle.Normal;
            UiFactory.Place(description.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -152f), new Vector2(820f, 46f));

            var bar = factory.Image(card, "ProgressBar", theme.pill, new Color(theme.deep.r, theme.deep.g, theme.deep.b, 0.12f));
            UiFactory.Place(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(-100f, -212f), new Vector2(580f, 32f));
            var fill = factory.Image(bar.transform, "Fill", theme.pill, theme.accent);
            UiFactory.ProgressFill(fill);
            UiFactory.Stretch(fill.rectTransform);
            var progress = factory.Text(card, "Progress", string.Empty, 36, theme.textDark, TextAnchor.MiddleLeft);
            UiFactory.Place(progress.rectTransform, new Vector2(0.5f, 1f), new Vector2(305f, -204f), new Vector2(190f, 48f));

            var status = factory.Text(card, "Status", string.Empty, 32, theme.primary, TextAnchor.MiddleLeft);
            UiFactory.Place(status.rectTransform, new Vector2(0f, 0f), new Vector2(40f, 74f), new Vector2(560f, 44f));
            var streak = factory.Text(card, "Streak", string.Empty, 26, theme.mutedText, TextAnchor.MiddleLeft, body: true);
            UiFactory.Place(streak.rectTransform, new Vector2(0f, 0f), new Vector2(40f, 28f), new Vector2(560f, 40f));

            var mark = factory.Image(card, "CompletedMark", theme.disc, theme.secondary);
            UiFactory.Place(mark.rectTransform, new Vector2(1f, 1f), new Vector2(-18f, -14f), new Vector2(62f, 62f));
            var check = factory.Image(mark.transform, "Check", GameArtGenerator.Icon("Check"), Color.white);
            UiFactory.Stretch(check.rectTransform, 18f, 18f, 18f, 18f);
            mark.gameObject.SetActive(false);

            var claim = factory.Button(card, "ClaimButton", "CLAIM", theme.primary, new Vector2(240f, 92f), 40);
            UiFactory.Place((RectTransform)claim.transform, new Vector2(1f, 0f), new Vector2(-26f, 24f), new Vector2(240f, 92f));
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
