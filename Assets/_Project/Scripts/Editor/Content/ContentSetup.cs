using DesiMerge.Audio;
using DesiMerge.Configuration;
using DesiMerge.Gameplay;
using DesiMerge.PowerUps;
using DesiMerge.Progression;
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
        private const string DisplayFontPath = "Assets/_Project/Art/Fonts/LilitaOne-Regular.ttf";

        [MenuItem("Desi Merge/Setup/Create All Content")]
        public static void CreateAllContent()
        {
            FoundationSetup.CreateFoundationAssets();
            GameplaySetup.EnsureGameplayData();
            UiArtGenerator.EnsureAll();
            GameArtGenerator.EnsureAll(overwrite: false);
            ItemArtGenerator.EnsureAll(overwrite: false);
            AudioSynth.EnsureAll();
            EnsureTheme();
            EnsureAudioLibrary();
            EnsureEffectMaterial(ContentPaths.SparkleMaterialPath, UiArtGenerator.SparklePath);
            EnsureEffectMaterial(ContentPaths.RingMaterialPath, GameArtGenerator.RingPath);
            EnsureButtonPrefab();
            EnsureSettingsPanelPrefab();
            ProgressionContentBuilder.EnsureCatalogs();
            ProgressionContentBuilder.EnsureCollectionPanelPrefab();
            ProgressionContentBuilder.EnsureAchievementsPanelPrefab();
            PowerUpContentBuilder.EnsurePowerUpCatalog();
            PowerUpContentBuilder.EnsureDailyRewards();
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
            AssetDatabase.DeleteAsset(ContentPaths.CollectionPanelPrefabPath);
            AssetDatabase.DeleteAsset(ContentPaths.AchievementsPanelPrefabPath);
            AssetDatabase.DeleteAsset(ContentPaths.ButtonPrefabPath);
            GameplaySetup.EnsureItemPrefab(overwrite: true);
            CreateAllContent();
        }

        [MenuItem("Desi Merge/Setup/Apply Visual Redesign (regenerates art, scenes and prefabs)")]
        public static void ApplyVisualRedesign()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Apply visual redesign?",
                    "This regenerates the generated art, the UI theme values, the UI prefabs, MergeItem.prefab, MainMenu.unity and Gameplay.unity. " +
                    "Gameplay data, saves, configs and audio are kept.", "Apply", "Cancel"))
            {
                return;
            }

            GameArtGenerator.EnsureAll(overwrite: true);
            ItemArtGenerator.EnsureAll(overwrite: true);
            EnsureTheme();
            ApplyDesignSystem(LoadTheme());
            AssetDatabase.SaveAssets();
            RebuildGeneratedScenesAndPrefabs();
        }

        internal static void ApplyDesignSystem(UITheme theme)
        {
            theme.background = ArtCanvas.Hex("#FFF4DE");
            theme.primary = ArtCanvas.Hex("#E0218A");
            theme.secondary = ArtCanvas.Hex("#0E9F8A");
            theme.accent = ArtCanvas.Hex("#FFB627");
            theme.deep = ArtCanvas.Hex("#1B1F4B");
            theme.danger = ArtCanvas.Hex("#FF5E57");
            theme.textDark = ArtCanvas.Hex("#2A1A33");
            theme.textLight = Color.white;
            theme.overlay = new Color(0.07f, 0.05f, 0.14f, 0.7f);
            theme.card = ArtCanvas.Hex("#FFF9EE");
            theme.mutedText = ArtCanvas.Hex("#5C5473");
            theme.panel = GameArtGenerator.Load(GameArtGenerator.PanelPath);
            theme.button = GameArtGenerator.Load(GameArtGenerator.ButtonPath);
            theme.pill = GameArtGenerator.Load(GameArtGenerator.PillPath);
            theme.disc = GameArtGenerator.Load(GameArtGenerator.DiscPath);
            theme.glow = GameArtGenerator.Load(GameArtGenerator.GlowPath);
            theme.ornament = GameArtGenerator.Load(GameArtGenerator.OrnamentPath);
            theme.borderStrip = GameArtGenerator.Load(GameArtGenerator.BorderStripPath);
            theme.backgroundGradient = GameArtGenerator.Load(GameArtGenerator.BackgroundPath);
            theme.pauseIcon = GameArtGenerator.Icon("Pause");
            theme.font = AssetDatabase.LoadAssetAtPath<Font>(DisplayFontPath);
            theme.bodyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EditorUtility.SetDirty(theme);
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
            ApplyDesignSystem(theme);
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

        private static void EnsureEffectMaterial(string materialPath, string texturePath)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) != null)
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
            var material = new Material(shader) { mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath) };
            AssetDatabase.CreateAsset(material, materialPath);
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

            AssignIfEmpty(serialized, "audioLibrary", AssetDatabase.LoadAssetAtPath<AudioLibrary>(ContentPaths.AudioLibraryPath));
            AssignIfEmpty(serialized, "itemProgression", AssetDatabase.LoadAssetAtPath<MergeProgression>(ContentPaths.MergeProgressionPath));
            AssignIfEmpty(serialized, "achievements", AssetDatabase.LoadAssetAtPath<AchievementCatalog>(ContentPaths.AchievementsPath));
            AssignIfEmpty(serialized, "dailyChallenges", AssetDatabase.LoadAssetAtPath<DailyChallengeCatalog>(ContentPaths.DailyChallengesPath));
            AssignIfEmpty(serialized, "powerUps", AssetDatabase.LoadAssetAtPath<PowerUpCatalog>(ContentPaths.PowerUpsPath));

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        private static void AssignIfEmpty(SerializedObject serialized, string property, Object value)
        {
            var field = serialized.FindProperty(property);
            if (field == null)
            {
                Debug.LogError($"[Setup] GameConfig has no field '{property}'.");
                return;
            }

            if (field.objectReferenceValue == null)
            {
                field.objectReferenceValue = value;
            }
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
            image.sprite = theme.button;
            image.type = Image.Type.Sliced;
            image.color = theme.primary;
            var shadow = root.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(theme.textDark.r, theme.textDark.g, theme.textDark.b, 0.22f);
            shadow.effectDistance = new Vector2(0f, -8f);

            var icon = factory.Image(root, "Icon", null, Color.white);
            icon.preserveAspect = true;
            UiFactory.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, theme.buttonLip * 0.5f), new Vector2(72f, 72f));
            icon.gameObject.SetActive(false);
            var label = factory.Text(root, "Label", "BUTTON", theme.buttonSize, theme.textLight);
            var labelShadow = label.gameObject.AddComponent<Shadow>();
            labelShadow.effectColor = new Color(theme.textDark.r, theme.textDark.g, theme.textDark.b, 0.35f);
            labelShadow.effectDistance = new Vector2(0f, -4f);
            UiFactory.Stretch(label.rectTransform, 20f, 20f, 6f, 6f + theme.buttonLip);

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(0.97f, 0.97f, 0.97f);
            colors.pressedColor = new Color(0.84f, 0.84f, 0.84f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.62f, 0.62f, 0.68f, 0.8f);
            button.colors = colors;
            EditorRefs.Set(root.gameObject.AddComponent<ButtonFeedback>(), ("pressedScale", theme.pressedScale));

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
            var (transition, card) = factory.Modal(host, "SettingsPanel", new Vector2(920f, 980f), "SETTINGS");

            var musicIcon = factory.Image(card, "MusicIcon", GameArtGenerator.Icon("Music"), theme.primary);
            UiFactory.Place(musicIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-370f, 205f), new Vector2(64f, 64f));
            var musicLabel = factory.Text(card, "MusicLabel", "Music", theme.bodySize + 6, theme.textDark, TextAnchor.MiddleLeft);
            UiFactory.Place(musicLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-30f, 205f), new Vector2(600f, 70f));
            var musicSlider = factory.Slider(card, "MusicSlider", new Vector2(-120f, 110f), 540f);
            var musicMute = factory.Toggle(card, "MusicMute", "Mute", new Vector2(300f, 110f));

            var sfxIcon = factory.Image(card, "SfxIcon", GameArtGenerator.Icon("Speaker"), theme.secondary);
            UiFactory.Place(sfxIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-370f, -15f), new Vector2(64f, 64f));
            var sfxLabel = factory.Text(card, "SfxLabel", "Sound Effects", theme.bodySize + 6, theme.textDark, TextAnchor.MiddleLeft);
            UiFactory.Place(sfxLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-30f, -15f), new Vector2(600f, 70f));
            var sfxSlider = factory.Slider(card, "SfxSlider", new Vector2(-120f, -110f), 540f);
            var sfxMute = factory.Toggle(card, "SfxMute", "Mute", new Vector2(300f, -110f));

            var close = factory.Button(card, "CloseButton", "DONE", theme.primary, new Vector2(520f, 150f), icon: GameArtGenerator.Icon("Check"));
            UiFactory.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(520f, 150f));
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
