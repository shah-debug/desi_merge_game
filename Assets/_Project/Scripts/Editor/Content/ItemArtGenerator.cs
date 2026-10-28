using System;
using System.Collections.Generic;
using System.IO;
using DesiMerge.Configuration;
using UnityEditor;
using UnityEngine;

namespace DesiMerge.Editor.Content
{
    internal static class ItemArtGenerator
    {
        public const string Folder = "Assets/_Project/Art/Sprites/Items";
        public const int Size = 512;
        public const float SafeRadius = 0.985f;

        public static readonly Color Ink = ArtCanvas.Hex("#2A1A33");
        private static readonly Color White = Color.white;
        private static readonly Color Blush = ArtCanvas.WithAlpha(ArtCanvas.Hex("#FF6F91"), 0.55f);
        private const float Outline = 0.05f;

        private static readonly Dictionary<string, Action<ArtCanvas>> Drawers = new Dictionary<string, Action<ArtCanvas>>
        {
            { "patti", Patti },
            { "nimbu", Nimbu },
            { "pyaz", Pyaz },
            { "tamatar", Tamatar },
            { "roti", Roti },
            { "aam", Aam },
            { "chai", Chai },
            { "mango_shake", MangoShake },
            { "rickshaw", Rickshaw },
            { "bus", Bus },
        };

        public static bool HasArtFor(string id) => Drawers.ContainsKey(id);

        public static string PathFor(int level, string id) => $"{Folder}/{level + 1:00}_{id}.png";

        public static void Draw(string id, ArtCanvas canvas) => Drawers[id](canvas);

        public static void EnsureAll(bool overwrite)
        {
            var progression = AssetDatabase.LoadAssetAtPath<MergeProgression>(ContentPaths.MergeProgressionPath);
            if (progression == null)
            {
                Debug.LogError("[Setup] No MergeProgression asset; item art not generated.");
                return;
            }

            ContentPaths.EnsureFolder(Folder);
            var placeholder = AssetDatabase.LoadAssetAtPath<Sprite>(GameplaySetup.CircleSpritePath);
            for (var level = 0; level < progression.Count; level++)
            {
                var definition = progression.Get(level);
                if (definition == null || !HasArtFor(definition.Id))
                {
                    Debug.LogWarning($"[Setup] No generated art for level {level} ('{definition?.Id}'); it keeps its current sprite.");
                    continue;
                }

                var path = PathFor(level, definition.Id);
                if (overwrite || AssetDatabase.LoadAssetAtPath<Sprite>(path) == null)
                {
                    var canvas = new ArtCanvas(Size, Size);
                    canvas.Zoom = 0.97f;
                    Draw(definition.Id, canvas);
                    WriteSprite(path, canvas.ToTexture(), Size);
                }

                if (definition.Sprite == null || definition.Sprite == placeholder)
                {
                    EditorRefs.Set(definition, ("sprite", AssetDatabase.LoadAssetAtPath<Sprite>(path)), ("tintSprite", false));
                    EditorUtility.SetDirty(definition);
                }
            }
        }

        private static void WriteSprite(string path, Texture2D texture, int pixelsPerUnit)
        {
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.mipmapEnabled = true;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = Size;
            importer.textureCompression = TextureImporterCompression.Compressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.Tight;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static Vector2 V(float x, float y) => new Vector2(x, y);

        private static Color C(string hex) => ArtCanvas.Hex(hex);

        private static void Face(ArtCanvas a, Vector2 centre, float scale, float spacing = 0.22f)
        {
            foreach (var side in new[] { -1f, 1f })
            {
                var eye = centre + V(side * spacing * scale, 0.04f * scale);
                a.Fill(p => Sdf.Ellipse(p, eye, V(0.062f, 0.082f) * scale), Ink);
                a.Fill(p => Sdf.Circle(p, eye + V(-0.018f, 0.03f) * scale, 0.024f * scale), White);
                a.Fill(p => Sdf.Ellipse(p, centre + V(side * (spacing + 0.075f) * scale, -0.075f * scale), V(0.068f, 0.04f) * scale), Blush);
            }

            a.Fill(p => Sdf.Arc(p, centre + V(0f, -0.01f * scale), 0.08f * scale, 205f * Mathf.Deg2Rad, 335f * Mathf.Deg2Rad, 0.03f * scale), Ink);
        }

        private static void Gloss(ArtCanvas a, Vector2 centre, Vector2 radii, float degrees, float alpha = 0.55f)
        {
            a.FillInside(p => Sdf.Ellipse(Sdf.Rotate(p, centre, -degrees), centre, radii), ArtCanvas.WithAlpha(White, alpha), 0.01f);
        }

        private static void Patti(ArtCanvas a)
        {
            float Leaf(Vector2 p)
            {
                var q = Sdf.Rotate(p, Vector2.zero, -12f);
                var lobes = Sdf.SmoothUnion(Sdf.Circle(q, V(-0.37f, 0.22f), 0.52f), Sdf.Circle(q, V(0.37f, 0.22f), 0.52f), 0.08f);
                return Sdf.SmoothUnion(lobes, Sdf.Polygon(q, new[] { V(-0.86f, 0.12f), V(0.86f, 0.12f), V(0f, -0.9f) }), 0.16f);
            }

            var stemTop = Sdf.Rotate(V(0.04f, 0.92f), Vector2.zero, 12f);
            var stemBase = Sdf.Rotate(V(0f, 0.5f), Vector2.zero, 12f);
            a.Fill(p => Sdf.Capsule(p, stemBase, stemTop, 0.075f), Ink);
            a.Fill(p => Sdf.Capsule(p, stemBase, stemTop, 0.04f), C("#3E9A3A"));
            a.Body(Leaf, Outline, Ink, C("#A6E35F"), C("#2E8B3E"));

            var veinColor = ArtCanvas.WithAlpha(C("#D9F7A8"), 0.85f);
            Vector2 R(float x, float y) => Sdf.Rotate(V(x, y), Vector2.zero, 12f);
            a.FillInside(p => Sdf.TaperedCapsule(p, R(0f, 0.55f), R(0f, -0.82f), 0.028f, 0.008f), veinColor);
            foreach (var side in new[] { -1f, 1f })
            {
                a.FillInside(p => Sdf.TaperedCapsule(p, R(0f, 0.32f), R(side * 0.5f, 0.5f), 0.02f, 0.006f), veinColor);
                a.FillInside(p => Sdf.TaperedCapsule(p, R(0f, 0.08f), R(side * 0.62f, 0.2f), 0.02f, 0.006f), veinColor);
                a.FillInside(p => Sdf.TaperedCapsule(p, R(0f, -0.3f), R(side * 0.42f, -0.18f), 0.018f, 0.006f), veinColor);
            }

            Gloss(a, V(-0.42f, 0.42f), V(0.16f, 0.08f), 35f);
            Face(a, R(0f, -0.12f), 0.95f, 0.2f);
        }

        private static void Nimbu(ArtCanvas a)
        {
            float Fruit(Vector2 p)
            {
                var q = Sdf.Rotate(p, Vector2.zero, 20f);
                var body = Sdf.Ellipse(q, Vector2.zero, V(0.8f, 0.7f));
                var tips = Sdf.Union(Sdf.Circle(q, V(0.82f, 0f), 0.11f), Sdf.Circle(q, V(-0.83f, 0f), 0.09f));
                return Sdf.SmoothUnion(body, tips, 0.14f);
            }

            a.Body(Fruit, Outline, Ink, C("#F6F77A"), C("#9DBF2C"));
            var pore = ArtCanvas.WithAlpha(C("#FFFFC8"), 0.5f);
            foreach (var spot in new[] { V(0.42f, 0.3f), V(0.55f, -0.12f), V(-0.5f, -0.3f), V(0.25f, -0.5f), V(-0.2f, 0.5f), V(0.6f, 0.15f), V(-0.62f, 0.05f) })
            {
                a.FillInside(p => Sdf.Circle(p, spot, 0.025f), pore);
            }

            Gloss(a, V(-0.36f, 0.32f), V(0.22f, 0.09f), 28f);

            float LeafShape(Vector2 p) => Sdf.Ellipse(Sdf.Rotate(p, V(-0.2f, 0.72f), 25f), V(-0.2f, 0.72f), V(0.2f, 0.085f));
            a.Body(LeafShape, 0.035f, Ink, C("#7ED957"), C("#2E8B3E"), 0.08f);
            Face(a, V(0.02f, -0.06f), 1.05f);
        }

        private static void Pyaz(ArtCanvas a)
        {
            float Bulb(Vector2 p)
            {
                var round = Sdf.Circle(p, V(0f, -0.12f), 0.78f);
                var tip = Sdf.Polygon(p, new[] { V(-0.3f, 0.42f), V(0.3f, 0.42f), V(0.02f, 0.9f) });
                return Sdf.SmoothUnion(round, tip, 0.22f);
            }

            a.Body(Bulb, Outline, Ink, C("#E08AD0"), C("#7A2675"));
            var stripe = ArtCanvas.WithAlpha(C("#F7C4EE"), 0.55f);
            foreach (var rx in new[] { 0.18f, 0.42f, 0.64f })
            {
                a.FillInside(p => Mathf.Abs(Sdf.Ellipse(p, V(0f, -0.1f), V(rx, 0.86f))) - 0.012f, stripe);
            }

            a.FillInside(p => Sdf.Circle(p, V(0.02f, 1.02f), 0.38f), ArtCanvas.WithAlpha(C("#9C5A3C"), 0.7f), 0.04f);
            Gloss(a, V(-0.42f, 0.12f), V(0.2f, 0.1f), 50f);
            Face(a, V(0f, -0.22f), 1.05f);
        }

        private static void Tamatar(ArtCanvas a)
        {
            float Fruit(Vector2 p)
            {
                var body = Sdf.Ellipse(p, V(0f, -0.08f), V(0.9f, 0.82f));
                var dip = Sdf.Circle(p, V(0f, 0.86f), 0.12f);
                return Sdf.Subtract(body, dip);
            }

            a.Body(Fruit, Outline, Ink, C("#FF7A63"), C("#C41E2E"));
            Gloss(a, V(-0.45f, 0.25f), V(0.2f, 0.1f), 35f);

            var star = StarVertices(V(0f, 0f), 5, 0.42f, 0.14f, Mathf.PI * 0.5f);
            float Calyx(Vector2 p) => Sdf.Polygon(V(p.x, (p.y - 0.66f) / 0.55f), star) * 0.55f - 0.02f;
            a.Body(Calyx, 0.035f, Ink, C("#7AD65A"), C("#2E7D32"), 0.06f);
            a.Fill(p => Sdf.Capsule(p, V(0f, 0.68f), V(0.05f, 0.88f), 0.07f), Ink);
            a.Fill(p => Sdf.Capsule(p, V(0f, 0.68f), V(0.05f, 0.88f), 0.04f), C("#3E8E3A"));
            Face(a, V(0f, -0.2f), 1.1f);
        }

        private static void Roti(ArtCanvas a)
        {
            float Bread(Vector2 p)
            {
                var angle = Mathf.Atan2(p.y, p.x);
                var radius = 0.9f + 0.022f * Mathf.Sin(angle * 5f) + 0.014f * Mathf.Sin(angle * 9f + 1f);
                return p.magnitude - radius;
            }

            a.Body(Bread, Outline, Ink, C("#FBE3B4"), C("#D49A55"), 0.3f);
            var light = ArtCanvas.WithAlpha(C("#A9652E"), 0.45f);
            var dark = ArtCanvas.WithAlpha(C("#7A4421"), 0.6f);
            var spots = new[]
            {
                (V(-0.5f, 0.42f), 0.1f, light), (V(-0.38f, 0.5f), 0.05f, dark), (V(0.45f, 0.5f), 0.12f, light),
                (V(0.55f, 0.38f), 0.05f, dark), (V(0.62f, -0.2f), 0.09f, light), (V(-0.66f, -0.12f), 0.08f, light),
                (V(-0.4f, -0.58f), 0.11f, light), (V(-0.3f, -0.62f), 0.045f, dark), (V(0.32f, -0.6f), 0.07f, light),
                (V(0.05f, 0.66f), 0.06f, light), (V(0.7f, 0.08f), 0.035f, dark), (V(-0.72f, 0.2f), 0.035f, dark),
            };
            foreach (var (centre, radius, color) in spots)
            {
                a.FillInside(p => Sdf.Ellipse(p, centre, V(radius * 1.25f, radius)), color, 0.03f);
            }

            a.FillInside(p => Mathf.Abs(p.magnitude - 0.74f) - 0.012f, ArtCanvas.WithAlpha(C("#C98A4B"), 0.35f), 0.02f);
            Gloss(a, V(-0.3f, 0.3f), V(0.3f, 0.12f), 25f, 0.3f);
            Face(a, V(0f, -0.06f), 1.15f);
        }

        private static void Aam(ArtCanvas a)
        {
            float Fruit(Vector2 p)
            {
                var q = Sdf.Rotate(p, Vector2.zero, -8f);
                return Sdf.SmoothUnion(Sdf.Circle(q, V(0.08f, -0.15f), 0.76f), Sdf.Circle(q, V(-0.22f, 0.38f), 0.47f), 0.3f);
            }

            a.Body(Fruit, Outline, Ink, C("#FFE066"), C("#EE7A12"));
            a.FillInside(p => Sdf.Ellipse(p, V(0.38f, -0.32f), V(0.42f, 0.36f)), ArtCanvas.WithAlpha(C("#E8452C"), 0.35f), 0.25f);
            Gloss(a, V(-0.4f, 0.38f), V(0.16f, 0.09f), 50f);

            var stemA = Sdf.Rotate(V(-0.24f, 0.78f), Vector2.zero, 8f);
            var stemB = Sdf.Rotate(V(-0.16f, 0.93f), Vector2.zero, 8f);
            a.Fill(p => Sdf.Capsule(p, stemA, stemB, 0.06f), Ink);
            a.Fill(p => Sdf.Capsule(p, stemA, stemB, 0.03f), C("#7A4A2A"));
            float LeafShape(Vector2 p) => Sdf.Ellipse(Sdf.Rotate(p, V(0.05f, 0.8f), -18f), V(0.05f, 0.8f), V(0.22f, 0.085f));
            a.Body(LeafShape, 0.035f, Ink, C("#7ED957"), C("#2E8B3E"), 0.08f);
            Face(a, V(0.02f, -0.16f), 1.15f);
        }

        private static void Chai(ArtCanvas a)
        {
            float Saucer(Vector2 p) => Sdf.Ellipse(p, V(0f, -0.6f), V(0.72f, 0.15f));
            a.Body(Saucer, 0.045f, Ink, C("#FFFFFF"), C("#C9C0D6"), 0.1f);
            a.FillInside(p => Mathf.Abs(Sdf.Ellipse(p, V(0f, -0.6f), V(0.56f, 0.1f))) - 0.018f, C("#12A08C"));

            float Handle(Vector2 p) => Mathf.Abs(Sdf.Circle(p, V(0.66f, -0.1f), 0.18f)) - 0.055f;
            a.Body(Handle, 0.04f, Ink, C("#FFFFFF"), C("#D3CBE0"), 0.05f);

            var cup = new[] { V(-0.64f, 0.3f), V(0.64f, 0.3f), V(0.4f, -0.58f), V(-0.4f, -0.58f) };
            float Cup(Vector2 p) => Sdf.Polygon(p, cup) - 0.06f;
            a.Body(Cup, 0.045f, Ink, C("#FFFFFF"), C("#CFC6DD"), 0.16f);
            a.FillInside(p => Sdf.Box(p, V(0f, -0.4f), V(0.8f, 0.065f), 0f), C("#12A08C"));
            for (var i = -2; i <= 2; i++)
            {
                var dot = V(i * 0.17f, -0.4f);
                var color = i % 2 == 0 ? C("#FFB300") : C("#E0218A");
                a.FillInside(p => Sdf.Circle(p, dot, 0.035f), color);
            }

            a.Fill(p => Sdf.Ellipse(p, V(0f, 0.34f), V(0.71f, 0.12f)), Ink);
            a.Fill(p => Sdf.Ellipse(p, V(0f, 0.34f), V(0.66f, 0.085f)), C("#E9C49A"));
            a.Fill(p => Sdf.Ellipse(p, V(0f, 0.33f), V(0.56f, 0.06f)), C("#B9733F"));
            a.FillInside(p => Sdf.Capsule(p, V(-0.5f, 0.12f), V(-0.4f, -0.36f), 0.035f), ArtCanvas.WithAlpha(White, 0.85f));

            foreach (var x in new[] { -0.22f, 0.18f })
            {
                float Steam(Vector2 p)
                {
                    var lower = Sdf.Arc(p, V(x + 0.09f, 0.56f), 0.1f, 90f * Mathf.Deg2Rad, 270f * Mathf.Deg2Rad, 0.06f);
                    var upper = Sdf.Arc(p, V(x + 0.09f, 0.76f), 0.1f, -90f * Mathf.Deg2Rad, 90f * Mathf.Deg2Rad, 0.06f);
                    return Sdf.Union(lower, upper);
                }

                a.Fill(p => Steam(p) - 0.022f, ArtCanvas.WithAlpha(Ink, 0.85f));
                a.Fill(Steam, C("#FFFDF7"));
            }

            Face(a, V(0f, -0.1f), 1.0f);
        }

        private static void MangoShake(ArtCanvas a)
        {
            var glass = new[] { V(-0.68f, 0.36f), V(0.68f, 0.36f), V(0.42f, -0.8f), V(-0.42f, -0.8f) };
            float Glass(Vector2 p) => Sdf.Polygon(p, glass) - 0.05f;

            a.Fill(p => Sdf.Capsule(p, V(0.1f, 0.42f), V(0.3f, 0.84f), 0.09f), Ink);
            a.Fill(p => Sdf.Capsule(p, V(0.1f, 0.42f), V(0.3f, 0.84f), 0.055f), C("#E0218A"));
            var axis = (V(0.3f, 0.84f) - V(0.1f, 0.42f)).normalized;
            a.FillInside(p => Mathf.Repeat(Vector2.Dot(p, axis) * 9f, 1f) < 0.5f ? -1f : 1f, ArtCanvas.WithAlpha(White, 0.9f));

            a.Fill(p => Glass(p) - Outline, Ink);
            a.Fill(Glass, ArtCanvas.WithAlpha(C("#FFF6E8"), 1f));
            a.Fill(p => Sdf.Intersect(Glass(p) + 0.06f, p.y - 0.26f), p => Color.Lerp(C("#FF9A1F"), C("#FFD045"), Mathf.InverseLerp(-0.8f, 0.3f, p.y)));
            a.FillInside(p => Sdf.Box(p, V(0f, -0.62f), V(0.8f, 0.035f), 0f), ArtCanvas.WithAlpha(C("#E0218A"), 0.9f));
            a.FillInside(p => Sdf.Box(p, V(0f, -0.53f), V(0.8f, 0.018f), 0f), ArtCanvas.WithAlpha(C("#12A08C"), 0.9f));
            a.FillInside(p => Sdf.Capsule(p, V(-0.5f, 0.2f), V(-0.4f, -0.5f), 0.032f), ArtCanvas.WithAlpha(White, 0.75f));

            float Cream(Vector2 p)
            {
                var d = Sdf.Circle(p, V(-0.42f, 0.4f), 0.18f);
                d = Sdf.SmoothUnion(d, Sdf.Circle(p, V(-0.15f, 0.46f), 0.22f), 0.05f);
                d = Sdf.SmoothUnion(d, Sdf.Circle(p, V(0.15f, 0.46f), 0.21f), 0.05f);
                d = Sdf.SmoothUnion(d, Sdf.Circle(p, V(0.42f, 0.4f), 0.18f), 0.05f);
                return Sdf.SmoothUnion(d, Sdf.Circle(p, V(0f, 0.58f), 0.17f), 0.06f);
            }

            a.Body(Cream, 0.045f, Ink, C("#FFFFFF"), C("#EBD3B0"), 0.12f);
            float Slice(Vector2 p) => Sdf.Circle(p, V(-0.66f, 0.36f), 0.16f);
            a.Body(Slice, 0.04f, Ink, C("#FFE066"), C("#F29A1E"), 0.08f);
            a.FillInside(p => Mathf.Abs(Sdf.Circle(p, V(-0.66f, 0.36f), 0.1f)) - 0.012f, ArtCanvas.WithAlpha(White, 0.6f));
            Face(a, V(0f, -0.2f), 1.05f);
        }

        private static void Rickshaw(ArtCanvas a)
        {
            foreach (var x in new[] { -0.5f, 0.5f })
            {
                a.Fill(p => Sdf.Circle(p, V(x, -0.6f), 0.19f), Ink);
                a.Fill(p => Sdf.Circle(p, V(x, -0.6f), 0.15f), C("#3B3550"));
                a.Fill(p => Sdf.Circle(p, V(x, -0.6f), 0.06f), C("#B8B4C8"));
            }

            a.Fill(p => Sdf.Circle(p, V(0f, -0.74f), 0.2f), Ink);
            a.Fill(p => Sdf.Circle(p, V(0f, -0.74f), 0.16f), C("#3B3550"));
            a.Fill(p => Sdf.Circle(p, V(0f, -0.74f), 0.06f), C("#B8B4C8"));

            float Lower(Vector2 p) => Sdf.Box(p, V(0f, -0.26f), V(0.64f, 0.34f), 0.22f);
            a.Body(Lower, Outline, Ink, C("#2FD3A8"), C("#0B8A6E"));
            a.Fill(p => Sdf.Box(p, V(0f, -0.56f), V(0.2f, 0.08f), 0.05f), Ink);
            a.Fill(p => Sdf.Box(p, V(0f, -0.56f), V(0.16f, 0.05f), 0.04f), C("#C8C6D6"));

            foreach (var x in new[] { -0.42f, 0.42f })
            {
                TruckFlower(a, V(x, -0.3f), 0.13f, C("#E0218A"), C("#FFC233"));
            }

            a.Fill(p => Sdf.Circle(p, V(0f, -0.22f), 0.15f), Ink);
            a.Fill(p => Sdf.Circle(p, V(0f, -0.22f), 0.12f), p => Color.Lerp(C("#FFF7C2"), C("#FFC233"), Mathf.InverseLerp(0.1f, -0.34f, p.y)));
            a.Fill(p => Sdf.Circle(p, V(-0.04f, -0.18f), 0.035f), White);

            float Canopy(Vector2 p) => Sdf.Intersect(Sdf.Ellipse(p, V(0f, 0.12f), V(0.88f, 0.78f)), 0.04f - p.y);
            a.Body(Canopy, Outline, Ink, C("#3E3863"), C("#1C1833"), 0.18f);
            a.FillInside(p => Mathf.Abs(Sdf.Ellipse(p, V(0f, 0.12f), V(0.76f, 0.66f))) - 0.022f, C("#FFC233"));
            for (var i = 0; i < 7; i++)
            {
                var angle = Mathf.Lerp(25f, 155f, i / 6f) * Mathf.Deg2Rad;
                var dot = V(0f, 0.12f) + V(Mathf.Cos(angle) * 0.76f, Mathf.Sin(angle) * 0.66f);
                a.FillInside(p => Sdf.Circle(p, dot, 0.035f), i % 2 == 0 ? C("#E0218A") : C("#2FD3A8"));
            }

            for (var i = 0; i < 9; i++)
            {
                var x = Mathf.Lerp(-0.72f, 0.72f, i / 8f);
                var tassel = new[] { V(x - 0.07f, 0.07f), V(x + 0.07f, 0.07f), V(x, -0.06f) };
                var color = i % 2 == 0 ? C("#E0218A") : C("#FFC233");
                a.Fill(p => Sdf.Polygon(p, tassel) - 0.02f, Ink);
                a.Fill(p => Sdf.Polygon(p, tassel), color);
            }

            float Windshield(Vector2 p) => Sdf.Box(p, V(0f, 0.34f), V(0.5f, 0.24f), 0.16f);
            a.Fill(p => Windshield(p) - 0.035f, Ink);
            a.Fill(Windshield, p => Color.Lerp(C("#D9F6FF"), C("#7CC8E0"), Mathf.InverseLerp(0.58f, 0.1f, p.y)));
            a.Fill(p => Sdf.Capsule(p, V(-0.34f, 0.18f), V(-0.12f, 0.5f), 0.025f), ArtCanvas.WithAlpha(White, 0.7f));
            Face(a, V(0.04f, 0.32f), 1.0f, 0.2f);
        }

        private static void Bus(ArtCanvas a)
        {
            foreach (var x in new[] { -0.36f, 0.36f })
            {
                a.Fill(p => Sdf.Box(p, V(x, -0.72f), V(0.12f, 0.1f), 0.05f), Ink);
                a.Fill(p => Sdf.Box(p, V(x, -0.72f), V(0.085f, 0.07f), 0.04f), C("#3B3550"));
            }

            float Body(Vector2 p) => Sdf.Box(p, V(0f, -0.02f), V(0.7f, 0.74f), 0.28f);
            a.Body(Body, Outline, Ink, C("#FFD45C"), C("#E89B12"));
            a.FillInside(p => 0.42f - p.y, C("#12A08C"));
            for (var i = -3; i <= 3; i++)
            {
                var centre = V(i * 0.2f, 0.42f);
                a.FillInside(p => Sdf.Circle(p, centre, 0.1f), C("#12A08C"));
            }

            a.FillInside(p => Mathf.Abs(p.y - 0.42f + 0.06f * Mathf.Cos(p.x * Mathf.PI * 5f)) - 0.018f, C("#FFC233"));
            TruckFlower(a, V(0f, 0.6f), 0.12f, C("#E0218A"), C("#FFC233"));
            foreach (var x in new[] { -0.36f, 0.36f })
            {
                a.FillInside(p => Sdf.Circle(p, V(x, 0.6f), 0.05f), White);
                a.FillInside(p => Sdf.Circle(p, V(x, 0.6f), 0.025f), C("#E0218A"));
            }

            a.FillInside(p => Sdf.Box(p, V(0f, -0.14f), V(0.9f, 0.045f), 0f), C("#E0218A"));
            for (var i = -4; i <= 4; i++)
            {
                var dot = V(i * 0.14f, -0.14f);
                a.FillInside(p => Sdf.Circle(p, dot, 0.022f), White);
            }

            float Glass(Vector2 p) => Sdf.Box(p, V(0f, 0.15f), V(0.56f, 0.2f), 0.1f);
            a.Fill(p => Glass(p) - 0.035f, Ink);
            a.Fill(Glass, p => Color.Lerp(C("#D9F6FF"), C("#6DBBD8"), Mathf.InverseLerp(0.35f, -0.05f, p.y)));
            a.Fill(p => Sdf.Box(p, V(0f, 0.15f), V(0.025f, 0.2f), 0f), Ink);
            a.Fill(p => Sdf.Capsule(p, V(-0.46f, 0.03f), V(-0.3f, 0.27f), 0.022f), ArtCanvas.WithAlpha(White, 0.7f));
            a.Fill(p => Sdf.Capsule(p, V(0.12f, 0.03f), V(0.26f, 0.25f), 0.022f), ArtCanvas.WithAlpha(White, 0.7f));

            foreach (var x in new[] { -0.46f, 0.46f })
            {
                a.Fill(p => Sdf.Circle(p, V(x, -0.36f), 0.13f), Ink);
                a.Fill(p => Sdf.Circle(p, V(x, -0.36f), 0.1f), p => Color.Lerp(C("#FFFBE0"), C("#FFC233"), Mathf.InverseLerp(-0.28f, -0.46f, p.y)));
                a.Fill(p => Sdf.Circle(p, V(x - 0.03f, -0.33f), 0.03f), White);
            }

            a.Fill(p => Sdf.Box(p, V(0f, -0.38f), V(0.24f, 0.1f), 0.06f), Ink);
            a.Fill(p => Sdf.Box(p, V(0f, -0.38f), V(0.2f, 0.065f), 0.05f), C("#3B3550"));
            a.Fill(p => Sdf.Arc(p, V(0f, -0.33f), 0.14f, 205f * Mathf.Deg2Rad, 335f * Mathf.Deg2Rad, 0.03f), C("#FFF4D6"));

            float Bumper(Vector2 p) => Sdf.Box(p, V(0f, -0.6f), V(0.66f, 0.075f), 0.06f);
            a.Body(Bumper, 0.035f, Ink, C("#F4F5FA"), C("#9AA0B5"), 0.05f);

            foreach (var side in new[] { -1f, 1f })
            {
                var eye = V(side * 0.27f, 0.17f);
                a.Fill(p => Sdf.Ellipse(p, eye, V(0.062f, 0.082f)), Ink);
                a.Fill(p => Sdf.Circle(p, eye + V(-0.018f, 0.03f), 0.024f), White);
                a.Fill(p => Sdf.Ellipse(p, V(side * 0.42f, 0.06f), V(0.06f, 0.035f)), Blush);
            }
        }

        private static void TruckFlower(ArtCanvas a, Vector2 centre, float radius, Color petal, Color heart)
        {
            for (var i = 0; i < 6; i++)
            {
                var angle = i * 60f;
                var offset = V(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * radius * 0.55f;
                float Petal(Vector2 p) => Sdf.Ellipse(Sdf.Rotate(p, centre + offset, -angle), centre + offset, V(radius * 0.5f, radius * 0.3f));
                a.Fill(p => Petal(p) - radius * 0.12f, Ink);
                a.Fill(Petal, petal);
            }

            a.Fill(p => Sdf.Circle(p, centre, radius * 0.32f), Ink);
            a.Fill(p => Sdf.Circle(p, centre, radius * 0.22f), heart);
        }

        public static Vector2[] StarVertices(Vector2 centre, int points, float outer, float inner, float rotation)
        {
            var vertices = new Vector2[points * 2];
            for (var i = 0; i < vertices.Length; i++)
            {
                var angle = rotation + i * Mathf.PI / points;
                var radius = i % 2 == 0 ? outer : inner;
                vertices[i] = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }

            return vertices;
        }
    }
}
