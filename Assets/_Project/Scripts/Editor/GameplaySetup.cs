using System.IO;
using DesiMerge.Configuration;
using DesiMerge.Gameplay;
using DesiMerge.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DesiMerge.Editor
{
    /// <summary>
    /// Generates the Phase 2 placeholder content: sprites, physics material, merge item definitions, progression,
    /// gameplay config, the MergeItem prefab and the Gameplay scene. Existing assets are kept, so tuned values and
    /// scene edits survive re-running. Also points GameConfig at the Gameplay scene and sets the build scene order.
    /// Batch mode: -executeMethod DesiMerge.Editor.GameplaySetup.CreateGameplayContent
    /// </summary>
    public static class GameplaySetup
    {
        public const string GameplayScenePath = "Assets/_Project/Scenes/Gameplay.unity";
        public const string GameplaySceneName = "Gameplay";

        private const string PlaceholderArtFolder = "Assets/_Project/Art/Placeholder";
        private const string CircleSpritePath = PlaceholderArtFolder + "/Circle.png";
        private const string SquareSpritePath = PlaceholderArtFolder + "/Square.png";
        private const string PhysicsFolder = "Assets/_Project/Settings/Physics";
        private const string ItemMaterialPath = PhysicsFolder + "/MergeItem.physicsMaterial2D";
        private const string ItemsFolder = "Assets/_Project/ScriptableObjects/MergeItems";
        private const string ProgressionPath = "Assets/_Project/ScriptableObjects/MergeProgression.asset";
        private const string GameplayConfigPath = "Assets/_Project/ScriptableObjects/GameplayConfig.asset";
        private const string ItemPrefabPath = "Assets/_Project/Prefabs/MergeItem.prefab";

        // Provisional progression (id, display name, tint, radius, score when created by a merge).
        private static readonly (string Id, string Name, Color Tint, float Radius, int Score)[] Items =
        {
            ("patti", "Patti", new Color(0.30f, 0.69f, 0.31f), 0.30f, 1),
            ("nimbu", "Nimbu", new Color(0.80f, 0.86f, 0.22f), 0.40f, 3),
            ("pyaz", "Pyaz", new Color(0.61f, 0.35f, 0.71f), 0.50f, 6),
            ("tamatar", "Tamatar", new Color(0.90f, 0.22f, 0.21f), 0.60f, 10),
            ("roti", "Roti", new Color(0.87f, 0.72f, 0.53f), 0.72f, 15),
            ("aam", "Aam", new Color(1.00f, 0.76f, 0.03f), 0.85f, 21),
            ("chai", "Chai", new Color(0.55f, 0.36f, 0.24f), 1.00f, 28),
            ("mango_shake", "Mango Shake", new Color(1.00f, 0.60f, 0.00f), 1.15f, 36),
            ("rickshaw", "Rickshaw", new Color(0.00f, 0.59f, 0.53f), 1.30f, 45),
            ("bus", "Bus", new Color(0.25f, 0.32f, 0.71f), 1.45f, 55),
        };

        private const int SpawnableLevelCount = 5;

        [MenuItem("Desi Merge/Setup/Create Gameplay Content")]
        public static void CreateGameplayContent()
        {
            FoundationSetup.CreateFoundationAssets();
            EnsureFolder(PlaceholderArtFolder);
            EnsureFolder(PhysicsFolder);
            EnsureFolder(ItemsFolder);

            EnsureSprite(CircleSpritePath, GenerateCircle(256), 256, SpriteMeshType.Tight);
            EnsureSprite(SquareSpritePath, GenerateSquare(8), 8, SpriteMeshType.FullRect);
            EnsurePhysicsMaterial();
            EnsureDefinitions();
            EnsureProgression();
            EnsureGameplayConfig();
            EnsureItemPrefab();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GameplayScenePath) == null)
            {
                EditorSceneUtility.CreateScene(GameplayScenePath, PopulateGameplayScene);
            }

            PointGameConfigAtGameplayScene();
            EditorSceneUtility.SetLeadingBuildScenes(FoundationSetup.BootstrapScenePath, GameplayScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Gameplay content is in place.");
        }

        [MenuItem("Desi Merge/Setup/Rebuild Gameplay Scene (overwrites)")]
        public static void RebuildGameplayScene()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Rebuild Gameplay scene?",
                    $"This deletes {GameplayScenePath} and generates it again. Manual edits to that scene are lost.", "Rebuild", "Cancel"))
            {
                return;
            }

            AssetDatabase.DeleteAsset(GameplayScenePath);
            CreateGameplayContent();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void EnsureSprite(string path, Texture2D texture, int pixelsPerUnit, SpriteMeshType meshType)
        {
            if (AssetDatabase.LoadAssetAtPath<Sprite>(path) != null)
            {
                Object.DestroyImmediate(texture);
                return;
            }

            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = meshType;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        /// <summary>White disc with a soft grey rim so touching items of the same colour stay distinguishable.</summary>
        private static Texture2D GenerateCircle(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = (size - 1) * 0.5f;
            var radius = size * 0.5f - 1f;
            var rim = size * 0.06f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    var alpha = Mathf.Clamp01(radius - distance + 0.5f);
                    var shade = distance > radius - rim ? 0.7f : 1f;
                    texture.SetPixel(x, y, new Color(shade, shade, shade, alpha));
                }
            }

            texture.Apply();
            return texture;
        }

        private static Texture2D GenerateSquare(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static void EnsurePhysicsMaterial()
        {
            if (AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(ItemMaterialPath) != null)
            {
                return;
            }

            AssetDatabase.CreateAsset(new PhysicsMaterial2D("MergeItem") { friction = 0.3f, bounciness = 0.05f }, ItemMaterialPath);
        }

        private static string DefinitionPath(int level) => $"{ItemsFolder}/{level + 1:00}_{Items[level].Id}.asset";

        private static void EnsureDefinitions()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
            for (var level = 0; level < Items.Length; level++)
            {
                var path = DefinitionPath(level);
                if (AssetDatabase.LoadAssetAtPath<MergeItemDefinition>(path) != null)
                {
                    continue;
                }

                var item = Items[level];
                var definition = ScriptableObject.CreateInstance<MergeItemDefinition>();
                definition.Set(item.Id, item.Name, sprite, item.Tint, item.Radius, 0.5f + item.Radius * item.Radius * 2f, item.Score);
                AssetDatabase.CreateAsset(definition, path);
            }
        }

        private static void EnsureProgression()
        {
            if (AssetDatabase.LoadAssetAtPath<MergeProgression>(ProgressionPath) != null)
            {
                return;
            }

            var definitions = new MergeItemDefinition[Items.Length];
            for (var level = 0; level < Items.Length; level++)
            {
                definitions[level] = AssetDatabase.LoadAssetAtPath<MergeItemDefinition>(DefinitionPath(level));
            }

            var progression = ScriptableObject.CreateInstance<MergeProgression>();
            progression.Set(definitions, SpawnableLevelCount);
            AssetDatabase.CreateAsset(progression, ProgressionPath);
        }

        private static void EnsureGameplayConfig()
        {
            if (AssetDatabase.LoadAssetAtPath<GameplayConfig>(GameplayConfigPath) != null)
            {
                return;
            }

            var config = ScriptableObject.CreateInstance<GameplayConfig>();
            config.Set(AssetDatabase.LoadAssetAtPath<MergeProgression>(ProgressionPath), AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(ItemMaterialPath));
            AssetDatabase.CreateAsset(config, GameplayConfigPath);
        }

        private static void EnsureItemPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ItemPrefabPath) != null)
            {
                return;
            }

            var root = new GameObject("MergeItem");
            var spriteRenderer = root.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
            spriteRenderer.sortingOrder = 1;

            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var circle = root.AddComponent<CircleCollider2D>();
            circle.radius = 0.5f;

            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(root.transform, false);
            var label = labelObject.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.font = font;
            label.fontSize = 64;
            label.characterSize = 0.03f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color(0.1f, 0.08f, 0.06f);
            var labelRenderer = labelObject.GetComponent<MeshRenderer>();
            labelRenderer.sharedMaterial = font.material;
            labelRenderer.sortingOrder = 2;

            var item = root.AddComponent<MergeItem>();
            var serialized = new SerializedObject(item);
            serialized.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            serialized.FindProperty("body").objectReferenceValue = body;
            serialized.FindProperty("label").objectReferenceValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, ItemPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void PointGameConfigAtGameplayScene()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(FoundationSetup.GameConfigPath);
            var serialized = new SerializedObject(config);
            var firstScene = serialized.FindProperty("firstSceneName");
            if (string.IsNullOrEmpty(firstScene.stringValue))
            {
                firstScene.stringValue = GameplaySceneName;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(config);
            }
        }

        private static void PopulateGameplayScene()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameplayConfig>(GameplayConfigPath);
            var itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ItemPrefabPath).GetComponent<MergeItem>();
            var square = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Camera: 16 world units tall; the board's floor sits near the bottom and the HUD uses the top.
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.98f, 0.88f, 0.70f);

            var boardObject = new GameObject("Board");
            boardObject.transform.position = new Vector3(0f, -7f, 0f);
            var board = boardObject.AddComponent<MergeBoard>();
            var items = new GameObject("Items").transform;
            items.SetParent(boardObject.transform, false);
            SetReferences(board, ("itemPrefab", itemPrefab), ("itemRoot", items), ("blockSprite", square));

            var dropObject = new GameObject("DropController");
            var drop = dropObject.AddComponent<DropController>();
            var previewObject = new GameObject("Preview");
            previewObject.transform.SetParent(dropObject.transform, false);
            var preview = previewObject.AddComponent<SpriteRenderer>();
            preview.sortingOrder = 3;
            preview.enabled = false;
            SetReferences(drop, ("worldCamera", camera), ("preview", preview));

            var hud = BuildHud(font);
            BuildEventSystem();

            var entryObject = new GameObject("GameplayEntryPoint");
            var entry = entryObject.AddComponent<GameplayEntryPoint>();
            SetReferences(entry, ("gameplayConfig", config), ("board", board), ("dropController", drop), ("hud", hud));
        }

        private static GameplayHud BuildHud(Font font)
        {
            var canvasObject = new GameObject("HUD", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            var root = canvasObject.transform;
            var dark = new Color(0.25f, 0.13f, 0.05f);

            var score = CreateText(root, "ScoreText", font, "0", 96, TextAnchor.UpperLeft, dark,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -60f), new Vector2(600f, 120f));
            var best = CreateText(root, "BestText", font, "Best 0", 44, TextAnchor.UpperLeft, dark,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -180f), new Vector2(600f, 60f));

            CreateText(root, "NextCaption", font, "Next", 40, TextAnchor.UpperCenter, dark,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-260f, -50f), new Vector2(220f, 50f));
            var nextImage = CreateRect(root, "NextImage", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-205f, -110f), new Vector2(110f, 110f))
                .gameObject.AddComponent<Image>();
            nextImage.preserveAspect = true;
            nextImage.raycastTarget = false;
            var nextLabel = CreateText(root, "NextLabel", font, string.Empty, 34, TextAnchor.UpperCenter, dark,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-260f, -225f), new Vector2(220f, 50f));

            var panel = CreateRect(root, "GameOverPanel", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panel.pivot = new Vector2(0.5f, 0.5f);
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.65f);
            var gameOverText = CreateText(panel, "GameOverText", font, "Game Over", 80, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-450f, 400f), new Vector2(900f, 500f));

            var buttonRect = CreateRect(panel, "RestartButton", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-250f, -150f), new Vector2(500f, 150f));
            var buttonImage = buttonRect.gameObject.AddComponent<Image>();
            buttonImage.color = new Color(0.9f, 0.45f, 0.1f);
            var button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            CreateText(buttonRect, "Label", font, "Restart", 64, TextAnchor.MiddleCenter, Color.white,
                Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(500f, 150f));

            var errorText = CreateText(root, "ErrorText", font, string.Empty, 44, TextAnchor.MiddleCenter, new Color(0.8f, 0f, 0f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-480f, 100f), new Vector2(960f, 200f));

            var hud = canvasObject.AddComponent<GameplayHud>();
            SetReferences(hud,
                ("scoreText", score), ("bestText", best), ("nextImage", nextImage), ("nextLabel", nextLabel),
                ("gameOverPanel", panel.gameObject), ("gameOverText", gameOverText), ("restartButton", button), ("errorText", errorText));
            panel.gameObject.SetActive(false);
            errorText.gameObject.SetActive(false);
            return hud;
        }

        private static void BuildEventSystem()
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        /// <summary>Creates a RectTransform positioned by its top-left corner (pivot 0,1) relative to the anchor.</summary>
        private static RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            if (anchorMin == Vector2.zero && anchorMax == Vector2.one)
            {
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            return rect;
        }

        private static Text CreateText(Transform parent, string name, Font font, string content, int fontSize, TextAnchor alignment, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            var rect = CreateRect(parent, name, anchorMin, anchorMax, position, size);
            if (anchorMin == Vector2.zero && anchorMax == Vector2.zero && position == Vector2.zero)
            {
                // Fill the parent (used for button labels).
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void SetReferences(Object target, params (string Property, Object Value)[] references)
        {
            var serialized = new SerializedObject(target);
            foreach (var (property, value) in references)
            {
                var field = serialized.FindProperty(property);
                if (field == null)
                {
                    Debug.LogError($"[Setup] {target.GetType().Name} has no serialized field '{property}'.");
                    continue;
                }

                field.objectReferenceValue = value;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
