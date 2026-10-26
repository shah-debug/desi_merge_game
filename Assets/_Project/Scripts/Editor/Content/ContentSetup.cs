using DesiMerge.Audio;
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
    /// One entry point for all generated content: foundation assets, gameplay data, placeholder UI art and audio,
    /// UI theme, audio library, UI prefabs and the MainMenu/Gameplay scenes, plus GameConfig and build order.
    /// "Create All Content" only creates what is missing; "Rebuild" regenerates the scenes and prefabs.
    /// Batch mode: -executeMethod DesiMerge.Editor.Content.ContentSetup.CreateAllContent
    /// </summary>
    public static class ContentSetup
    {
        private const string MainMenuSceneName = "MainMenu";

        [MenuItem("Desi Merge/Setup/Create All Content")]
        public static void CreateAllContent()
        {
            FoundationSetup.CreateFoundationAssets();
            GameplaySetup.EnsureGameplayData();
            UiArtGenerator.EnsureAll();
            AudioSynth.EnsureAll();
            EnsureTheme();
            EnsureAudioLibrary();
            EnsureSparkleMaterial();
            EnsureButtonPrefab();
            EnsureSettingsPanelPrefab();
            ConfigureGameConfig();

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ContentPaths.MainMenuScenePath) == null)
            {
                EditorSceneUtility.CreateScene(ContentPaths.MainMenuScenePath, MainMenuSceneBuilder.Populate);
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ContentPaths.GameplayScenePath) == null)
            {
                EditorSceneUtility.CreateScene(ContentPaths.GameplayScenePath, GameplaySceneBuilder.Populate);
            }

            EditorSceneUtility.SetLeadingBuildScenes(FoundationSetup.BootstrapScenePath, ContentPaths.MainMenuScenePath, ContentPaths.GameplayScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] All content is in place.");
        }

        [MenuItem("Desi Merge/Setup/Rebuild Generated Scenes and Prefabs (overwrites)")]
        public static void RebuildGeneratedScenesAndPrefabs()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Rebuild generated content?",
                    "This regenerates MainMenu.unity, Gameplay.unity, the UI prefabs and the MergeItem prefab. " +
                    "Manual edits to those files are lost. Data assets (items, configs, art, audio) are kept.", "Rebuild", "Cancel"))
            {
                return;
            }

            AssetDatabase.DeleteAsset(ContentPaths.MainMenuScenePath);
            AssetDatabase.DeleteAsset(ContentPaths.GameplayScenePath);
            AssetDatabase.DeleteAsset(ContentPaths.SettingsPanelPrefabPath);
            AssetDatabase.DeleteAsset(ContentPaths.ButtonPrefabPath);
            GameplaySetup.EnsureItemPrefab(overwrite: true);
            CreateAllContent();
        }

        internal static UITheme LoadTheme() => AssetDatabase.LoadAssetAtPath<UITheme>(ContentPaths.ThemePath);

        private static void EnsureTheme()
        {
            if (LoadTheme() != null)
            {
                return;
            }

            ContentPaths.EnsureFolder("Assets/_Project/Settings/UI");
            var theme = ScriptableObject.CreateInstance<UITheme>();
            theme.roundedPanel = AssetDatabase.LoadAssetAtPath<Sprite>(UiArtGenerator.RoundedPanelPath);
            theme.paintedFrame = AssetDatabase.LoadAssetAtPath<Sprite>(UiArtGenerator.PaintedFramePath);
            theme.pattern = AssetDatabase.LoadAssetAtPath<Sprite>(UiArtGenerator.PatternPath);
            theme.flower = AssetDatabase.LoadAssetAtPath<Sprite>(UiArtGenerator.FlowerPath);
            theme.circle = AssetDatabase.LoadAssetAtPath<Sprite>(GameplaySetup.CircleSpritePath);
            theme.pauseIcon = AssetDatabase.LoadAssetAtPath<Sprite>(UiArtGenerator.PauseIconPath);
            theme.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            AssetDatabase.CreateAsset(theme, ContentPaths.ThemePath);
        }

        private static void EnsureAudioLibrary()
        {
            if (AssetDatabase.LoadAssetAtPath<AudioLibrary>(ContentPaths.AudioLibraryPath) != null)
            {
                return;
            }

            AudioClip Clip(string path) => AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            var library = ScriptableObject.CreateInstance<AudioLibrary>();
            library.Set(
                new[]
                {
                    new AudioLibrary.SfxEntry { cue = SfxCue.Drop, clip = Clip(AudioSynth.DropPath), volume = 0.8f },
                    new AudioLibrary.SfxEntry { cue = SfxCue.Merge, clip = Clip(AudioSynth.MergePath), volume = 0.9f },
                    new AudioLibrary.SfxEntry { cue = SfxCue.ButtonTap, clip = Clip(AudioSynth.ButtonPath), volume = 0.6f },
                    new AudioLibrary.SfxEntry { cue = SfxCue.GameOver, clip = Clip(AudioSynth.GameOverPath), volume = 1f },
                },
                new[]
                {
                    new AudioLibrary.MusicEntry { track = MusicTrack.Menu, clip = Clip(AudioSynth.MenuMusicPath), volume = 0.55f },
                    new AudioLibrary.MusicEntry { track = MusicTrack.Gameplay, clip = Clip(AudioSynth.GameplayMusicPath), volume = 0.45f },
                });
            AssetDatabase.CreateAsset(library, ContentPaths.AudioLibraryPath);
        }

        private static void EnsureSparkleMaterial()
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(ContentPaths.SparkleMaterialPath) != null)
            {
                return;
            }

            Shader shader = null;
            foreach (var candidate in new[] { "Universal Render Pipeline/2D/Sprite-Unlit-Default", "Universal Render Pipeline/Particles/Unlit", "Sprites/Default" })
            {
                shader = Shader.Find(candidate);
                if (shader != null)
                {
                    break;
                }
            }

            if (shader == null)
            {
                Debug.LogWarning("[Setup] No suitable shader for sparkles; merges will play without particles.");
                return;
            }

            ContentPaths.EnsureFolder("Assets/_Project/Art/Materials");
            var material = new Material(shader) { mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(UiArtGenerator.SparklePath) };
            AssetDatabase.CreateAsset(material, ContentPaths.SparkleMaterialPath);
        }

        private static void ConfigureGameConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(FoundationSetup.GameConfigPath);
            var serialized = new SerializedObject(config);
            var firstScene = serialized.FindProperty("firstSceneName");
            // Phase 2 pointed straight at Gameplay; now the game opens on the menu. A custom value is respected.
            if (string.IsNullOrEmpty(firstScene.stringValue) || firstScene.stringValue == "Gameplay")
            {
                firstScene.stringValue = MainMenuSceneName;
            }

            var library = serialized.FindProperty("audioLibrary");
            if (library.objectReferenceValue == null)
            {
                library.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioLibrary>(ContentPaths.AudioLibraryPath);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        private static void EnsureButtonPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ContentPaths.ButtonPrefabPath) != null)
            {
                return;
            }

            ContentPaths.EnsureFolder(ContentPaths.UiPrefabsFolder);
            var factory = new UiFactory(LoadTheme());
            var theme = factory.Theme;
            var root = UiFactory.Rect("ThemedButton", null);
            root.sizeDelta = new Vector2(600f, 160f);
            var image = root.gameObject.AddComponent<Image>();
            image.sprite = theme.roundedPanel;
            image.type = Image.Type.Sliced;
            image.color = theme.primary;
            var shadow = root.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            shadow.effectDistance = new Vector2(0f, -10f);

            var frame = factory.Image(root, "Frame", theme.paintedFrame, theme.accent);
            UiFactory.Stretch(frame.rectTransform);
            var label = factory.Text(root, "Label", "BUTTON", theme.buttonSize, theme.textLight, TextAnchor.MiddleCenter, outline: true);
            UiFactory.Stretch(label.rectTransform, 20f, 20f, 10f, 10f);

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.7f);
            button.colors = colors;
            root.gameObject.AddComponent<ButtonFeedback>();

            PrefabUtility.SaveAsPrefabAsset(root.gameObject, ContentPaths.ButtonPrefabPath);
            Object.DestroyImmediate(root.gameObject);
        }

        private static void EnsureSettingsPanelPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ContentPaths.SettingsPanelPrefabPath) != null)
            {
                return;
            }

            var factory = new UiFactory(LoadTheme());
            var theme = factory.Theme;
            var host = UiFactory.Rect("Host", null);
            host.sizeDelta = new Vector2(1080f, 1920f);
            var (transition, card) = factory.Modal(host, "SettingsPanel", new Vector2(920f, 1000f), theme.deep);

            var title = factory.Text(card, "Title", "Settings", theme.headingSize, theme.accent, TextAnchor.MiddleCenter, outline: true);
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(800f, 120f));

            var musicLabel = factory.Text(card, "MusicLabel", "Music", theme.bodySize + 6, theme.textLight, TextAnchor.MiddleLeft);
            UiFactory.Place(musicLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-60f, 250f), new Vector2(680f, 70f));
            var musicSlider = factory.Slider(card, "MusicSlider", new Vector2(-130f, 160f), 520f);
            var musicMute = factory.Toggle(card, "MusicMute", "Mute", new Vector2(290f, 160f));

            var sfxLabel = factory.Text(card, "SfxLabel", "Sound Effects", theme.bodySize + 6, theme.textLight, TextAnchor.MiddleLeft);
            UiFactory.Place(sfxLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-60f, 30f), new Vector2(680f, 70f));
            var sfxSlider = factory.Slider(card, "SfxSlider", new Vector2(-130f, -60f), 520f);
            var sfxMute = factory.Toggle(card, "SfxMute", "Mute", new Vector2(290f, -60f));

            var close = factory.Button(card, "CloseButton", "DONE", theme.primary, new Vector2(560f, 160f));
            UiFactory.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(560f, 160f));
            UiFactory.Record(close.transform);

            var panel = transition.gameObject.AddComponent<SettingsPanel>();
            EditorRefs.Set(panel,
                ("transition", transition), ("musicSlider", musicSlider), ("musicMuteToggle", musicMute),
                ("sfxSlider", sfxSlider), ("sfxMuteToggle", sfxMute), ("closeButton", close));

            PrefabUtility.SaveAsPrefabAsset(transition.gameObject, ContentPaths.SettingsPanelPrefabPath);
            Object.DestroyImmediate(host.gameObject);
        }
    }
}
