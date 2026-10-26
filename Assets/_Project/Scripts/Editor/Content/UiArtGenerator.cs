using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DesiMerge.Editor.Content
{
    /// <summary>
    /// Draws the original placeholder UI art (rounded panel, painted truck-art frame, geometric/floral pattern,
    /// flower motif, sparkle, pause icon) into PNGs and imports them as sprites. Nothing is downloaded or copied.
    /// Existing files are kept, so hand-made replacements at the same paths survive re-running the setup.
    /// </summary>
    internal static class UiArtGenerator
    {
        public const string Folder = "Assets/_Project/Art/UI";
        public const string RoundedPanelPath = Folder + "/RoundedPanel.png";
        public const string PaintedFramePath = Folder + "/PaintedFrame.png";
        public const string PatternPath = Folder + "/TruckArtPattern.png";
        public const string FlowerPath = Folder + "/Flower.png";
        public const string SparklePath = Folder + "/Sparkle.png";
        public const string PauseIconPath = Folder + "/PauseIcon.png";

        private static readonly Color Teal = new Color(0f, 0.54f, 0.48f);
        private static readonly Color Magenta = new Color(0.85f, 0.11f, 0.38f);
        private static readonly Color Saffron = new Color(1f, 0.70f, 0f);
        private static readonly Color Blue = new Color(0.10f, 0.14f, 0.49f);

        public static void EnsureAll()
        {
            Ensure(RoundedPanelPath, () => RoundedRect(128, 40f, 0f, 0f), 100, new Vector4(44, 44, 44, 44), false);
            Ensure(PaintedFramePath, PaintedFrame, 100, new Vector4(48, 48, 48, 48), false);
            Ensure(PatternPath, Pattern, 100, Vector4.zero, true);
            Ensure(FlowerPath, () => Flower(128), 100, Vector4.zero, false);
            Ensure(SparklePath, () => Sparkle(64), 64, Vector4.zero, false);
            Ensure(PauseIconPath, () => PauseIcon(64), 64, Vector4.zero, false);
        }

        private static void Ensure(string path, Func<Texture2D> draw, int pixelsPerUnit, Vector4 border, bool repeat)
        {
            if (AssetDatabase.LoadAssetAtPath<Sprite>(path) != null)
            {
                return;
            }

            ContentPaths.EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            var texture = draw();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.spriteBorder = border;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; // required for sliced/tiled drawing
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        /// <summary>Signed distance to a rounded box centred in the texture (negative inside).</summary>
        private static float RoundedBoxDistance(float x, float y, int size, float radius, float inset)
        {
            var half = size * 0.5f - inset;
            var px = Mathf.Abs(x - size * 0.5f) - (half - radius);
            var py = Mathf.Abs(y - size * 0.5f) - (half - radius);
            var outside = new Vector2(Mathf.Max(px, 0f), Mathf.Max(py, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(px, py), 0f) - radius;
        }

        private static Texture2D RoundedRect(int size, float radius, float inset, float ring)
        {
            var texture = NewTexture(size);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var d = RoundedBoxDistance(x + 0.5f, y + 0.5f, size, radius, inset + 1f);
                    var alpha = Mathf.Clamp01(0.5f - d);
                    if (ring > 0f)
                    {
                        alpha *= Mathf.Clamp01(d + ring + 0.5f);
                    }

                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }

        /// <summary>Thick painted outline with an inner row of dots, like hand-painted truck panel borders.</summary>
        private static Texture2D PaintedFrame()
        {
            const int size = 128;
            var texture = NewTexture(size);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var d = RoundedBoxDistance(x + 0.5f, y + 0.5f, size, 40f, 1f);
                    var outer = Mathf.Clamp01(0.5f - d) * Mathf.Clamp01(d + 9.5f);
                    var dotPhase = Mathf.Repeat((x + y) * 0.5f, 8f);
                    var dots = Mathf.Clamp01(1.5f - Mathf.Abs(d + 17f)) * (dotPhase < 4f ? 1f : 0f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Max(outer, dots)));
                }
            }

            texture.Apply();
            return texture;
        }

        /// <summary>Tileable diamond lattice with flowers and dots in truck-art colours (128 px, period 64).</summary>
        private static Texture2D Pattern()
        {
            const int size = 128;
            var texture = NewTexture(size);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var color = new Color(0f, 0f, 0f, 0f);
                    var a = Mathf.Abs(Mathf.Repeat(x - y + 32f, 64f) - 32f);
                    var b = Mathf.Abs(Mathf.Repeat(x + y + 32f, 64f) - 32f);
                    var line = Mathf.Clamp01(2f - Mathf.Min(a, b));
                    color = Blend(color, Teal, line * 0.9f);

                    // Flowers at diamond centres (32,0) and (0,32) repeating every 64 px.
                    var flower = Mathf.Max(FlowerMask(x, y, 32f, 0f), FlowerMask(x, y, 0f, 32f));
                    color = Blend(color, Magenta, flower);
                    var centre = Mathf.Max(DiscMask(x, y, 32f, 0f, 3.5f), DiscMask(x, y, 0f, 32f, 3.5f));
                    color = Blend(color, Saffron, centre);

                    // Dots on lattice crossings.
                    var crossing = Mathf.Max(DiscMask(x, y, 0f, 0f, 3f), DiscMask(x, y, 32f, 32f, 3f));
                    color = Blend(color, Saffron, crossing);
                    var inner = Mathf.Max(DiscMask(x, y, 16f, 16f, 1.8f), DiscMask(x, y, 48f, 16f, 1.8f));
                    inner = Mathf.Max(inner, Mathf.Max(DiscMask(x, y, 16f, 48f, 1.8f), DiscMask(x, y, 48f, 48f, 1.8f)));
                    color = Blend(color, Blue, inner);
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return texture;
        }

        private static Texture2D Flower(int size)
        {
            var texture = NewTexture(size);
            var c = size * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x + 0.5f - c;
                    var dy = y + 0.5f - c;
                    var r = Mathf.Sqrt(dx * dx + dy * dy) / c;
                    var angle = Mathf.Atan2(dy, dx);
                    var petalEdge = 0.62f + 0.33f * Mathf.Abs(Mathf.Cos(angle * 3f));
                    var alpha = Mathf.Clamp01((petalEdge - r) * c * 0.5f);
                    var shade = r < 0.25f ? 0.75f : 1f;
                    texture.SetPixel(x, y, new Color(shade, shade, shade, alpha));
                }
            }

            texture.Apply();
            return texture;
        }

        private static Texture2D Sparkle(int size)
        {
            var texture = NewTexture(size);
            var c = size * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = Mathf.Abs(x + 0.5f - c) / c;
                    var dy = Mathf.Abs(y + 0.5f - c) / c;
                    var star = Mathf.Exp(-dx * 9f) * Mathf.Exp(-dy * 2.2f) + Mathf.Exp(-dy * 9f) * Mathf.Exp(-dx * 2.2f);
                    var glow = Mathf.Exp(-(dx * dx + dy * dy) * 6f) * 0.6f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(star + glow)));
                }
            }

            texture.Apply();
            return texture;
        }

        private static Texture2D PauseIcon(int size)
        {
            var texture = NewTexture(size);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var bar = Mathf.Max(BarMask(x, y, size, 0.30f), BarMask(x, y, size, 0.70f));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, bar));
                }
            }

            texture.Apply();
            return texture;
        }

        private static float BarMask(int x, int y, int size, float centreX)
        {
            var px = Mathf.Abs(x + 0.5f - size * centreX) - size * 0.09f;
            var py = Mathf.Abs(y + 0.5f - size * 0.5f) - size * 0.30f;
            return Mathf.Clamp01(0.5f - Mathf.Max(px, py));
        }

        private static float DiscMask(float x, float y, float cx, float cy, float radius)
        {
            var dx = Mathf.Abs(Mathf.Repeat(x + 0.5f - cx + 32f, 64f) - 32f);
            var dy = Mathf.Abs(Mathf.Repeat(y + 0.5f - cy + 32f, 64f) - 32f);
            return Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
        }

        private static float FlowerMask(float x, float y, float cx, float cy)
        {
            var mask = 0f;
            for (var i = 0; i < 5; i++)
            {
                var angle = i * Mathf.PI * 2f / 5f + Mathf.PI * 0.5f;
                mask = Mathf.Max(mask, DiscMask(x, y, cx + Mathf.Cos(angle) * 7f, cy + Mathf.Sin(angle) * 7f, 5f));
            }

            return mask;
        }

        private static Color Blend(Color under, Color over, float amount)
        {
            if (amount <= 0f)
            {
                return under;
            }

            var alpha = amount + under.a * (1f - amount);
            var rgb = (Color)(((Vector4)over * amount) + ((Vector4)under * under.a * (1f - amount)));
            rgb /= Mathf.Max(alpha, 0.0001f);
            rgb.a = alpha;
            return rgb;
        }

        private static Texture2D NewTexture(int size)
        {
            return new Texture2D(size, size, TextureFormat.RGBA32, false);
        }
    }
}
