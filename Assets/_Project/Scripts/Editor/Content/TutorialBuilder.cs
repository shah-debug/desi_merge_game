using DesiMerge.Configuration;
using DesiMerge.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.Editor.Content
{
    internal static class TutorialBuilder
    {
        public static TutorialOverlay Build(UiFactory factory, Transform root)
        {
            var theme = factory.Theme;
            var overlay = UiFactory.Stretch(UiFactory.Rect("TutorialOverlay", root));
            overlay.gameObject.AddComponent<CanvasGroup>();
            UiFactory.Stretch(factory.Image(overlay, "Dim", null, new Color(theme.overlay.r, theme.overlay.g, theme.overlay.b, 0.55f), raycast: true).rectTransform);

            var card = factory.Panel(overlay, "Card", theme.card, theme.accent, 10f).rectTransform;
            UiFactory.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(900f, 1040f));
            GameplaySceneBuilder.AddShadow(card.Find("Rim").gameObject, theme, 0.3f, 16f);
            var ribbon = factory.Panel(card, "Header", theme.secondary).rectTransform;
            ribbon.anchorMin = new Vector2(0f, 1f);
            ribbon.anchorMax = new Vector2(1f, 1f);
            ribbon.pivot = new Vector2(0.5f, 1f);
            ribbon.offsetMin = new Vector2(16f, -136f);
            ribbon.offsetMax = new Vector2(-16f, -16f);
            var heading = factory.Text(ribbon, "Title", "HOW TO PLAY", theme.headingSize - 8, theme.textLight, outline: true);
            UiFactory.Stretch(heading.rectTransform, 80f, 80f, 0f, 8f);
            factory.Ornament(ribbon, "OrnamentLeft", new Vector2(0f, 0.5f), new Vector2(18f, 4f), 70f);
            factory.Ornament(ribbon, "OrnamentRight", new Vector2(1f, 0.5f), new Vector2(-18f, 4f), 70f);

            var progression = AssetDatabase.LoadAssetAtPath<MergeProgression>(ContentPaths.MergeProgressionPath);
            Sprite Item(int level) => progression != null && level < progression.Count ? progression.Get(level)?.Sprite : null;

            var pages = new GameObject[4];
            pages[0] = Page(factory, card, "DropPage", "AIM AND DROP", "Drag left or right to aim, then let go to drop.");
            ItemImage(factory, pages[0].transform, Item(0), new Vector2(0f, 120f), 190f);
            Arrow(factory, pages[0].transform, new Vector2(0f, -40f), theme.primary);

            pages[1] = Page(factory, card, "MergePage", "MATCH TO MERGE", "Two of the same merge into the next item and score points.");
            ItemImage(factory, pages[1].transform, Item(0), new Vector2(-280f, 60f), 150f);
            Sign(factory, pages[1].transform, "+", new Vector2(-150f, 60f));
            ItemImage(factory, pages[1].transform, Item(0), new Vector2(-20f, 60f), 150f);
            Sign(factory, pages[1].transform, "=", new Vector2(120f, 60f));
            ItemImage(factory, pages[1].transform, Item(1), new Vector2(270f, 60f), 200f);

            pages[2] = Page(factory, card, "DangerPage", "WATCH THE LINE", "Keep items below the dashed line. If they stay above it too long, the game ends.");
            var line = factory.Image(pages[2].transform, "DangerLine", GameArtGenerator.Load(GameArtGenerator.DangerDashPath), theme.danger);
            line.type = Image.Type.Tiled;
            UiFactory.Place(line.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(680f, 22f));
            ItemImage(factory, pages[2].transform, Item(4), new Vector2(-150f, 40f), 190f);
            ItemImage(factory, pages[2].transform, Item(2), new Vector2(60f, 10f), 130f);
            ItemImage(factory, pages[2].transform, Item(3), new Vector2(210f, 30f), 160f);

            pages[3] = Page(factory, card, "PowerUpPage", "POWER-UPS", "Hammer smashes one item. Shuffle mixes the board. You get free uses every game.");
            PowerUpBadge(factory, pages[3].transform, "Hammer", theme.primary, new Vector2(-150f, 60f));
            PowerUpBadge(factory, pages[3].transform, "Shuffle", theme.secondary, new Vector2(150f, 60f));

            var dots = new Object[pages.Length];
            for (var i = 0; i < pages.Length; i++)
            {
                var dot = factory.Image(card, "Dot" + i, theme.disc, theme.accent);
                UiFactory.Place(dot.rectTransform, new Vector2(0.5f, 0f), new Vector2((i - (pages.Length - 1) * 0.5f) * 44f, 214f), new Vector2(24f, 24f));
                dots[i] = dot;
            }

            var next = factory.Button(card, "NextButton", "NEXT", theme.primary, new Vector2(520f, 150f), 58, GameArtGenerator.Icon("Play"));
            UiFactory.Place((RectTransform)next.transform, new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(520f, 150f));
            UiFactory.Record(next.transform);
            var skip = factory.Button(overlay, "SkipButton", "SKIP", theme.deep, new Vector2(220f, 100f), 40);
            UiFactory.Place((RectTransform)skip.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -720f), new Vector2(240f, 100f));
            UiFactory.Record(skip.transform);

            var transition = overlay.gameObject.AddComponent<DesiMerge.UI.Animation.PanelTransition>();
            EditorRefs.Set(transition, ("card", card), ("duration", theme.panelDuration));
            var tutorial = overlay.gameObject.AddComponent<TutorialOverlay>();
            EditorRefs.Set(tutorial,
                ("transition", transition), ("pages", pages), ("dots", dots), ("dotOn", theme.primary),
                ("dotOff", new Color(theme.deep.r, theme.deep.g, theme.deep.b, 0.2f)),
                ("nextButton", next), ("nextLabel", next.transform.Find("Label").GetComponent<Text>()), ("skipButton", skip));
            return tutorial;
        }

        private static GameObject Page(UiFactory factory, Transform card, string name, string title, string body)
        {
            var theme = factory.Theme;
            var page = UiFactory.Stretch(UiFactory.Rect(name, card), 0f, 0f, 150f, 250f);
            var heading = factory.Text(page, "Heading", title, 64, theme.deep);
            UiFactory.Place(heading.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(800f, 80f));
            var text = factory.Text(page, "Body", body, 38, theme.mutedText, body: true);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.fontStyle = FontStyle.Normal;
            UiFactory.Place(text.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(780f, 150f));
            return page.gameObject;
        }

        private static void ItemImage(UiFactory factory, Transform parent, Sprite sprite, Vector2 position, float size)
        {
            if (sprite == null)
            {
                return;
            }

            var image = factory.Image(parent, "Item", sprite, Color.white);
            image.preserveAspect = true;
            UiFactory.Place(image.rectTransform, new Vector2(0.5f, 0.5f), position, new Vector2(size, size));
        }

        private static void Sign(UiFactory factory, Transform parent, string sign, Vector2 position)
        {
            var text = factory.Text(parent, "Sign", sign, 90, factory.Theme.deep);
            UiFactory.Place(text.rectTransform, new Vector2(0.5f, 0.5f), position, new Vector2(100f, 110f));
        }

        private static void Arrow(UiFactory factory, Transform parent, Vector2 position, Color color)
        {
            var arrow = factory.Image(parent, "Arrow", GameArtGenerator.Icon("Play"), color);
            UiFactory.Place(arrow.rectTransform, new Vector2(0.5f, 0.5f), position, new Vector2(90f, 90f));
            arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -90f);
        }

        private static void PowerUpBadge(UiFactory factory, Transform parent, string icon, Color color, Vector2 position)
        {
            var disc = factory.Image(parent, icon + "Badge", factory.Theme.disc, color);
            UiFactory.Place(disc.rectTransform, new Vector2(0.5f, 0.5f), position, new Vector2(200f, 200f));
            var image = factory.Image(disc.transform, "Icon", GameArtGenerator.Icon(icon), Color.white);
            UiFactory.Stretch(image.rectTransform, 46f, 46f, 46f, 46f);
            var caption = factory.Text(parent, icon + "Caption", icon.ToUpperInvariant(), 40, factory.Theme.deep);
            UiFactory.Place(caption.rectTransform, new Vector2(0.5f, 0.5f), position + new Vector2(0f, -136f), new Vector2(260f, 56f));
        }
    }
}
