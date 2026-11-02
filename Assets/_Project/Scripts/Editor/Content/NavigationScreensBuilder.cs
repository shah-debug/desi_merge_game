using DesiMerge.UI;
using DesiMerge.UI.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.Editor.Content
{
    /// <summary>Records (Achievements / Score tabs) and the in-game hamburger menu with its screens.</summary>
    internal static class NavigationScreensBuilder
    {
        public static RecordsPanel BuildRecordsPanel(UiFactory factory, Transform root)
        {
            var theme = factory.Theme;
            var (transition, card) = factory.Modal(root, "RecordsPanel", new Vector2(960f, 1600f), "RECORDS", theme.deep);
            var close = RewardScreensBuilder.CloseButton(factory, card);

            var tabButtons = new Object[2];
            var tabBackgrounds = new Object[2];
            var tabLabels = new Object[2];
            var tabIcons = new Object[2];
            var names = new[] { ("ACHIEVEMENTS", "Trophy"), ("SCORE", "Chart") };
            for (var i = 0; i < names.Length; i++)
            {
                var tab = factory.Button(card, names[i].Item1 + "Tab", names[i].Item1, theme.card, new Vector2(436f, 104f), 38, GameArtGenerator.Icon(names[i].Item2), theme.deep);
                UiFactory.Place((RectTransform)tab.transform, new Vector2(0.5f, 1f), new Vector2(-222f + i * 444f, -186f), new Vector2(436f, 104f));
                UiFactory.Record(tab.transform);
                tabButtons[i] = tab;
                tabBackgrounds[i] = tab.GetComponent<Image>();
                tabLabels[i] = tab.transform.Find("Label").GetComponent<Text>();
                tabIcons[i] = tab.transform.Find("Icon").GetComponent<Image>();
            }

            var note = factory.Text(card, "Note", string.Empty, 30, theme.mutedText, body: true);
            UiFactory.Place(note.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -302f), new Vector2(860f, 44f));

            var achievementsPage = Page(card, "AchievementsPage");
            var summary = factory.Text(achievementsPage, "Summary", string.Empty, 30, theme.mutedText, body: true);
            UiFactory.Place(summary.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(860f, 44f));
            var (achievementScroll, achievementList) = ScrollList(factory, achievementsPage, "Scroll", 56f);
            var template = RewardScreensBuilder.ProgressRow(factory, achievementList, "AchievementRow", 0f, leadingIcon: true);

            var scorePage = Page(card, "ScorePage");
            var personal = factory.Text(scorePage, "Personal", "Personal records on this phone", 30, theme.mutedText, body: true);
            UiFactory.Place(personal.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(860f, 44f));
            var (scoreScroll, scoreList) = ScrollList(factory, scorePage, "Scroll", 56f);
            var stats = StatsCard(factory, scoreList, out var best, out var games, out var merges, out var streak, out var highestName, out var highestImage, out var rank);
            var caption = factory.Text(scoreList, "RecentCaption", "RECENT GAMES", 40, theme.deep);
            caption.rectTransform.sizeDelta = new Vector2(900f, 64f);
            var empty = factory.Text(scoreList, "RecentEmpty", "Finish a game to start your score history.", 32, theme.mutedText, body: true);
            empty.rectTransform.sizeDelta = new Vector2(900f, 70f);
            var recent = RecentRow(factory, scoreList);
            stats.SetAsFirstSibling();

            var panel = transition.gameObject.AddComponent<RecordsPanel>();
            EditorRefs.Set(panel, ("transition", transition), ("closeButton", close), ("tabButtons", tabButtons), ("tabBackgrounds", tabBackgrounds),
                ("tabLabels", tabLabels), ("tabIcons", tabIcons), ("tabSelectedColor", theme.deep), ("tabNormalColor", ArtCanvas.Hex("#F1E6D2")),
                ("tabSelectedText", theme.accent), ("tabNormalText", theme.mutedText),
                ("achievementsPage", achievementsPage.gameObject), ("scorePage", scorePage.gameObject),
                ("achievementsScroll", achievementScroll), ("scoreScroll", scoreScroll),
                ("achievementList", achievementList), ("achievementTemplate", template), ("achievementSummary", summary),
                ("gamesIcon", GameArtGenerator.Icon("Play")), ("mergesIcon", GameArtGenerator.Icon("Grid")), ("scoreIcon", GameArtGenerator.Icon("Crown")),
                ("discoverIcon", GameArtGenerator.Icon("Star")), ("streakIcon", GameArtGenerator.Icon("Calendar")),
                ("bestText", best), ("gamesText", games), ("mergesText", merges), ("streakText", streak), ("highestItemText", highestName),
                ("highestItemImage", highestImage), ("rankText", rank), ("recentList", scoreList), ("recentTemplate", recent), ("recentEmptyText", empty));
            note.gameObject.SetActive(false);
            return panel;
        }

        public static SkinsPanel BuildSkinsPanel(UiFactory factory, Transform root)
        {
            var theme = factory.Theme;
            var (transition, card) = factory.Modal(root, "SkinsPanel", new Vector2(960f, 1500f), "SKINS", theme.accent);
            card.Find("Header/Title").GetComponent<Text>().color = theme.deep;
            var close = RewardScreensBuilder.CloseButton(factory, card);
            var diamonds = MainMenuSceneBuilder.DiamondPill(factory, card, new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(200f, 72f));
            var status = factory.Text(card, "Status", string.Empty, 30, theme.primary, body: true);
            status.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.Place(status.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(860f, 80f));
            status.gameObject.SetActive(false);
            var page = UiFactory.Stretch(UiFactory.Rect("Page", card), 30f, 30f, 280f, 120f);
            var (_, list) = ScrollList(factory, page, "Scroll", 0f);

            var row = factory.Panel(list, "SkinRow", Color.white).rectTransform;
            row.sizeDelta = new Vector2(880f, 150f);
            GameplaySceneBuilder.AddShadow(row.gameObject, theme, 0.15f, 6f);
            var disc = factory.Image(row, "PreviewDisc", theme.disc, ArtCanvas.Hex("#F1E6D2"));
            UiFactory.Place(disc.rectTransform, new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(118f, 118f));
            var preview = factory.Image(disc.transform, "Preview", null, Color.white);
            preview.preserveAspect = true;
            UiFactory.Stretch(preview.rectTransform, 8f, 8f, 8f, 8f);
            var title = factory.Text(row, "Title", string.Empty, 44, theme.deep, TextAnchor.MiddleLeft);
            UiFactory.Place(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(160f, 20f), new Vector2(400f, 56f));
            var detail = factory.Text(row, "Detail", string.Empty, 26, theme.mutedText, TextAnchor.MiddleLeft, body: true);
            UiFactory.Place(detail.rectTransform, new Vector2(0f, 0.5f), new Vector2(160f, -30f), new Vector2(420f, 40f));
            var use = factory.Button(row, "UseButton", "USE", theme.secondary, new Vector2(230f, 100f), 40);
            UiFactory.Place((RectTransform)use.transform, new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(230f, 100f));
            UiFactory.Record(use.transform);
            var buy = factory.Button(row, "BuyButton", string.Empty, theme.accent, new Vector2(230f, 100f));
            UiFactory.Place((RectTransform)buy.transform, new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(230f, 100f));
            UiFactory.Record(buy.transform);
            var buyGroup = buy.gameObject.AddComponent<CanvasGroup>();
            var gem = factory.Image(buy.transform, "Gem", GameArtGenerator.Load(GameArtGenerator.IconPath("DiamondArt")), Color.white);
            gem.preserveAspect = true;
            UiFactory.Place(gem.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-52f, 5f), new Vector2(56f, 56f));
            var price = factory.Text(buy.transform, "Price", "0", 42, theme.deep);
            UiFactory.Place(price.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(26f, 3f), new Vector2(130f, 60f));
            var inUse = factory.Image(row, "InUse", theme.disc, theme.secondary);
            UiFactory.Place(inUse.rectTransform, new Vector2(1f, 0.5f), new Vector2(-90f, 0f), new Vector2(92f, 92f));
            var check = factory.Image(inUse.transform, "Check", GameArtGenerator.Icon("Check"), Color.white);
            UiFactory.Stretch(check.rectTransform, 18f, 18f, 18f, 18f);
            var template = row.gameObject.AddComponent<SkinRow>();
            EditorRefs.Set(template, ("preview", preview), ("title", title), ("detail", detail), ("useButton", use), ("buyButton", buy),
                ("priceText", price), ("inUseMark", inUse.gameObject), ("buyGroup", buyGroup));

            var panel = transition.gameObject.AddComponent<SkinsPanel>();
            EditorRefs.Set(panel, ("transition", transition), ("closeButton", close), ("list", list), ("rowTemplate", template),
                ("statusText", status), ("diamonds", diamonds));
            return panel;
        }

        /// <summary>The in-game menu: a dropdown under the hamburger button, plus the screens it opens.</summary>
        public static GameplayMenu BuildGameplayMenu(UiFactory factory, Transform canvasRoot, RectTransform safe, System.Collections.Generic.List<Object> responsive)
        {
            var theme = factory.Theme;
            var root = UiFactory.Stretch(UiFactory.Rect("GameMenu", safe));
            root.gameObject.AddComponent<CanvasGroup>();
            var dim = factory.Image(root, "OutsideTap", null, new Color(theme.overlay.r, theme.overlay.g, theme.overlay.b, 0.45f), raycast: true);
            UiFactory.Stretch(dim.rectTransform, -200f, -200f, -200f, -200f);
            var outside = dim.gameObject.AddComponent<Button>();
            outside.transition = Selectable.Transition.None;

            var card = factory.Panel(root, "Card", theme.card, theme.accent, 8f).rectTransform;
            UiFactory.Place(card, new Vector2(0f, 1f), new Vector2(24f, -246f), new Vector2(720f, 980f));
            GameplaySceneBuilder.AddShadow(card.Find("Rim").gameObject, theme, 0.3f, 14f);
            var header = factory.Panel(card, "Header", theme.deep).rectTransform;
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.offsetMin = new Vector2(14f, -124f);
            header.offsetMax = new Vector2(-14f, -14f);
            var headerIcon = factory.Image(header, "Icon", GameArtGenerator.Icon("Menu"), theme.accent);
            UiFactory.Place(headerIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(34f, 0f), new Vector2(56f, 56f));
            var title = factory.Text(header, "Title", "MENU", 60, theme.accent, TextAnchor.MiddleLeft, outline: true);
            UiFactory.Place(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(110f, 0f), new Vector2(400f, 80f));
            var close = RewardScreensBuilder.CloseButton(factory, card);

            Button Tile(string name, string label, string icon, Color color, Color iconColor, int index)
            {
                var column = index % 2;
                var row = index / 2;
                var tile = factory.Button(card, name, label, color, new Vector2(332f, 150f), 34, GameArtGenerator.Icon(icon), iconColor);
                UiFactory.Place((RectTransform)tile.transform, new Vector2(0f, 1f), new Vector2(20f + column * 348f, -150f - row * 166f), new Vector2(332f, 150f));
                if (iconColor == theme.deep)
                {
                    GameplaySceneBuilder.SetLabelColor(tile, theme.deep);
                }

                UiFactory.Record(tile.transform);
                return tile;
            }

            var records = Tile("RecordsButton", "RECORDS", "Podium", theme.deep, Color.white, 0);
            var awards = Tile("AchievementsButton", "AWARDS", "Trophy", theme.primary, Color.white, 1);
            var collection = Tile("CollectionButton", "COLLECTION", "Grid", theme.secondary, Color.white, 2);
            var daily = Tile("DailyRewardsButton", "DAILY", "Gift", theme.accent, theme.deep, 3);
            var missions = Tile("MissionsButton", "MISSIONS", "Missions", theme.secondary, Color.white, 4);
            var settings = Tile("SettingsButton", "SETTINGS", "Gear", theme.deep, Color.white, 5);
            var help = Tile("HelpButton", "HOW TO PLAY", "Help", theme.accent, theme.deep, 6);
            var home = Tile("HomeButton", "HOME", "Home", theme.danger, Color.white, 7);
            var dailyDot = MainMenuSceneBuilder.Dot(factory, (RectTransform)daily.transform, new Vector2(1f, 1f), new Vector2(-8f, -8f));
            var missionsDot = MainMenuSceneBuilder.Dot(factory, (RectTransform)missions.transform, new Vector2(1f, 1f), new Vector2(-8f, -8f));

            var resume = factory.Button(card, "ResumeButton", "RETURN TO GAME", theme.primary, new Vector2(680f, 140f), 48, GameArtGenerator.Icon("Play"));
            UiFactory.Place((RectTransform)resume.transform, new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(680f, 140f));
            UiFactory.Record(resume.transform);

            var transition = root.gameObject.AddComponent<PanelTransition>();
            EditorRefs.Set(transition, ("card", card), ("duration", theme.panelDuration));
            responsive.Add(card);

            var recordsPanel = BuildRecordsPanel(factory, canvasRoot);
            var collectionPanel = MainMenuSceneBuilder.InstantiatePanel(ContentPaths.CollectionPanelPrefabPath, canvasRoot);
            var settingsPanel = MainMenuSceneBuilder.InstantiatePanel(ContentPaths.SettingsPanelPrefabPath, canvasRoot);
            var dailyPanel = RewardScreensBuilder.BuildDailyRewardsPanel(factory, canvasRoot);
            var challenge = MainMenuSceneBuilder.BuildDailyChallengeCard(factory, canvasRoot);
            var missionsPanel = RewardScreensBuilder.BuildMissionsPanel(factory, canvasRoot, challenge);
            var skinsPanel = BuildSkinsPanel(factory, canvasRoot);
            var presenter = RewardScreensBuilder.BuildRewardPresenter(factory, canvasRoot);
            foreach (var screen in new Component[] { recordsPanel, collectionPanel.transform, settingsPanel.transform, dailyPanel, missionsPanel, skinsPanel, presenter })
            {
                responsive.Add(screen.transform.Find("Card"));
            }

            var menu = root.gameObject.AddComponent<GameplayMenu>();
            EditorRefs.Set(menu, ("transition", transition), ("outsideButton", outside), ("closeButton", close), ("resumeButton", resume),
                ("recordsButton", records), ("achievementsButton", awards), ("collectionButton", collection), ("dailyRewardsButton", daily),
                ("missionsButton", missions), ("settingsButton", settings), ("helpButton", help), ("homeButton", home),
                ("dailyRewardsDot", dailyDot), ("missionsDot", missionsDot), ("records", recordsPanel),
                ("collection", collectionPanel.GetComponent<CollectionPanel>()), ("settings", settingsPanel.GetComponent<SettingsPanel>()),
                ("dailyRewards", dailyPanel), ("missions", missionsPanel), ("rewardPresenter", presenter), ("skins", skinsPanel));
            return menu;
        }

        private static RectTransform Page(RectTransform card, string name) => UiFactory.Stretch(UiFactory.Rect(name, card), 30f, 30f, 340f, 36f);

        private static (ScrollRect, RectTransform) ScrollList(UiFactory factory, RectTransform parent, string name, float top)
        {
            var viewport = UiFactory.Stretch(UiFactory.Rect(name, parent), 0f, 0f, top, 0f);
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = UiFactory.Rect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.padding = new RectOffset(0, 0, 10, 24);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;
            return (scroll, content);
        }

        private static RectTransform StatsCard(UiFactory factory, RectTransform list, out Text best, out Text games, out Text merges, out Text streak,
            out Text highestName, out Image highestImage, out Text rank)
        {
            var theme = factory.Theme;
            var card = factory.Panel(list, "Stats", Color.white).rectTransform;
            card.sizeDelta = new Vector2(900f, 470f);
            GameplaySceneBuilder.AddShadow(card.gameObject, theme, 0.15f, 6f);

            var crown = factory.Image(card, "BestIcon", GameArtGenerator.Icon("Crown"), theme.accent);
            UiFactory.Place(crown.rectTransform, new Vector2(0f, 1f), new Vector2(34f, -30f), new Vector2(70f, 70f));
            var bestCaption = factory.Text(card, "BestCaption", "PERSONAL BEST", 32, theme.mutedText, TextAnchor.MiddleLeft);
            UiFactory.Place(bestCaption.rectTransform, new Vector2(0f, 1f), new Vector2(120f, -26f), new Vector2(400f, 40f));
            best = factory.Text(card, "Best", "0", 84, theme.primary, TextAnchor.MiddleLeft);
            UiFactory.Place(best.rectTransform, new Vector2(0f, 1f), new Vector2(118f, -62f), new Vector2(420f, 96f));

            highestImage = factory.Image(card, "HighestItem", null, Color.white);
            highestImage.preserveAspect = true;
            UiFactory.Place(highestImage.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -24f), new Vector2(130f, 130f));
            highestName = factory.Text(card, "HighestName", string.Empty, 30, theme.deep, TextAnchor.MiddleRight);
            UiFactory.Place(highestName.rectTransform, new Vector2(1f, 1f), new Vector2(-30f, -156f), new Vector2(300f, 40f));
            var highestCaption = factory.Text(card, "HighestCaption", "BIGGEST ITEM", 24, theme.mutedText, TextAnchor.MiddleRight);
            UiFactory.Place(highestCaption.rectTransform, new Vector2(1f, 1f), new Vector2(-30f, -194f), new Vector2(300f, 32f));

            Text Stat(string caption, int column)
            {
                var x = 34f + column * 290f;
                var label = factory.Text(card, caption + "Caption", caption, 26, theme.mutedText, TextAnchor.MiddleLeft);
                UiFactory.Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(x, -246f), new Vector2(280f, 34f));
                var value = factory.Text(card, caption + "Value", "0", 46, theme.deep, TextAnchor.MiddleLeft);
                UiFactory.Place(value.rectTransform, new Vector2(0f, 1f), new Vector2(x, -282f), new Vector2(280f, 60f));
                return value;
            }

            games = Stat("GAMES", 0);
            merges = Stat("MERGES", 1);
            streak = Stat("DAY STREAK", 2);
            streak.fontSize = 34;

            var rankStrip = factory.Image(card, "RankStrip", theme.pill, new Color(theme.deep.r, theme.deep.g, theme.deep.b, 0.08f));
            UiFactory.Place(rankStrip.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(840f, 76f));
            var medal = factory.Image(rankStrip.transform, "Medal", GameArtGenerator.Icon("Medal"), theme.primary);
            UiFactory.Place(medal.rectTransform, new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(52f, 52f));
            rank = factory.Text(rankStrip.transform, "Rank", string.Empty, 28, theme.deep, TextAnchor.MiddleLeft, body: true);
            UiFactory.Stretch(rank.rectTransform, 84f, 20f, 0f, 0f);
            return card;
        }

        private static ScoreRecordRow RecentRow(UiFactory factory, RectTransform list)
        {
            var theme = factory.Theme;
            var row = factory.Panel(list, "RecentRow", Color.white).rectTransform;
            row.sizeDelta = new Vector2(900f, 100f);
            var disc = factory.Image(row, "IndexDisc", theme.disc, theme.deep);
            UiFactory.Place(disc.rectTransform, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(64f, 64f));
            var index = factory.Text(disc.transform, "Index", "1", 32, theme.accent);
            UiFactory.Stretch(index.rectTransform, 0f, 0f, 0f, 2f);
            var score = factory.Text(row, "Score", "0", 50, theme.deep, TextAnchor.MiddleLeft);
            UiFactory.Place(score.rectTransform, new Vector2(0f, 0.5f), new Vector2(110f, 0f), new Vector2(260f, 70f));
            var date = factory.Text(row, "Date", string.Empty, 28, theme.mutedText, TextAnchor.MiddleLeft, body: true);
            UiFactory.Place(date.rectTransform, new Vector2(0f, 0.5f), new Vector2(380f, 0f), new Vector2(230f, 50f));
            var badge = factory.Image(row, "NewBest", theme.pill, theme.accent);
            UiFactory.Place(badge.rectTransform, new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(230f, 62f));
            var star = factory.Image(badge.transform, "Star", GameArtGenerator.Icon("Star"), theme.deep);
            UiFactory.Place(star.rectTransform, new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(40f, 40f));
            var badgeText = factory.Text(badge.transform, "Text", "NEW BEST", 30, theme.deep);
            UiFactory.Stretch(badgeText.rectTransform, 56f, 10f, 0f, 2f);
            var component = row.gameObject.AddComponent<ScoreRecordRow>();
            EditorRefs.Set(component, ("indexText", index), ("scoreText", score), ("dateText", date), ("newBestBadge", badge.gameObject));
            return component;
        }
    }
}
