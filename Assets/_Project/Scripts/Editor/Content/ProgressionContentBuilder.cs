using DesiMerge.Progression;
using DesiMerge.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.Editor.Content
{
    /// <summary>
    /// Phase 4 content: default achievement and daily-challenge catalogs (data only, stable IDs) and the
    /// Collection / Achievements panel prefabs. Existing assets are kept so tuned catalogs survive re-running.
    /// Shipped rewards are None: the game design doesn't define an economy yet.
    /// </summary>
    internal static class ProgressionContentBuilder
    {
        public static void EnsureCatalogs()
        {
            ContentPaths.EnsureFolder(ContentPaths.ProgressionFolder);

            if (AssetDatabase.LoadAssetAtPath<AchievementCatalog>(ContentPaths.AchievementsPath) == null)
            {
                var catalog = ScriptableObject.CreateInstance<AchievementCatalog>();
                catalog.Set(new[]
                {
                    Achievement("first_game", "First Game", "Finish your first game", AchievementType.CompleteGames, 1),
                    Achievement("games_10", "Regular", "Finish 10 games", AchievementType.CompleteGames, 10),
                    Achievement("merges_50", "Mixer", "Merge 50 times", AchievementType.TotalMerges, 50),
                    Achievement("merges_500", "Merge Ustaad", "Merge 500 times", AchievementType.TotalMerges, 500),
                    Achievement("score_500", "High Scorer", "Reach a best score of 500", AchievementType.BestScore, 500),
                    Achievement("discover_chai", "Chai Time", "Discover Chai", AchievementType.DiscoverItem, 1, "chai"),
                    Achievement("discover_bus", "All Aboard!", "Discover the Bus", AchievementType.DiscoverItem, 1, "bus"),
                    Achievement("streak_3", "Three Days Running", "Finish a game on 3 days in a row", AchievementType.DayStreak, 3),
                });
                AssetDatabase.CreateAsset(catalog, ContentPaths.AchievementsPath);
            }

            if (AssetDatabase.LoadAssetAtPath<DailyChallengeCatalog>(ContentPaths.DailyChallengesPath) == null)
            {
                var catalog = ScriptableObject.CreateInstance<DailyChallengeCatalog>();
                catalog.Set(new[]
                {
                    Daily("daily_merges_20", "Merge Mela", "Merge 20 times today", DailyChallengeType.MergesInDay, 20),
                    Daily("daily_score_300", "Score Chase", "Score 300 in one game", DailyChallengeType.ScoreInOneGame, 300),
                    Daily("daily_create_aam", "Aam Season", "Create an Aam (or better)", DailyChallengeType.CreateItem, 1, "aam"),
                    Daily("daily_games_3", "Three Rounds", "Finish 3 games today", DailyChallengeType.CompleteGames, 3),
                    Daily("daily_merges_40", "Busy Bazaar", "Merge 40 times today", DailyChallengeType.MergesInDay, 40),
                    Daily("daily_score_600", "Big Score", "Score 600 in one game", DailyChallengeType.ScoreInOneGame, 600),
                    Daily("daily_create_chai", "Chai Break", "Create a Chai (or better)", DailyChallengeType.CreateItem, 1, "chai"),
                });
                AssetDatabase.CreateAsset(catalog, ContentPaths.DailyChallengesPath);
            }
        }

        public static void EnsureCollectionPanelPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ContentPaths.CollectionPanelPrefabPath) != null)
            {
                return;
            }

            var factory = new UiFactory(ContentSetup.LoadTheme());
            var theme = factory.Theme;
            var host = UiFactory.Rect("Host", null);
            host.sizeDelta = new Vector2(1080f, 1920f);
            var (transition, card) = factory.Modal(host, "CollectionPanel", new Vector2(960f, 980f), theme.deep);

            var title = factory.Text(card, "Title", "Collection", theme.headingSize, theme.accent, TextAnchor.MiddleCenter, outline: true);
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(800f, 110f));
            var counter = factory.Text(card, "Counter", "0 / 0 discovered", theme.bodySize, theme.textLight);
            UiFactory.Place(counter.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(800f, 56f));

            var grid = UiFactory.Place(UiFactory.Rect("Grid", card), new Vector2(0.5f, 1f), new Vector2(0f, -230f), new Vector2(910f, 540f));
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(170f, 250f);
            layout.spacing = new Vector2(10f, 24f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 5;
            layout.childAlignment = TextAnchor.UpperCenter;

            var slotRoot = UiFactory.Rect("SlotTemplate", grid);
            slotRoot.sizeDelta = layout.cellSize;
            var slotBackground = factory.Image(slotRoot, "Background", theme.roundedPanel, new Color(1f, 1f, 1f, 0.1f));
            UiFactory.Stretch(slotBackground.rectTransform);
            var icon = factory.Image(slotRoot, "Icon", theme.circle, Color.white);
            UiFactory.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(130f, 130f));
            icon.preserveAspect = true;
            var lockedMark = factory.Text(slotRoot, "LockedMark", "?", 72, theme.textLight, TextAnchor.MiddleCenter, outline: true);
            UiFactory.Place(lockedMark.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(130f, 90f));
            var label = factory.Text(slotRoot, "Label", "???", 28, theme.textLight);
            UiFactory.Place(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(170f, 60f));
            var slot = slotRoot.gameObject.AddComponent<CollectionSlot>();
            EditorRefs.Set(slot, ("icon", icon), ("label", label), ("lockedMark", lockedMark.gameObject));

            var empty = factory.Text(card, "EmptyText", "No items configured", theme.bodySize, theme.textLight);
            UiFactory.Place(empty.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 80f));
            empty.gameObject.SetActive(false);

            var close = factory.Button(card, "CloseButton", "DONE", theme.primary, new Vector2(480f, 150f));
            UiFactory.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(480f, 150f));
            UiFactory.Record(close.transform);

            var panel = transition.gameObject.AddComponent<CollectionPanel>();
            EditorRefs.Set(panel,
                ("transition", transition), ("grid", grid), ("slotTemplate", slot), ("counterText", counter),
                ("emptyText", empty), ("closeButton", close), ("placeholderSprite", theme.circle));

            PrefabUtility.SaveAsPrefabAsset(transition.gameObject, ContentPaths.CollectionPanelPrefabPath);
            Object.DestroyImmediate(host.gameObject);
        }

        public static void EnsureAchievementsPanelPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ContentPaths.AchievementsPanelPrefabPath) != null)
            {
                return;
            }

            var factory = new UiFactory(ContentSetup.LoadTheme());
            var theme = factory.Theme;
            var host = UiFactory.Rect("Host", null);
            host.sizeDelta = new Vector2(1080f, 1920f);
            var (transition, card) = factory.Modal(host, "AchievementsPanel", new Vector2(960f, 1280f), theme.deep);

            var title = factory.Text(card, "Title", "Achievements", theme.headingSize, theme.accent, TextAnchor.MiddleCenter, outline: true);
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(860f, 110f));

            var statsLabels = factory.Text(card, "StatsLabels", "Best score\nGames completed\nTotal merges\nHighest item\nDay streak", 34, theme.textLight, TextAnchor.UpperLeft);
            UiFactory.Place(statsLabels.rectTransform, new Vector2(0.5f, 1f), new Vector2(-200f, -160f), new Vector2(400f, 240f));
            statsLabels.lineSpacing = 1.1f;
            var statsValues = factory.Text(card, "StatsValues", string.Empty, 34, theme.accent, TextAnchor.UpperRight);
            UiFactory.Place(statsValues.rectTransform, new Vector2(0.5f, 1f), new Vector2(200f, -160f), new Vector2(400f, 240f));
            statsValues.lineSpacing = 1.1f;

            var summary = factory.Text(card, "Summary", string.Empty, 34, theme.textLight);
            UiFactory.Place(summary.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -415f), new Vector2(860f, 50f));

            var list = UiFactory.Place(UiFactory.Rect("List", card), new Vector2(0.5f, 1f), new Vector2(0f, -475f), new Vector2(880f, 620f));
            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var rowRoot = UiFactory.Rect("RowTemplate", list);
            rowRoot.sizeDelta = new Vector2(880f, 70f);
            var background = factory.Image(rowRoot, "Background", theme.roundedPanel, new Color(0f, 0f, 0f, 0.25f));
            UiFactory.Stretch(background.rectTransform);
            var rowTitle = factory.Text(rowRoot, "Title", "Title", 30, theme.textLight, TextAnchor.UpperLeft);
            UiFactory.Place(rowTitle.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -6f), new Vector2(560f, 36f));
            var rowDescription = factory.Text(rowRoot, "Description", "Description", 22, new Color(1f, 1f, 1f, 0.75f), TextAnchor.UpperLeft);
            rowDescription.fontStyle = FontStyle.Normal;
            UiFactory.Place(rowDescription.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -40f), new Vector2(600f, 28f));
            var rowProgress = factory.Text(rowRoot, "Progress", "0 / 1", 28, theme.accent, TextAnchor.MiddleRight);
            UiFactory.Place(rowProgress.rectTransform, new Vector2(1f, 0.5f), new Vector2(-80f, 6f), new Vector2(220f, 40f));
            var fill = factory.Image(rowRoot, "ProgressFill", theme.roundedPanel, theme.accent);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            // Under the progress number (right side), clear of the description text.
            UiFactory.Place(fill.rectTransform, new Vector2(1f, 0f), new Vector2(-80f, 8f), new Vector2(220f, 6f));
            var mark = factory.Image(rowRoot, "UnlockedMark", theme.flower, theme.accent);
            UiFactory.Place(mark.rectTransform, new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(54f, 54f));
            var row = rowRoot.gameObject.AddComponent<AchievementRow>();
            EditorRefs.Set(row,
                ("title", rowTitle), ("description", rowDescription), ("progress", rowProgress), ("progressFill", fill),
                ("unlockedMark", mark.gameObject), ("background", background));

            var empty = factory.Text(card, "EmptyText", "No achievements configured", theme.bodySize, theme.textLight);
            UiFactory.Place(empty.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 80f));
            empty.gameObject.SetActive(false);

            var close = factory.Button(card, "CloseButton", "DONE", theme.primary, new Vector2(480f, 140f));
            UiFactory.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(480f, 140f));
            UiFactory.Record(close.transform);

            var panel = transition.gameObject.AddComponent<AchievementsPanel>();
            EditorRefs.Set(panel,
                ("transition", transition), ("statsValues", statsValues), ("summaryText", summary), ("list", list),
                ("rowTemplate", row), ("emptyText", empty), ("closeButton", close));

            PrefabUtility.SaveAsPrefabAsset(transition.gameObject, ContentPaths.AchievementsPanelPrefabPath);
            Object.DestroyImmediate(host.gameObject);
        }

        private static AchievementDefinition Achievement(string id, string title, string description, AchievementType type, long target, string itemId = "")
        {
            return new AchievementDefinition { id = id, title = title, description = description, type = type, target = target, itemId = itemId };
        }

        private static DailyChallengeDefinition Daily(string id, string title, string description, DailyChallengeType type, long target, string itemId = "")
        {
            return new DailyChallengeDefinition { id = id, title = title, description = description, type = type, target = target, itemId = itemId };
        }
    }
}
