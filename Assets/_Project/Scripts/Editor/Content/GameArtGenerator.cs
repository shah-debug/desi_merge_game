using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace DesiMerge.Editor.Content
{
    internal static class GameArtGenerator
    {
        public const string Folder = "Assets/_Project/Art/UI/Game";
        public const string IconFolder = "Assets/_Project/Art/UI/Icons";
        public const string PanelPath = Folder + "/Panel.png";
        public const string ButtonPath = Folder + "/ButtonBase.png";
        public const string PillPath = Folder + "/Pill.png";
        public const string DiscPath = Folder + "/Disc.png";
        public const string RingPath = Folder + "/Ring.png";
        public const string GlowPath = Folder + "/Glow.png";
        public const string OrnamentPath = Folder + "/Ornament.png";
        public const string BorderStripPath = Folder + "/BorderStrip.png";
        public const string BackgroundPath = Folder + "/Background.png";
        public const string WallTilePath = Folder + "/WallTile.png";
        public const string DangerDashPath = Folder + "/DangerDash.png";
        public const string AppIconPath = "Assets/_Project/Art/AppIcon.png";

        private static readonly Color Ink = ItemArtGenerator.Ink;
        private static readonly Color Navy = ArtCanvas.Hex("#1B1F4B");
        private static readonly Color Teal = ArtCanvas.Hex("#0E9F8A");
        private static readonly Color Magenta = ArtCanvas.Hex("#E0218A");
        private static readonly Color Saffron = ArtCanvas.Hex("#FFB627");

        public static readonly string[] IconNames =
        {
            "Pause", "Play", "Hammer", "Shuffle", "Gear", "Home", "Restart", "Trophy", "Grid", "Close", "Crown", "Star",
            "Music", "Speaker", "Lock", "Check", "Video",
        };

        public static string IconPath(string name) => $"{IconFolder}/{name}.png";

        public static Sprite Load(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        public static Sprite Icon(string name) => Load(IconPath(name));

        public static void EnsureAll(bool overwrite)
        {
            ContentPaths.EnsureFolder(Folder);
            ContentPaths.EnsureFolder(IconFolder);
            Sprite(overwrite, PanelPath, 128, 128, Panel, 100, new Vector4(44, 44, 44, 44));
            Sprite(overwrite, ButtonPath, 128, 128, ButtonBase, 100, new Vector4(46, 56, 46, 46));
            Sprite(overwrite, PillPath, 128, 128, a => a.Fill(p => p.magnitude - 0.97f, Color.white), 100, new Vector4(62, 62, 62, 62));
            Sprite(overwrite, DiscPath, 128, 128, a => a.Fill(p => p.magnitude - 0.97f, Color.white), 100, Vector4.zero);
            Sprite(overwrite, RingPath, 128, 128, a => a.Fill(p => Mathf.Abs(p.magnitude - 0.8f) - 0.08f, Color.white, 0.04f), 100, Vector4.zero);
            Sprite(overwrite, GlowPath, 128, 128, Glow, 100, Vector4.zero);
            Sprite(overwrite, OrnamentPath, 256, 256, Ornament, 100, Vector4.zero);
            Sprite(overwrite, BorderStripPath, 128, 32, BorderStrip, 100, Vector4.zero, repeat: true);
            Sprite(overwrite, BackgroundPath, 32, 256, Background, 100, Vector4.zero);
            Sprite(overwrite, WallTilePath, 64, 128, WallTile, 128, Vector4.zero, repeat: true);
            Sprite(overwrite, DangerDashPath, 64, 16, a => a.Fill(p => Sdf.Capsule(p, new Vector2(-0.5f, 0f), new Vector2(0.3f, 0f), 0.15f), Color.white), 128, Vector4.zero, repeat: true);
            foreach (var name in IconNames)
            {
                Sprite(overwrite, IconPath(name), 128, 128, a => a.Fill(IconShape(name), Color.white), 100, Vector4.zero);
            }

            EnsureAppIcon(overwrite);
        }

        private static void Sprite(bool overwrite, string path, int width, int height, Action<ArtCanvas> draw, int pixelsPerUnit, Vector4 border, bool repeat = false)
        {
            if (!overwrite && Load(path) != null)
            {
                return;
            }

            var canvas = new ArtCanvas(width, height);
            draw(canvas);
            var texture = canvas.ToTexture();
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
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static void Panel(ArtCanvas a)
        {
            a.Fill(p => Sdf.Box(p, Vector2.zero, new Vector2(0.98f, 0.98f), 0.66f), p => Gray(Mathf.Lerp(0.9f, 1f, Mathf.InverseLerp(-1f, 1f, p.y))));
        }

        private static void ButtonBase(ArtCanvas a)
        {
            a.Fill(p => Sdf.Box(p, Vector2.zero, new Vector2(0.98f, 0.98f), 0.66f), Gray(0.66f));
            a.Fill(p => Sdf.Box(p, new Vector2(0f, 0.08f), new Vector2(0.98f, 0.9f), 0.64f), p => Gray(Mathf.Lerp(0.9f, 1f, Mathf.InverseLerp(-0.8f, 0.9f, p.y))));
            a.Fill(p => Sdf.Box(p, new Vector2(0f, 0.62f), new Vector2(0.62f, 0.12f), 0.12f), ArtCanvas.WithAlpha(Color.white, 0.35f), 0.04f);
        }

        private static void Glow(ArtCanvas a)
        {
            a.Fill(p => p.magnitude - 1f, p =>
            {
                var t = 1f - Mathf.Clamp01(p.magnitude);
                return ArtCanvas.WithAlpha(Color.white, t * t);
            });
        }

        private static void Ornament(ArtCanvas a)
        {
            for (var i = 0; i < 8; i++)
            {
                var angle = 22.5f + i * 45f;
                var centre = Direction(angle) * 0.66f;
                float Leaf(Vector2 p) => Sdf.Ellipse(Sdf.Rotate(p, centre, -angle), centre, new Vector2(0.27f, 0.11f));
                a.Body(Leaf, 0.035f, Ink, ArtCanvas.Hex("#39C9A8"), Teal, 0.06f);
            }

            for (var i = 0; i < 8; i++)
            {
                var angle = i * 45f;
                var centre = Direction(angle) * 0.47f;
                float Petal(Vector2 p) => Sdf.Ellipse(Sdf.Rotate(p, centre, -angle), centre, new Vector2(0.38f, 0.2f));
                a.Body(Petal, 0.04f, Ink, ArtCanvas.Hex("#FF5FB0"), Magenta, 0.1f);
                a.Fill(p => Sdf.Circle(p, Direction(angle) * 0.64f, 0.045f), Color.white);
            }

            for (var i = 0; i < 8; i++)
            {
                var angle = 22.5f + i * 45f;
                var centre = Direction(angle) * 0.22f;
                float Inner(Vector2 p) => Sdf.Ellipse(Sdf.Rotate(p, centre, -angle), centre, new Vector2(0.17f, 0.09f));
                a.Body(Inner, 0.03f, Ink, ArtCanvas.Hex("#FFD36B"), Saffron, 0.05f);
            }

            a.Body(p => Sdf.Circle(p, Vector2.zero, 0.15f), 0.035f, Ink, ArtCanvas.Hex("#3A3F8F"), Navy, 0.06f);
            a.Fill(p => Sdf.Circle(p, Vector2.zero, 0.065f), Saffron);
            a.Fill(p => Sdf.Circle(p, new Vector2(-0.05f, 0.05f), 0.03f), Color.white);
        }

        private static void BorderStrip(ArtCanvas a)
        {
            a.Fill(p => -1f, Navy);
            a.Fill(p => Mathf.Abs(p.y) - 0.25f + 0.045f, ArtCanvas.Hex("#232861"));
            a.Fill(p => Mathf.Min(p.y - 0.2f, -p.y - 0.2f) + 0.01f, Saffron);
            foreach (var x in new[] { -0.5f, 0.5f })
            {
                a.Fill(p => Sdf.Polygon(p, new[] { new Vector2(x - 0.22f, 0f), new Vector2(x, 0.13f), new Vector2(x + 0.22f, 0f), new Vector2(x, -0.13f) }), Magenta);
                a.Fill(p => Sdf.Circle(p, new Vector2(x, 0f), 0.04f), Color.white);
            }

            foreach (var x in new[] { -1f, 0f, 1f })
            {
                a.Fill(p => Sdf.Circle(p, new Vector2(x, 0f), 0.065f), Teal);
                a.Fill(p => Sdf.Circle(p, new Vector2(x, 0f), 0.03f), Saffron);
            }
        }

        private static void Background(ArtCanvas a)
        {
            var top = ArtCanvas.Hex("#FFF7E8");
            var bottom = ArtCanvas.Hex("#FCE3C2");
            a.Fill(p => -1f, p => Color.Lerp(bottom, top, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-a.HalfHeight, a.HalfHeight, p.y))));
        }

        private static void WallTile(ArtCanvas a)
        {
            a.Fill(p => -1f, Navy);
            a.Fill(p => Mathf.Abs(Mathf.Abs(p.x) - 0.78f) - 0.1f, Saffron);
            a.Fill(p => Mathf.Abs(Mathf.Abs(p.x) - 0.6f) - 0.025f, ArtCanvas.WithAlpha(Color.white, 0.5f));
            for (var i = 0; i < 4; i++)
            {
                var y = -1.5f + i;
                var color = i % 2 == 0 ? Magenta : Teal;
                a.Fill(p => Sdf.Polygon(p, new[] { new Vector2(0f, y + 0.42f), new Vector2(0.42f, y), new Vector2(0f, y - 0.42f), new Vector2(-0.42f, y) }), color);
                a.Fill(p => Sdf.Circle(p, new Vector2(0f, y), 0.1f), Color.white);
            }

            for (var i = 0; i < 5; i++)
            {
                var y = -2f + i;
                a.Fill(p => Sdf.Circle(p, new Vector2(0f, y), 0.11f), Saffron);
            }
        }

        private static Func<Vector2, float> IconShape(string name)
        {
            switch (name)
            {
                case "Pause":
                    return p => Mathf.Min(Sdf.Box(p, new Vector2(-0.3f, 0f), new Vector2(0.16f, 0.58f), 0.1f), Sdf.Box(p, new Vector2(0.3f, 0f), new Vector2(0.16f, 0.58f), 0.1f));
                case "Play":
                    return p => Sdf.Polygon(p, new[] { new Vector2(-0.3f, 0.5f), new Vector2(-0.3f, -0.5f), new Vector2(0.52f, 0f) }) - 0.1f;
                case "Hammer":
                    return p =>
                    {
                        var q = Sdf.Rotate(p, Vector2.zero, 40f);
                        var handle = Sdf.Capsule(q, new Vector2(0f, -0.78f), new Vector2(0f, 0.2f), 0.11f);
                        var head = Sdf.Box(q, new Vector2(0.06f, 0.4f), new Vector2(0.48f, 0.17f), 0.06f);
                        var face = Sdf.Box(q, new Vector2(-0.38f, 0.4f), new Vector2(0.14f, 0.26f), 0.06f);
                        return Sdf.Union(handle, Sdf.Union(head, face));
                    };
                case "Shuffle":
                    return p =>
                    {
                        var d = float.MaxValue;
                        foreach (var s in new[] { 1f, -1f })
                        {
                            d = Sdf.Union(d, Sdf.Capsule(p, new Vector2(-0.74f, -0.42f * s), new Vector2(-0.3f, -0.42f * s), 0.1f));
                            d = Sdf.Union(d, Sdf.Capsule(p, new Vector2(-0.3f, -0.42f * s), new Vector2(0.22f, 0.42f * s), 0.1f));
                            d = Sdf.Union(d, Sdf.Capsule(p, new Vector2(0.22f, 0.42f * s), new Vector2(0.45f, 0.42f * s), 0.1f));
                            d = Sdf.Union(d, Sdf.Polygon(p, new[] { new Vector2(0.42f, 0.42f * s + 0.25f), new Vector2(0.42f, 0.42f * s - 0.25f), new Vector2(0.8f, 0.42f * s) }) - 0.03f);
                        }

                        return d;
                    };
                case "Gear":
                    return p =>
                    {
                        var d = Sdf.Circle(p, Vector2.zero, 0.52f);
                        for (var i = 0; i < 8; i++)
                        {
                            d = Sdf.Union(d, Sdf.Box(Sdf.Rotate(p, Vector2.zero, -i * 45f), new Vector2(0f, 0.62f), new Vector2(0.14f, 0.2f), 0.05f));
                        }

                        return Sdf.Subtract(d, Sdf.Circle(p, Vector2.zero, 0.2f));
                    };
                case "Home":
                    return p =>
                    {
                        var roof = Sdf.Polygon(p, new[] { new Vector2(-0.72f, 0.06f), new Vector2(0f, 0.72f), new Vector2(0.72f, 0.06f) }) - 0.07f;
                        var body = Sdf.Box(p, new Vector2(0f, -0.32f), new Vector2(0.5f, 0.42f), 0.08f);
                        return Sdf.Subtract(Sdf.Union(roof, body), Sdf.Box(p, new Vector2(0f, -0.5f), new Vector2(0.14f, 0.28f), 0.06f));
                    };
                case "Restart":
                    return p =>
                    {
                        const float radius = 0.5f;
                        var start = 55f * Mathf.Deg2Rad;
                        var arc = Sdf.Arc(p, Vector2.zero, radius, start, 330f * Mathf.Deg2Rad, 0.19f);
                        var tip = new Vector2(Mathf.Cos(start), Mathf.Sin(start)) * radius;
                        var along = new Vector2(Mathf.Sin(start), -Mathf.Cos(start));
                        var radial = new Vector2(Mathf.Cos(start), Mathf.Sin(start));
                        var head = Sdf.Polygon(p, new[] { tip + radial * 0.26f - along * 0.06f, tip + along * 0.3f, tip - radial * 0.26f - along * 0.06f }) - 0.03f;
                        return Sdf.Union(arc, head);
                    };
                case "Trophy":
                    return p =>
                    {
                        var cup = Sdf.Intersect(Sdf.SmoothUnion(Sdf.Box(p, new Vector2(0f, 0.45f), new Vector2(0.44f, 0.28f), 0.06f), Sdf.Circle(p, new Vector2(0f, 0.2f), 0.38f), 0.05f), p.y - 0.73f);
                        var handles = Sdf.Union(Mathf.Abs(Sdf.Circle(p, new Vector2(-0.44f, 0.4f), 0.19f)) - 0.06f, Mathf.Abs(Sdf.Circle(p, new Vector2(0.44f, 0.4f), 0.19f)) - 0.06f);
                        var stem = Sdf.Box(p, new Vector2(0f, -0.28f), new Vector2(0.09f, 0.2f), 0.02f);
                        var foot = Sdf.Box(p, new Vector2(0f, -0.58f), new Vector2(0.38f, 0.11f), 0.05f);
                        return Sdf.Union(Sdf.Union(cup, handles), Sdf.Union(stem, foot));
                    };
                case "Grid":
                    return p =>
                    {
                        var d = float.MaxValue;
                        foreach (var x in new[] { -0.3f, 0.3f })
                        {
                            foreach (var y in new[] { -0.3f, 0.3f })
                            {
                                d = Sdf.Union(d, Sdf.Box(p, new Vector2(x, y), new Vector2(0.23f, 0.23f), 0.09f));
                            }
                        }

                        return d;
                    };
                case "Close":
                    return p => Sdf.Union(Sdf.Capsule(p, new Vector2(-0.45f, -0.45f), new Vector2(0.45f, 0.45f), 0.13f), Sdf.Capsule(p, new Vector2(-0.45f, 0.45f), new Vector2(0.45f, -0.45f), 0.13f));
                case "Crown":
                    return p =>
                    {
                        var body = Sdf.Polygon(p, new[]
                        {
                            new Vector2(-0.66f, -0.36f), new Vector2(-0.7f, 0.36f), new Vector2(-0.33f, 0.05f), new Vector2(0f, 0.5f),
                            new Vector2(0.33f, 0.05f), new Vector2(0.7f, 0.36f), new Vector2(0.66f, -0.36f),
                        }) - 0.05f;
                        var tips = Sdf.Union(Sdf.Circle(p, new Vector2(-0.7f, 0.44f), 0.11f), Sdf.Union(Sdf.Circle(p, new Vector2(0f, 0.6f), 0.11f), Sdf.Circle(p, new Vector2(0.7f, 0.44f), 0.11f)));
                        var band = Sdf.Box(p, new Vector2(0f, -0.52f), new Vector2(0.7f, 0.09f), 0.05f);
                        return Sdf.Union(Sdf.Union(body, tips), band);
                    };
                case "Star":
                    var star = ItemArtGenerator.StarVertices(Vector2.zero, 5, 0.82f, 0.38f, Mathf.PI * 0.5f);
                    return p => Sdf.Polygon(p, star) - 0.05f;
                case "Music":
                    return p =>
                    {
                        var heads = Sdf.Union(Sdf.Ellipse(p, new Vector2(-0.4f, -0.46f), new Vector2(0.22f, 0.16f)), Sdf.Ellipse(p, new Vector2(0.38f, -0.32f), new Vector2(0.22f, 0.16f)));
                        var stems = Sdf.Union(Sdf.Box(p, new Vector2(-0.24f, 0.04f), new Vector2(0.06f, 0.5f), 0f), Sdf.Box(p, new Vector2(0.54f, 0.18f), new Vector2(0.06f, 0.5f), 0f));
                        var beam = Sdf.Polygon(p, new[] { new Vector2(-0.3f, 0.4f), new Vector2(0.6f, 0.54f), new Vector2(0.6f, 0.76f), new Vector2(-0.3f, 0.62f) });
                        return Sdf.Union(heads, Sdf.Union(stems, beam));
                    };
                case "Speaker":
                    return p =>
                    {
                        var box = Sdf.Box(p, new Vector2(-0.52f, 0f), new Vector2(0.16f, 0.22f), 0.05f);
                        var cone = Sdf.Polygon(p, new[] { new Vector2(-0.42f, 0.22f), new Vector2(0.02f, 0.56f), new Vector2(0.02f, -0.56f), new Vector2(-0.42f, -0.22f) }) - 0.03f;
                        var waves = Sdf.Union(Sdf.Arc(p, new Vector2(0.04f, 0f), 0.32f, -45f * Mathf.Deg2Rad, 45f * Mathf.Deg2Rad, 0.11f), Sdf.Arc(p, new Vector2(0.04f, 0f), 0.6f, -50f * Mathf.Deg2Rad, 50f * Mathf.Deg2Rad, 0.11f));
                        return Sdf.Union(Sdf.Union(box, cone), waves);
                    };
                case "Lock":
                    return p =>
                    {
                        var body = Sdf.Box(p, new Vector2(0f, -0.26f), new Vector2(0.5f, 0.38f), 0.1f);
                        var shackle = Sdf.Union(Sdf.Arc(p, new Vector2(0f, 0.22f), 0.3f, 0f, Mathf.PI, 0.15f), Sdf.Union(Sdf.Box(p, new Vector2(-0.3f, 0.14f), new Vector2(0.075f, 0.1f), 0f), Sdf.Box(p, new Vector2(0.3f, 0.14f), new Vector2(0.075f, 0.1f), 0f)));
                        return Sdf.Subtract(Sdf.Union(body, shackle), Sdf.Union(Sdf.Circle(p, new Vector2(0f, -0.2f), 0.1f), Sdf.Box(p, new Vector2(0f, -0.36f), new Vector2(0.04f, 0.12f), 0f)));
                    };
                case "Check":
                    return p => Sdf.Union(Sdf.Capsule(p, new Vector2(-0.52f, 0.02f), new Vector2(-0.16f, -0.36f), 0.13f), Sdf.Capsule(p, new Vector2(-0.16f, -0.36f), new Vector2(0.56f, 0.44f), 0.13f));
                case "Video":
                    return p => Sdf.Subtract(Sdf.Box(p, Vector2.zero, new Vector2(0.74f, 0.52f), 0.16f), Sdf.Polygon(p, new[] { new Vector2(-0.16f, 0.26f), new Vector2(-0.16f, -0.26f), new Vector2(0.28f, 0f) }) - 0.04f);
                default:
                    throw new ArgumentException($"Unknown icon {name}");
            }
        }

        private static void EnsureAppIcon(bool overwrite)
        {
            if (overwrite || AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath) == null)
            {
                var a = new ArtCanvas(512, 512);
                var centre = ArtCanvas.Hex("#FF3EA5");
                var edge = ArtCanvas.Hex("#9C0F5C");
                a.Fill(p => -1f, p => Color.Lerp(centre, edge, Mathf.Clamp01(p.magnitude / 1.3f)));
                a.Fill(p => Mathf.Repeat(Mathf.Atan2(p.y, p.x) / (Mathf.PI * 2f) * 16f, 1f) < 0.5f ? -1f : 1f, ArtCanvas.WithAlpha(Saffron, 0.16f));
                a.Fill(p => Mathf.Abs(p.magnitude - 0.88f) - 0.035f, Saffron);
                for (var i = 0; i < 24; i++)
                {
                    var dot = Direction(i * 15f) * 0.88f;
                    a.Fill(p => Sdf.Circle(p, dot, 0.03f), i % 2 == 0 ? Navy : Teal);
                }

                a.Fill(p => Sdf.Circle(p, Vector2.zero, 0.7f), ArtCanvas.WithAlpha(Color.white, 0.12f), 0.2f);
                a.Offset = new Vector2(0f, -0.02f);
                a.Zoom = 0.66f;
                ItemArtGenerator.Draw("aam", a);
                var texture = a.ToTexture();
                File.WriteAllBytes(AppIconPath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(AppIconPath);
                var importer = (TextureImporter)AssetImporter.GetAtPath(AppIconPath);
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
            var current = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Any);
            if (current.Length == 0 || current[0] == null)
            {
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            }
        }

        private static Vector2 Direction(float degrees) => new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));

        private static Color Gray(float value) => new Color(value, value, value, 1f);
    }
}
