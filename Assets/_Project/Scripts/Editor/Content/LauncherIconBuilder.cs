using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

namespace DesiMerge.Editor.Content
{
    /// <summary>
    /// Original Android launcher icon: an adaptive icon (Android 8+, which is every device this game supports since
    /// minSdk is 26) made of a background layer (magenta truck-art sunburst) and a foreground layer (the mango with a
    /// small nimbu merging into it and a sparkle). Layers are 432 px = 108 dp; launchers show the centre 72 dp and
    /// may mask it to any shape, so all foreground art stays inside the 66 dp safe circle (radius 0.61 here).
    /// Unity 6.6 only offers the adaptive slot for Android (legacy/round are obsolete); AppIcon.png stays the default
    /// icon for other platforms.
    /// </summary>
    internal static class LauncherIconBuilder
    {
        public const string Folder = "Assets/_Project/Art/Icon";
        public const string BackgroundPath = Folder + "/LauncherBackground.png";
        public const string ForegroundPath = Folder + "/LauncherForeground.png";
        public const float SafeRadius = 0.61f;

        private const int Size = 432;
        private static readonly Color Saffron = ArtCanvas.Hex("#FFB627");
        private static readonly Color Navy = ArtCanvas.Hex("#1B1F4B");
        private static readonly Color Teal = ArtCanvas.Hex("#0E9F8A");

        [MenuItem("Desi Merge/Setup/Regenerate Launcher Icon")]
        public static void Regenerate()
        {
            Apply(overwrite: true);
            WritePreview("Temp/launcher-icon-preview.png");
        }

        public static void Apply(bool overwrite)
        {
            ContentPaths.EnsureFolder(Folder);
            Write(BackgroundPath, overwrite, DrawBackground);
            Write(ForegroundPath, overwrite, DrawForeground);
            Configure();
        }

        internal static void DrawBackground(ArtCanvas a)
        {
            var centre = ArtCanvas.Hex("#FF3EA5");
            var edge = ArtCanvas.Hex("#9C0F5C");
            a.Fill(p => -1f, p => Color.Lerp(centre, edge, Mathf.Clamp01(p.magnitude / 1.25f)));
            a.Fill(p => Mathf.Repeat(Mathf.Atan2(p.y, p.x) / (Mathf.PI * 2f) * 16f, 1f) < 0.5f ? -1f : 1f, ArtCanvas.WithAlpha(Saffron, 0.18f));
            a.Fill(p => Mathf.Abs(p.magnitude - 0.6f) - 0.02f, ArtCanvas.WithAlpha(Saffron, 0.9f));
            for (var i = 0; i < 20; i++)
            {
                var angle = i * 18f * Mathf.Deg2Rad;
                var dot = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.6f;
                a.Fill(p => Sdf.Circle(p, dot, 0.024f), i % 2 == 0 ? Navy : Teal);
            }

            a.Fill(p => Sdf.Circle(p, Vector2.zero, 0.5f), ArtCanvas.WithAlpha(Color.white, 0.14f), 0.25f);
        }

        internal static void DrawForeground(ArtCanvas a)
        {
            a.Offset = new Vector2(0.05f, 0.03f);
            a.Zoom = 0.44f;
            ItemArtGenerator.Draw("aam", a);
            a.Offset = new Vector2(-0.27f, -0.26f);
            a.Zoom = 0.2f;
            ItemArtGenerator.Draw("nimbu", a);
            a.Offset = Vector2.zero;
            a.Zoom = 1f;
            var sparkle = ItemArtGenerator.StarVertices(new Vector2(0.32f, 0.32f), 4, 0.11f, 0.03f, Mathf.PI * 0.5f);
            a.Fill(p => Sdf.Polygon(p, sparkle) - 0.012f, Color.white);
        }

        private static void Write(string path, bool overwrite, System.Action<ArtCanvas> draw)
        {
            if (!overwrite && AssetDatabase.LoadAssetAtPath<Texture2D>(path) != null)
            {
                return;
            }

            var canvas = new ArtCanvas(Size, Size);
            draw(canvas);
            var texture = canvas.ToTexture();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void Configure()
        {
            var background = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);
            var foreground = AssetDatabase.LoadAssetAtPath<Texture2D>(ForegroundPath);
            var target = NamedBuildTarget.Android;

            var adaptive = PlayerSettings.GetPlatformIcons(target, AndroidPlatformIconKind.Adaptive);
            foreach (var icon in adaptive)
            {
                icon.SetTexture(background, 0);
                icon.SetTexture(foreground, 1);
            }

            PlayerSettings.SetPlatformIcons(target, AndroidPlatformIconKind.Adaptive, adaptive);

            AssetDatabase.SaveAssets();
        }

        public const string StoreIconPath = "Documentation/StoreListing/icon-512.png";

        /// <summary>
        /// Google Play store icon: the same artwork drawn natively at 512 × 512 (not an upscale), opaque and full-bleed,
        /// because Play Console applies its own rounded mask.
        /// </summary>
        [MenuItem("Desi Merge/Release/Write Play Store Icon")]
        public static void WriteStoreIcon() => WriteStoreIcon(StoreIconPath);

        internal static void WriteStoreIcon(string path)
        {
            // Launchers show the centre ~2/3 of an adaptive icon (72 of 108 dp); draw larger and keep that centre so
            // the store icon matches what players see on their home screen.
            const int size = 512;
            const int full = 768;
            var canvas = new ArtCanvas(full, full);
            DrawBackground(canvas);
            DrawForeground(canvas);
            var source = canvas.ToTexture();
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var offset = (full - size) / 2;
            var pixels = source.GetPixels32();
            var cropped = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var p = pixels[(y + offset) * full + x + offset];
                    p.a = 255;
                    cropped[y * size + x] = p;
                }
            }

            Object.DestroyImmediate(source);
            texture.SetPixels32(cropped);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            Debug.Log($"[Release] Play Store icon written: {path}");
        }

        /// <summary>Writes a preview of the icon as launchers show it (circle and rounded square) to the given PNG.</summary>
        internal static void WritePreview(string path)
        {
            const int tile = 256;
            var background = new ArtCanvas(Size, Size);
            DrawBackground(background);
            var foreground = new ArtCanvas(Size, Size);
            DrawForeground(foreground);
            var back = background.ToTexture();
            var front = foreground.ToTexture();
            var preview = new Texture2D(tile * 2, tile, TextureFormat.RGBA32, false);
            for (var y = 0; y < tile; y++)
            {
                for (var x = 0; x < tile * 2; x++)
                {
                    var local = new Vector2((x % tile + 0.5f) / tile * 2f - 1f, (y + 0.5f) / tile * 2f - 1f);
                    var inside = x < tile ? local.magnitude <= 0.95f : Sdf.Box(local, Vector2.zero, new Vector2(0.95f, 0.95f), 0.3f) <= 0f;
                    if (!inside)
                    {
                        preview.SetPixel(x, y, new Color(0.93f, 0.93f, 0.95f, 1f));
                        continue;
                    }

                    var uv = (local * (72f / 108f) + Vector2.one) * 0.5f;
                    preview.SetPixel(x, y, ArtCanvas.Over(back.GetPixelBilinear(uv.x, uv.y), front.GetPixelBilinear(uv.x, uv.y)));
                }
            }

            File.WriteAllBytes(path, preview.EncodeToPNG());
            Object.DestroyImmediate(back);
            Object.DestroyImmediate(front);
            Object.DestroyImmediate(preview);
        }
    }

    /// <summary>Shows "Desi Merge" (with a space) under the launcher icon. The product name and save paths are unchanged.</summary>
    public sealed class AndroidAppLabel : IPostGenerateGradleAndroidProject
    {
        public const string Label = "Desi Merge";

        public int callbackOrder => 110;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            foreach (var module in new[] { path, Path.Combine(path, "..", "launcher") })
            {
                var strings = Path.Combine(module, "src", "main", "res", "values", "strings.xml");
                if (!File.Exists(strings))
                {
                    continue;
                }

                var xml = File.ReadAllText(strings);
                var updated = System.Text.RegularExpressions.Regex.Replace(xml, "<string name=\"app_name\">[^<]*</string>", $"<string name=\"app_name\">{Label}</string>");
                if (updated != xml)
                {
                    File.WriteAllText(strings, updated);
                }
            }
        }
    }
}
