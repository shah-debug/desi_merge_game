using DesiMerge.Configuration;
using DesiMerge.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.Editor.Content
{
    /// <summary>
    /// MainMenu.unity: truck-art background, title plate, the merge chain (a real preview of the game's items),
    /// Play and Settings. No buttons for features that don't exist yet (collection, shop).
    /// </summary>
    internal static class MainMenuSceneBuilder
    {
        public static void Populate()
        {
            var factory = new UiFactory(ContentSetup.LoadTheme());
            var theme = factory.Theme;
            var gameplayConfig = AssetDatabase.LoadAssetAtPath<GameplayConfig>(GameplaySetup.GameplayConfigPath);

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
            UiFactory.Place(plate, new Vector2(0.5f, 0.5f), new Vector2(0f, 560f), new Vector2(900f, 340f));
            var title = factory.Text(plate, "Title", "<color=#FFB300>Desi</color> Merge", theme.titleSize, theme.textLight, TextAnchor.MiddleCenter, outline: true);
            UiFactory.Stretch(title.rectTransform, 20f, 20f, 30f, 90f);
            var tagline = factory.Text(plate, "Tagline", "Milao  •  Barhao  •  Jeeto", theme.bodySize + 4, theme.accent);
            UiFactory.Place(tagline.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(820f, 60f));
            AddFlower(factory, plate, new Vector2(0f, 1f), new Vector2(-30f, 30f), theme.primary);
            AddFlower(factory, plate, new Vector2(1f, 1f), new Vector2(30f, 30f), theme.secondary);
            AddFlower(factory, plate, new Vector2(0f, 0f), new Vector2(-30f, -30f), theme.secondary);
            AddFlower(factory, plate, new Vector2(1f, 0f), new Vector2(30f, -30f), theme.primary);
            UiFactory.Entrance(plate.gameObject, 0f, new Vector2(0f, 220f));

            var chain = BuildMergeChain(factory, content, gameplayConfig != null ? gameplayConfig.Progression : null);
            UiFactory.Entrance(chain.gameObject, 0.12f, new Vector2(0f, -60f));

            var play = factory.Button(content, "PlayButton", "PLAY", theme.primary, new Vector2(640f, 210f), 100);
            UiFactory.Place((RectTransform)play.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -260f), new Vector2(640f, 210f));
            UiFactory.Record(play.transform);
            UiFactory.Entrance(play.gameObject, 0.25f, new Vector2(0f, -80f));

            var settings = factory.Button(content, "SettingsButton", "SETTINGS", theme.secondary, new Vector2(520f, 150f), 56);
            UiFactory.Place((RectTransform)settings.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -480f), new Vector2(520f, 150f));
            UiFactory.Record(settings.transform);
            UiFactory.Entrance(settings.gameObject, 0.35f, new Vector2(0f, -80f));

            var best = factory.Text(content, "BestScore", string.Empty, theme.bodySize + 8, theme.textDark);
            UiFactory.Place(best.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -640f), new Vector2(800f, 70f));
            UiFactory.Entrance(best.gameObject, 0.45f, new Vector2(0f, -40f));

            var settingsPanel = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ContentPaths.SettingsPanelPrefabPath), root);
            UiFactory.Stretch((RectTransform)settingsPanel.transform);
            UiFactory.Record(settingsPanel.transform);

            UiFactory.EventSystem();

            var entry = new GameObject("MainMenuEntryPoint").AddComponent<MainMenuEntryPoint>();
            EditorRefs.Set(entry,
                ("playButton", play), ("settingsButton", settings), ("bestScoreText", best),
                ("settingsPanel", settingsPanel.GetComponent<SettingsPanel>()),
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

        /// <summary>The ten merge levels in two rows, read from the real progression asset.</summary>
        private static RectTransform BuildMergeChain(UiFactory factory, Transform parent, MergeProgression progression)
        {
            var theme = factory.Theme;
            var chain = UiFactory.Place(UiFactory.Rect("MergeChain", parent), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1000f, 380f));
            var heading = factory.Text(chain, "Heading", "Merge chain", theme.bodySize, theme.textDark);
            UiFactory.Place(heading.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(800f, 56f));
            if (progression == null)
            {
                return chain;
            }

            for (var level = 0; level < progression.Count; level++)
            {
                var definition = progression.Get(level);
                var column = level % 5;
                var row = level / 5;
                var position = new Vector2(-400f + column * 200f, 50f - row * 160f);
                var size = Mathf.Lerp(70f, 120f, level / (float)Mathf.Max(1, progression.Count - 1));
                var icon = factory.Image(chain, $"Item{level + 1:00}", definition.Sprite, definition.Tint);
                UiFactory.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), position + new Vector2(0f, 10f), new Vector2(size, size));
                var label = factory.Text(chain, $"Label{level + 1:00}", definition.DisplayName, 26, theme.textDark);
                UiFactory.Place(label.rectTransform, new Vector2(0.5f, 0.5f), position + new Vector2(0f, -66f), new Vector2(190f, 36f));
            }

            return chain;
        }
    }
}
