using DesiMerge.UI;
using DesiMerge.UI.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.Editor.Content
{
    internal static class RewardScreensBuilder
    {
        public static RewardPresenter BuildRewardPresenter(UiFactory factory, Transform root)
        {
            var theme = factory.Theme;
            var overlay = UiFactory.Stretch(UiFactory.Rect("RewardPopup", root));
            overlay.gameObject.AddComponent<CanvasGroup>();
            UiFactory.Stretch(factory.Image(overlay, "Dim", null, theme.overlay, raycast: true).rectTransform);
            var rays = factory.Image(overlay, "Rays", theme.ornament, new Color(1f, 0.8f, 0.3f, 0.35f)).rectTransform;
            UiFactory.Place(rays, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1100f, 1100f));

            var card = factory.Panel(overlay, "Card", theme.card, theme.accent, 10f).rectTransform;
            UiFactory.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(800f, 900f));
            GameplaySceneBuilder.AddShadow(card.Find("Rim").gameObject, theme, 0.3f, 16f);
            var ribbon = Ribbon(factory, card, theme.primary, 150f);
            var title = factory.Text(ribbon, "Title", "REWARD!", theme.headingSize - 6, theme.accent, outline: true);
            UiFactory.Stretch(title.rectTransform, 90f, 90f, 0f, 14f);

            var subtitle = factory.Text(card, "Subtitle", string.Empty, 40, theme.deep);
            UiFactory.Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -196f), new Vector2(720f, 60f));

            var icons = new Object[2];
            var labels = new Object[2];
            var slotObjects = new Object[2];
            var slots = UiFactory.Place(UiFactory.Rect("Rewards", card), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(720f, 360f));
            var layout = slots.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 40f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            for (var i = 0; i < icons.Length; i++)
            {
                var slot = UiFactory.Rect("Reward" + i, slots);
                slot.sizeDelta = new Vector2(300f, 340f);
                var glow = factory.Image(slot, "Glow", theme.glow, new Color(theme.accent.r, theme.accent.g, theme.accent.b, 0.8f));
                UiFactory.Place(glow.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 10f), new Vector2(300f, 300f));
                var disc = factory.Image(slot, "Disc", theme.disc, i == 0 ? theme.primary : theme.secondary);
                UiFactory.Place(disc.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(200f, 200f));
                var icon = factory.Image(disc.transform, "Icon", null, Color.white);
                UiFactory.Stretch(icon.rectTransform, 44f, 44f, 44f, 44f);
                var label = factory.Text(slot, "Label", string.Empty, 46, theme.textDark);
                UiFactory.Place(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(300f, 64f));
                icons[i] = icon;
                labels[i] = label;
                slotObjects[i] = slot.gameObject;
            }

            var punch = UiFactory.Punch(slots.gameObject, 0.18f, 0.4f, true);
            var collect = factory.Button(card, "CollectButton", "COLLECT", theme.primary, new Vector2(520f, 150f), 58, GameArtGenerator.Icon("Check"));
            UiFactory.Place((RectTransform)collect.transform, new Vector2(0.5f, 0f), new Vector2(0f, 56f), new Vector2(520f, 150f));
            UiFactory.Record(collect.transform);

            var transition = overlay.gameObject.AddComponent<PanelTransition>();
            EditorRefs.Set(transition, ("card", card), ("duration", theme.panelDuration));
            var presenter = overlay.gameObject.AddComponent<RewardPresenter>();
            EditorRefs.Set(presenter,
                ("transition", transition), ("titleText", title), ("subtitleText", subtitle), ("rewardSlots", slotObjects), ("rewardIcons", icons), ("rewardLabels", labels),
                ("rays", rays), ("iconPunch", punch), ("collectButton", collect),
                ("hammerIcon", GameArtGenerator.Icon("Hammer")), ("shuffleIcon", GameArtGenerator.Icon("Shuffle")), ("giftIcon", GameArtGenerator.Icon("Gift")));
            return presenter;
        }

        public static DailyRewardsPanel BuildDailyRewardsPanel(UiFactory factory, Transform root)
        {
            var theme = factory.Theme;
            var (transition, card) = factory.Modal(root, "DailyRewardsPanel", new Vector2(960f, 1300f), "DAILY REWARDS", theme.primary);
            var close = CloseButton(factory, card);
            var streak = factory.Text(card, "Streak", string.Empty, 40, theme.deep);
            UiFactory.Place(streak.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -194f), new Vector2(860f, 56f));

            var tiles = new Object[7];
            for (var i = 0; i < tiles.Length; i++)
            {
                var row = i < 4 ? 0 : 1;
                var special = i == 6;
                var x = row == 0 ? -330f + i * 220f : -330f + (i - 4) * 220f + (special ? 110f : 0f);
                var size = special ? new Vector2(420f, 280f) : new Vector2(200f, 280f);
                tiles[i] = Tile(factory, card, i, new Vector2(x, -280f - row * 310f), size, special);
            }

            var info = factory.Text(card, "Info", "Rewards are added to your power-ups. Missing a day starts again from Day 1.", 28, theme.mutedText, body: true);
            info.horizontalOverflow = HorizontalWrapMode.Wrap;
            info.fontStyle = FontStyle.Normal;
            UiFactory.Place(info.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -900f), new Vector2(820f, 80f));
            var status = factory.Text(card, "Status", string.Empty, 30, theme.danger, body: true);
            status.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.Place(status.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 226f), new Vector2(820f, 90f));
            status.gameObject.SetActive(false);
            var claim = factory.Button(card, "ClaimButton", "CLAIM", theme.primary, new Vector2(680f, 160f), 56);
            UiFactory.Place((RectTransform)claim.transform, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(640f, 160f));
            UiFactory.Record(claim.transform);

            var panel = transition.gameObject.AddComponent<DailyRewardsPanel>();
            EditorRefs.Set(panel, ("transition", transition), ("closeButton", close), ("tiles", tiles), ("claimButton", claim),
                ("claimLabel", claim.transform.Find("Label").GetComponent<Text>()), ("streakText", streak), ("statusText", status));
            return panel;
        }

        public static MissionsPanel BuildMissionsPanel(UiFactory factory, Transform root, DailyChallengeCard challenge)
        {
            var theme = factory.Theme;
            var (transition, card) = factory.Modal(root, "MissionsPanel", new Vector2(960f, 1580f), "MISSIONS", theme.secondary);
            var close = CloseButton(factory, card);

            var tabButtons = new Object[3];
            var tabBackgrounds = new Object[3];
            var tabLabels = new Object[3];
            var names = new[] { "DAILY", "WEEKLY", "COMPLETED" };
            for (var i = 0; i < names.Length; i++)
            {
                var tab = factory.Button(card, names[i] + "Tab", names[i], theme.card, new Vector2(286f, 100f), 40);
                UiFactory.Place((RectTransform)tab.transform, new Vector2(0.5f, 1f), new Vector2(-296f + i * 296f, -186f), new Vector2(286f, 100f));
                UiFactory.Record(tab.transform);
                tabButtons[i] = tab;
                tabBackgrounds[i] = tab.GetComponent<Image>();
                tabLabels[i] = tab.transform.Find("Label").GetComponent<Text>();
            }

            var reset = factory.Text(card, "ResetTimer", string.Empty, 30, theme.mutedText, body: true);
            UiFactory.Place(reset.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -296f), new Vector2(860f, 44f));

            if (challenge != null)
            {
                var rect = (RectTransform)challenge.transform;
                rect.SetParent(card, false);
                UiFactory.Place(rect, new Vector2(0.5f, 1f), new Vector2(0f, -346f), new Vector2(920f, 370f));
                rect.localScale = new Vector3(0.96f, 0.96f, 1f);
                UiFactory.Record(rect);
            }

            var list = UiFactory.Place(UiFactory.Rect("List", card), new Vector2(0.5f, 1f), new Vector2(0f, -718f), new Vector2(900f, 1000f));
            var cards = new Object[6];
            for (var i = 0; i < cards.Length; i++)
            {
                cards[i] = ProgressRow(factory, list, "Mission" + i, -i * 164f);
            }

            var empty = factory.Text(list, "Empty", string.Empty, 36, theme.mutedText, body: true);
            UiFactory.Place(empty.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(820f, 80f));
            var status = factory.Text(card, "Status", string.Empty, 30, theme.danger, body: true);
            UiFactory.Place(status.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(860f, 60f));
            status.gameObject.SetActive(false);

            var panel = transition.gameObject.AddComponent<MissionsPanel>();
            EditorRefs.Set(panel, ("transition", transition), ("closeButton", close), ("tabButtons", tabButtons), ("tabBackgrounds", tabBackgrounds),
                ("tabLabels", tabLabels), ("tabSelectedColor", theme.deep), ("tabNormalColor", ArtCanvas.Hex("#F1E6D2")),
                ("tabSelectedText", theme.accent), ("tabNormalText", theme.mutedText), ("dailyChallengeCard", challenge), ("list", list),
                ("cards", cards), ("emptyText", empty), ("resetText", reset), ("statusText", status),
                ("listTopWithChallenge", -718f), ("listTopWithoutChallenge", -350f));
            return panel;
        }

        public static RanksPanel BuildRanksPanel(UiFactory factory, Transform root)
        {
            var theme = factory.Theme;
            var (transition, card) = factory.Modal(root, "RanksPanel", new Vector2(960f, 1520f), "RANKS", theme.deep);
            var close = CloseButton(factory, card);
            var best = factory.Text(card, "Best", string.Empty, 44, theme.deep);
            UiFactory.Place(best.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -192f), new Vector2(860f, 60f));
            var list = UiFactory.Place(UiFactory.Rect("List", card), new Vector2(0.5f, 1f), new Vector2(0f, -266f), new Vector2(900f, 1160f));
            var cards = new Object[7];
            for (var i = 0; i < cards.Length; i++)
            {
                cards[i] = ProgressRow(factory, list, "Rank" + i, -i * 164f);
            }

            var status = factory.Text(card, "Status", string.Empty, 30, theme.danger, body: true);
            UiFactory.Place(status.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(860f, 56f));
            status.gameObject.SetActive(false);
            var panel = transition.gameObject.AddComponent<RanksPanel>();
            EditorRefs.Set(panel, ("transition", transition), ("closeButton", close), ("cards", cards), ("bestText", best), ("statusText", status));
            return panel;
        }

        internal static Button CloseButton(UiFactory factory, RectTransform card)
        {
            var close = factory.Button(card, "CloseButton", string.Empty, factory.Theme.danger, new Vector2(112f, 112f), icon: GameArtGenerator.Icon("Close"));
            UiFactory.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(-6f, 6f), new Vector2(112f, 112f));
            UiFactory.Record(close.transform);
            return close;
        }

        private static RectTransform Ribbon(UiFactory factory, RectTransform card, Color color, float height)
        {
            var ribbon = factory.Panel(card, "Header", color).rectTransform;
            ribbon.anchorMin = new Vector2(0f, 1f);
            ribbon.anchorMax = new Vector2(1f, 1f);
            ribbon.pivot = new Vector2(0.5f, 1f);
            ribbon.offsetMin = new Vector2(16f, -height - 16f);
            ribbon.offsetMax = new Vector2(-16f, -16f);
            var strip = factory.Strip(ribbon, "Strip", 24f).rectTransform;
            strip.anchorMin = new Vector2(0f, 0f);
            strip.anchorMax = new Vector2(1f, 0f);
            strip.pivot = new Vector2(0.5f, 1f);
            strip.offsetMin = new Vector2(46f, -12f);
            strip.offsetMax = new Vector2(-46f, 12f);
            factory.Ornament(ribbon, "OrnamentLeft", new Vector2(0f, 0.5f), new Vector2(18f, 6f), 72f);
            factory.Ornament(ribbon, "OrnamentRight", new Vector2(1f, 0.5f), new Vector2(-18f, 6f), 72f);
            return ribbon;
        }

        private static DailyRewardTile Tile(UiFactory factory, RectTransform card, int index, Vector2 position, Vector2 size, bool special)
        {
            var theme = factory.Theme;
            var root = UiFactory.Place(UiFactory.Rect("Day" + (index + 1), card), new Vector2(0.5f, 1f), position, size);
            root.pivot = new Vector2(0.5f, 1f);
            root.anchoredPosition = position;
            var glow = factory.Image(root, "TodayGlow", theme.glow, theme.accent);
            UiFactory.Stretch(glow.rectTransform, -40f, -40f, -40f, -40f);
            var background = factory.Image(root, "Background", theme.panel, Color.white);
            UiFactory.Stretch(background.rectTransform);
            GameplaySceneBuilder.AddShadow(background.gameObject, theme, 0.2f, 8f);
            var day = factory.Text(root, "Day", $"DAY {index + 1}", 34, theme.deep);
            UiFactory.Place(day.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(size.x, 48f));
            var icon = factory.Image(root, "Icon", GameArtGenerator.Icon("Gift"), Color.white);
            icon.preserveAspect = true;
            var iconBack = factory.Image(root, "IconDisc", theme.disc, special ? theme.primary : theme.secondary);
            UiFactory.Place(iconBack.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(124f, 124f));
            icon.transform.SetParent(iconBack.transform, false);
            UiFactory.Stretch(icon.rectTransform, 26f, 26f, 26f, 26f);
            var amount = factory.Text(root, "Amount", string.Empty, 36, theme.textDark);
            UiFactory.Place(amount.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(size.x, 50f));
            if (special)
            {
                factory.Ornament(root, "Ornament", new Vector2(0f, 0.5f), new Vector2(64f, 6f), 90f, 0.8f);
                factory.Ornament(root, "Ornament2", new Vector2(1f, 0.5f), new Vector2(-64f, 6f), 90f, 0.8f);
            }

            var mark = factory.Image(root, "ClaimedMark", theme.disc, theme.secondary);
            UiFactory.Place(mark.rectTransform, new Vector2(1f, 1f), new Vector2(-6f, -6f), new Vector2(70f, 70f));
            var check = factory.Image(mark.transform, "Check", GameArtGenerator.Icon("Check"), Color.white);
            UiFactory.Stretch(check.rectTransform, 14f, 14f, 14f, 14f);
            var punch = UiFactory.Punch(root.gameObject, 0.12f, 0.3f, true);
            var tile = root.gameObject.AddComponent<DailyRewardTile>();
            EditorRefs.Set(tile, ("background", background), ("dayText", day), ("icon", icon), ("amountText", amount), ("claimedMark", mark.gameObject),
                ("todayGlow", glow.gameObject), ("punch", punch), ("claimedColor", ArtCanvas.Hex("#E8F4EF")),
                ("availableColor", ArtCanvas.Hex("#FFF1C9")), ("upcomingColor", Color.white));
            return tile;
        }

        private static ProgressCard ProgressRow(UiFactory factory, RectTransform list, string name, float y)
        {
            var theme = factory.Theme;
            var root = UiFactory.Rect(name, list);
            root.anchorMin = new Vector2(0.5f, 1f);
            root.anchorMax = new Vector2(0.5f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            root.sizeDelta = new Vector2(900f, 150f);
            root.anchoredPosition = new Vector2(0f, y);
            var background = factory.Image(root, "Background", theme.panel, Color.white);
            UiFactory.Stretch(background.rectTransform);
            GameplaySceneBuilder.AddShadow(background.gameObject, theme, 0.15f, 6f);

            var title = factory.Text(root, "Title", string.Empty, 40, theme.deep, TextAnchor.MiddleLeft);
            UiFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(30f, -14f), new Vector2(500f, 50f));
            var description = factory.Text(root, "Description", string.Empty, 27, theme.mutedText, TextAnchor.MiddleLeft, body: true);
            description.fontStyle = FontStyle.Normal;
            UiFactory.Place(description.rectTransform, new Vector2(0f, 1f), new Vector2(30f, -64f), new Vector2(500f, 36f));
            var track = factory.Image(root, "Track", theme.pill, new Color(theme.deep.r, theme.deep.g, theme.deep.b, 0.12f));
            UiFactory.Place(track.rectTransform, new Vector2(0f, 0f), new Vector2(30f, 22f), new Vector2(360f, 26f));
            var fill = factory.Image(track.transform, "Fill", theme.pill, theme.accent);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            UiFactory.Stretch(fill.rectTransform);
            var progress = factory.Text(root, "Progress", string.Empty, 28, theme.textDark, TextAnchor.MiddleLeft);
            UiFactory.Place(progress.rectTransform, new Vector2(0f, 0f), new Vector2(404f, 15f), new Vector2(150f, 40f));

            var rewardIcon = factory.Image(root, "RewardIcon", null, theme.primary);
            rewardIcon.preserveAspect = true;
            UiFactory.Place(rewardIcon.rectTransform, new Vector2(1f, 0.5f), new Vector2(-250f, 18f), new Vector2(60f, 60f));
            var rewardText = factory.Text(root, "Reward", string.Empty, 24, theme.deep);
            UiFactory.Place(rewardText.rectTransform, new Vector2(1f, 0.5f), new Vector2(-195f, -34f), new Vector2(170f, 34f));

            var claim = factory.Button(root, "ClaimButton", "CLAIM", theme.primary, new Vector2(176f, 100f), 36);
            UiFactory.Place((RectTransform)claim.transform, new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(176f, 100f));
            UiFactory.Record(claim.transform);
            var done = factory.Image(root, "DoneMark", theme.disc, theme.secondary);
            UiFactory.Place(done.rectTransform, new Vector2(1f, 0.5f), new Vector2(-66f, 0f), new Vector2(84f, 84f));
            var check = factory.Image(done.transform, "Check", GameArtGenerator.Icon("Check"), Color.white);
            UiFactory.Stretch(check.rectTransform, 16f, 16f, 16f, 16f);

            var card = root.gameObject.AddComponent<ProgressCard>();
            EditorRefs.Set(card, ("titleText", title), ("descriptionText", description), ("progressFill", fill), ("progressText", progress),
                ("rewardIcon", rewardIcon), ("rewardText", rewardText), ("claimButton", claim), ("doneMark", done.gameObject), ("background", background),
                ("normalColor", Color.white), ("claimableColor", ArtCanvas.Hex("#FFF1C9")), ("doneColor", ArtCanvas.Hex("#E8F4EF")));
            return card;
        }
    }
}
