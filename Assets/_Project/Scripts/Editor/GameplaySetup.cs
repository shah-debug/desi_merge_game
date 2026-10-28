using System.IO;
using DesiMerge.Configuration;
using DesiMerge.Editor.Content;
using DesiMerge.Gameplay;
using UnityEditor;
using UnityEngine;

namespace DesiMerge.Editor
{
    /// <summary>
    /// Generates the gameplay data: placeholder item sprites, physics material, merge item definitions, progression,
    /// gameplay config and the MergeItem prefab. Existing assets are kept, so tuned values survive re-running.
    /// Scenes are built by <see cref="ContentSetup"/>.
    /// </summary>
    public static class GameplaySetup
    {
        public const string PlaceholderArtFolder = "Assets/_Project/Art/Placeholder";
        public const string CircleSpritePath = PlaceholderArtFolder + "/Circle.png";
        public const string SquareSpritePath = PlaceholderArtFolder + "/Square.png";
        public const string GameplayConfigPath = "Assets/_Project/ScriptableObjects/GameplayConfig.asset";
        private const string PhysicsFolder = "Assets/_Project/Settings/Physics";
        private const string ItemMaterialPath = PhysicsFolder + "/MergeItem.physicsMaterial2D";
        private const string ItemsFolder = "Assets/_Project/ScriptableObjects/MergeItems";
        private const string ProgressionPath = "Assets/_Project/ScriptableObjects/MergeProgression.asset";
        private const string ItemPrefabPath = ContentPaths.ItemPrefabPath;

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

        /// <summary>Kept for existing batch scripts and docs; same as Desi Merge > Setup > Create All Content.</summary>
        public static void CreateGameplayContent() => ContentSetup.CreateAllContent();

        public static void EnsureGameplayData()
        {
            ContentPaths.EnsureFolder(PlaceholderArtFolder);
            ContentPaths.EnsureFolder(PhysicsFolder);
            ContentPaths.EnsureFolder(ItemsFolder);

            EnsureSprite(CircleSpritePath, GenerateCircle(256), 256, SpriteMeshType.Tight);
            EnsureSprite(SquareSpritePath, GenerateSquare(8), 8, SpriteMeshType.FullRect);
            EnsurePhysicsMaterial();
            EnsureDefinitions();
            EnsureProgression();
            EnsureGameplayConfig();
            EnsureItemPrefab();
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

        /// <param name="overwrite">Rebuild an existing prefab in place (keeps its asset GUID).</param>
        internal static void EnsureItemPrefab(bool overwrite = false)
        {
            if (!overwrite && AssetDatabase.LoadAssetAtPath<GameObject>(ItemPrefabPath) != null)
            {
                return;
            }

            // Root: physics only (body, collider, scaled to the item's diameter).
            // Visual child: sprite + label; merge "pop" animations scale only this child, never the collider.
            var root = new GameObject("MergeItem");
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var spriteRenderer = visual.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
            spriteRenderer.sortingOrder = 8;
            var punch = UiFactory.Punch(visual, 0.22f, 0.26f, false, 0.12f);

            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var circle = root.AddComponent<CircleCollider2D>();
            circle.radius = 0.5f;

            var item = root.AddComponent<MergeItem>();
            EditorRefs.Set(item, ("spriteRenderer", spriteRenderer), ("body", body), ("label", null), ("visualPunch", punch));

            PrefabUtility.SaveAsPrefabAsset(root, ItemPrefabPath);
            Object.DestroyImmediate(root);
        }
    }
}
