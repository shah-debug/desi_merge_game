using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DesiMerge.Editor.Content
{
    /// <summary>
    /// Original artwork for the item skins (10 items each, same sizes and pivots as the default items so they fit the
    /// same colliders): Mithai, Street Food, Truck Art (each default item in a truck-art medallion) and Numbers.
    /// </summary>
    internal static class SkinArtGenerator
    {
        public const string Folder = "Assets/_Project/Art/Sprites/Skins";
        private const int Size = 512;
        private static readonly Color Ink = ItemArtGenerator.Ink;
        private static readonly Color White = Color.white;

        public sealed class Skin
        {
            public string Id;
            public string Title;
            public string[] Keys;
            public string[] Names;
            public Action<int, ArtCanvas> Draw;
        }

        public static readonly string[] DefaultIds = { "patti", "nimbu", "pyaz", "tamatar", "roti", "aam", "chai", "mango_shake", "rickshaw", "bus" };

        public static readonly Skin[] Skins =
        {
            new Skin
            {
                Id = "mithai", Title = "Mithai",
                Keys = new[] { "peda", "ladoo", "gulab_jamun", "barfi", "jalebi", "cham_cham", "rasmalai", "kheer", "mithai_box", "mithai_thaal" },
                Names = new[] { "Peda", "Ladoo", "Gulab Jamun", "Barfi", "Jalebi", "Cham Cham", "Rasmalai", "Kheer", "Mithai Box", "Mithai Thaal" },
                Draw = DrawMithai,
            },
            new Skin
            {
                Id = "street_food", Title = "Street Food",
                Keys = new[] { "pakora", "samosa", "gol_gappa", "chaat", "bun_kabab", "paratha_roll", "seekh_kabab", "nihari", "biryani", "food_cart" },
                Names = new[] { "Pakora", "Samosa", "Gol Gappa", "Chaat", "Bun Kabab", "Paratha Roll", "Seekh Kabab", "Nihari", "Biryani", "Food Cart" },
                Draw = DrawStreetFood,
            },
            new Skin
            {
                Id = "truck_art", Title = "Truck Art",
                Keys = DefaultIds,
                Names = new[] { "Patti", "Nimbu", "Pyaz", "Tamatar", "Roti", "Aam", "Chai", "Mango Shake", "Rickshaw", "Bus" },
                Draw = DrawTruckArt,
            },
            new Skin
            {
                Id = "numbers", Title = "Numbers",
                Keys = new[] { "2", "4", "8", "16", "32", "64", "128", "256", "512", "1024" },
                Names = new[] { "2", "4", "8", "16", "32", "64", "128", "256", "512", "1024" },
                Draw = DrawNumber,
            },
        };

        public static string PathFor(Skin skin, int level) => $"{Folder}/{skin.Id}/{level + 1:00}_{skin.Keys[level]}.png";

        public static Sprite Load(Skin skin, int level) => AssetDatabase.LoadAssetAtPath<Sprite>(PathFor(skin, level));

        public static void EnsureAll(bool overwrite)
        {
            foreach (var skin in Skins)
            {
                ContentPaths.EnsureFolder($"{Folder}/{skin.Id}");
                for (var level = 0; level < 10; level++)
                {
                    var path = PathFor(skin, level);
                    if (!overwrite && AssetDatabase.LoadAssetAtPath<Sprite>(path) != null)
                    {
                        continue;
                    }

                    var canvas = new ArtCanvas(Size, Size) { Zoom = 0.97f };
                    skin.Draw(level, canvas);
                    ItemArtGenerator.WriteSprite(path, canvas.ToTexture(), Size);
                }
            }
        }

        private static Vector2 V(float x, float y) => new Vector2(x, y);

        private static void Face(ArtCanvas a, Vector2 c, float s) => ItemArtGenerator.Face(a, c, s);

        private static void Gloss(ArtCanvas a, Vector2 c, Vector2 r, float deg = -30f, float alpha = 0.5f) => ItemArtGenerator.Gloss(a, c, r, deg, alpha);

        private static float Hash(int i) => Mathf.Repeat(Mathf.Sin(i * 127.1f + 311.7f) * 43758.5453f, 1f);

        /// <summary>Speckles inside a shape (seeds, crumbs, coconut).</summary>
        private static void Speckle(ArtCanvas a, Func<Vector2, float> inside, int count, float radius, float size, Color color, int seed)
        {
            for (var i = 0; i < count; i++)
            {
                var angle = Hash(seed + i * 3) * Mathf.PI * 2f;
                var distance = Mathf.Sqrt(Hash(seed + i * 3 + 1)) * radius;
                var centre = V(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                var s = size * (0.7f + Hash(seed + i * 3 + 2) * 0.6f);
                a.FillInside(p => Mathf.Max(inside(p), Sdf.Ellipse(Sdf.Rotate(p, centre, angle * Mathf.Rad2Deg), centre, V(s, s * 0.6f))), color);
            }
        }

        private static float Bowl(Vector2 p, float top, float width, float depth) =>
            Mathf.Max(Sdf.Ellipse(p, V(0f, top), V(width, depth)), p.y - top);

        // ---------------- Mithai ----------------

        private static void DrawMithai(int level, ArtCanvas a)
        {
            switch (level)
            {
                case 0:
                {
                    Func<Vector2, float> peda = p => Sdf.Ellipse(p, V(0f, -0.04f), V(0.9f, 0.66f));
                    a.Body(peda, 0.05f, Ink, ArtCanvas.Hex("#F6DFAE"), ArtCanvas.Hex("#C7934E"));
                    a.FillInside(p => Sdf.Ellipse(p, V(0f, 0.2f), V(0.36f, 0.15f)), ArtCanvas.WithAlpha(ArtCanvas.Hex("#B07A3A"), 0.45f), 0.03f);
                    a.Body(p => Sdf.Ellipse(Sdf.Rotate(p, V(0f, 0.22f), 25f), V(0f, 0.22f), V(0.12f, 0.06f)), 0.025f, Ink, ArtCanvas.Hex("#8FD14F"), ArtCanvas.Hex("#4E9A2A"));
                    Face(a, V(0f, -0.24f), 0.95f);
                    break;
                }
                case 1:
                {
                    Func<Vector2, float> ball = p => Sdf.Circle(p, Vector2.zero, 0.9f);
                    a.Body(ball, 0.05f, Ink, ArtCanvas.Hex("#FFC93D"), ArtCanvas.Hex("#E06D00"));
                    Speckle(a, ball, 46, 0.82f, 0.07f, ArtCanvas.WithAlpha(ArtCanvas.Hex("#FFE79A"), 0.85f), 11);
                    Speckle(a, ball, 24, 0.82f, 0.05f, ArtCanvas.WithAlpha(ArtCanvas.Hex("#C25400"), 0.6f), 77);
                    Gloss(a, V(-0.34f, 0.42f), V(0.26f, 0.14f));
                    Face(a, V(0f, -0.12f), 1.05f);
                    break;
                }
                case 2:
                {
                    a.Fill(p => Sdf.Ellipse(p, V(0f, -0.66f), V(0.68f, 0.16f)), ArtCanvas.WithAlpha(ArtCanvas.Hex("#D9821F"), 0.75f), 0.02f);
                    Func<Vector2, float> jamun = p => Sdf.Ellipse(p, V(0f, 0.02f), V(0.86f, 0.8f));
                    a.Body(jamun, 0.05f, Ink, ArtCanvas.Hex("#A9521F"), ArtCanvas.Hex("#4A1A08"));
                    Gloss(a, V(-0.36f, 0.42f), V(0.3f, 0.15f), -30f, 0.65f);
                    a.FillInside(p => Sdf.Ellipse(p, V(0.42f, -0.4f), V(0.12f, 0.06f)), ArtCanvas.WithAlpha(White, 0.35f));
                    Face(a, V(0f, -0.08f), 1.05f);
                    break;
                }
                case 3:
                {
                    Func<Vector2, float> barfi = p => Sdf.Polygon(p, new[] { V(0f, 0.9f), V(0.9f, 0f), V(0f, -0.9f), V(-0.9f, 0f) }) - 0.04f;
                    a.Body(barfi, 0.05f, Ink, ArtCanvas.Hex("#FFF7E4"), ArtCanvas.Hex("#DCC593"));
                    a.FillInside(p => Mathf.Max(barfi(p), Sdf.Polygon(p, new[] { V(-0.35f, 0.42f), V(0.1f, 0.62f), V(0.25f, 0.3f), V(-0.2f, 0.2f) })), ArtCanvas.WithAlpha(ArtCanvas.Hex("#D9E2EC"), 0.85f));
                    a.FillInside(p => Mathf.Max(barfi(p), Sdf.Polygon(p, new[] { V(0.2f, -0.35f), V(0.55f, -0.15f), V(0.45f, -0.45f) })), ArtCanvas.WithAlpha(ArtCanvas.Hex("#D9E2EC"), 0.85f));
                    foreach (var c in new[] { V(-0.42f, -0.12f), V(0.42f, 0.18f), V(0f, -0.6f) })
                    {
                        a.Fill(p => Sdf.Ellipse(Sdf.Rotate(p, c, 35f), c, V(0.08f, 0.035f)), ArtCanvas.Hex("#7BC043"));
                    }

                    Face(a, V(0f, -0.06f), 0.95f);
                    break;
                }
                case 4:
                {
                    Func<Vector2, float> spiral = p =>
                    {
                        var d = float.MaxValue;
                        var radius = 0.78f;
                        var upper = true;
                        while (radius > 0.12f)
                        {
                            var centre = upper ? Vector2.zero : V(-0.1f, 0f);
                            d = Mathf.Min(d, upper ? Sdf.Arc(p, centre, radius, 0f, Mathf.PI, 0.18f) : Sdf.Arc(p, centre, radius, Mathf.PI, Mathf.PI * 2f, 0.18f));
                            radius -= 0.1f;
                            upper = !upper;
                        }

                        return d;
                    };
                    a.Body(spiral, 0.045f, Ink, ArtCanvas.Hex("#FFAE2B"), ArtCanvas.Hex("#D9480F"), 0.12f);
                    a.Fill(p => Sdf.Circle(p, V(-0.05f, 0.02f), 0.2f) - 0.045f, Ink);
                    a.Body(p => Sdf.Circle(p, V(-0.05f, 0.02f), 0.2f), 0.0f, Ink, ArtCanvas.Hex("#FFB43C"), ArtCanvas.Hex("#E06A12"));
                    ItemArtGenerator.Face(a, V(-0.05f, 0.0f), 0.55f);
                    break;
                }
                case 5:
                {
                    Func<Vector2, float> cham = p => Sdf.Capsule(Sdf.Rotate(p, Vector2.zero, 18f), V(-0.5f, 0f), V(0.5f, 0f), 0.42f);
                    a.Body(cham, 0.05f, Ink, ArtCanvas.Hex("#FFC2D3"), ArtCanvas.Hex("#E2557B"));
                    Speckle(a, cham, 40, 0.8f, 0.05f, ArtCanvas.WithAlpha(White, 0.85f), 5);
                    Gloss(a, V(-0.3f, 0.25f), V(0.22f, 0.1f), -10f);
                    Face(a, V(0.02f, -0.05f), 1f);
                    break;
                }
                case 6:
                case 7:
                {
                    var kheer = level == 7;
                    Func<Vector2, float> bowl = p => Bowl(p, 0.12f, 0.92f, 0.82f);
                    a.Body(bowl, 0.05f, Ink, kheer ? ArtCanvas.Hex("#E08A52") : ArtCanvas.Hex("#EEF1F7"), kheer ? ArtCanvas.Hex("#8C3F1A") : ArtCanvas.Hex("#9AA6C0"));
                    a.Body(p => Sdf.Ellipse(p, V(0f, 0.12f), V(0.92f, 0.24f)), 0.04f, Ink, kheer ? ArtCanvas.Hex("#FFF4DA") : ArtCanvas.Hex("#FFE79E"), kheer ? ArtCanvas.Hex("#EED9AE") : ArtCanvas.Hex("#F2C455"));
                    if (kheer)
                    {
                        foreach (var c in new[] { V(-0.4f, 0.14f), V(0.1f, 0.2f), V(0.45f, 0.08f) })
                        {
                            a.Body(p => Sdf.Ellipse(Sdf.Rotate(p, c, 30f), c, V(0.12f, 0.05f)), 0.02f, Ink, ArtCanvas.Hex("#F2D3A2"), ArtCanvas.Hex("#C49A5E"));
                        }

                        foreach (var c in new[] { V(-0.15f, 0.08f), V(0.3f, 0.18f) })
                        {
                            a.Fill(p => Sdf.Capsule(p, c, c + V(0.12f, 0.03f), 0.012f), ArtCanvas.Hex("#F07F13"));
                        }
                    }
                    else
                    {
                        foreach (var c in new[] { V(-0.42f, 0.16f), V(0.05f, 0.2f), V(0.48f, 0.12f) })
                        {
                            a.Body(p => Sdf.Ellipse(p, c, V(0.2f, 0.09f)), 0.025f, Ink, White, ArtCanvas.Hex("#E2E2D0"));
                            a.Fill(p => Sdf.Circle(p, c + V(0f, 0.02f), 0.03f), ArtCanvas.Hex("#7BC043"));
                        }
                    }

                    Face(a, V(0f, -0.38f), 1f);
                    break;
                }
                case 8:
                {
                    Func<Vector2, float> box = p => Sdf.Box(p, V(0f, -0.12f), V(0.64f, 0.5f), 0.1f);
                    a.Body(box, 0.05f, Ink, ArtCanvas.Hex("#FF6FAE"), ArtCanvas.Hex("#B8125C"));
                    a.Body(p => Sdf.Box(p, V(0f, 0.38f), V(0.7f, 0.14f), 0.07f), 0.05f, Ink, ArtCanvas.Hex("#FF8CC0"), ArtCanvas.Hex("#C81F6B"));
                    a.FillInside(p => Mathf.Max(box(p), Mathf.Abs(p.x) - 0.08f), ArtCanvas.Hex("#FFC23D"));
                    a.FillInside(p => Mathf.Max(Sdf.Box(p, V(0f, 0.38f), V(0.7f, 0.14f), 0.07f), Mathf.Abs(p.x) - 0.08f), ArtCanvas.Hex("#FFC23D"));
                    foreach (var side in new[] { -1f, 1f })
                    {
                        a.Body(p => Sdf.Ellipse(Sdf.Rotate(p, V(side * 0.16f, 0.6f), side * 30f), V(side * 0.16f, 0.6f), V(0.18f, 0.1f)), 0.035f, Ink, ArtCanvas.Hex("#FFD66B"), ArtCanvas.Hex("#E09A10"));
                    }

                    Face(a, V(0f, -0.28f), 1.05f);
                    break;
                }
                default:
                {
                    a.Body(p => Sdf.Ellipse(p, V(0f, -0.28f), V(0.84f, 0.44f)), 0.05f, Ink, ArtCanvas.Hex("#FFE07A"), ArtCanvas.Hex("#C08A14"));
                    a.FillInside(p => Mathf.Abs(Sdf.Ellipse(p, V(0f, -0.24f), V(0.7f, 0.33f))) - 0.015f, ArtCanvas.WithAlpha(ArtCanvas.Hex("#A0700E"), 0.6f));
                    var sweets = new (Vector2 c, float r, string light, string dark)[]
                    {
                        (V(-0.5f, 0.02f), 0.24f, "#FFC93D", "#E06D00"), (V(0.5f, 0.02f), 0.24f, "#A9521F", "#4A1A08"),
                        (V(-0.18f, 0.2f), 0.27f, "#FFC2D3", "#E2557B"), (V(0.2f, 0.22f), 0.26f, "#FFF7E4", "#DCC593"), (V(0f, 0.5f), 0.25f, "#FFC93D", "#E06D00"),
                    };
                    foreach (var s in sweets)
                    {
                        a.Body(p => Sdf.Circle(p, s.c, s.r), 0.04f, Ink, ArtCanvas.Hex(s.light), ArtCanvas.Hex(s.dark));
                    }

                    Face(a, V(0f, -0.46f), 1f);
                    break;
                }
            }
        }

        // ---------------- Street food ----------------

        private static void DrawStreetFood(int level, ArtCanvas a)
        {
            switch (level)
            {
                case 0:
                {
                    var lumps = new[] { (V(-0.4f, 0.1f), 0.48f), (V(0.38f, 0.16f), 0.46f), (V(0f, 0.42f), 0.42f), (V(-0.18f, -0.34f), 0.46f), (V(0.3f, -0.3f), 0.44f) };
                    Func<Vector2, float> pakora = p =>
                    {
                        var d = float.MaxValue;
                        foreach (var (c, r) in lumps)
                        {
                            d = Sdf.SmoothUnion(d, Sdf.Circle(p, c, r), 0.15f);
                        }

                        return d;
                    };
                    a.Body(pakora, 0.05f, Ink, ArtCanvas.Hex("#F5C14E"), ArtCanvas.Hex("#B5621A"));
                    Speckle(a, pakora, 30, 0.75f, 0.05f, ArtCanvas.WithAlpha(ArtCanvas.Hex("#8A4410"), 0.55f), 31);
                    Speckle(a, pakora, 10, 0.7f, 0.05f, ArtCanvas.Hex("#4E9A2A"), 91);
                    Face(a, V(0f, -0.02f), 1f);
                    break;
                }
                case 1:
                {
                    Func<Vector2, float> samosa = p => Sdf.Polygon(p, new[] { V(0f, 0.8f), V(0.74f, -0.5f), V(-0.74f, -0.5f) }) - 0.08f;
                    a.Body(samosa, 0.05f, Ink, ArtCanvas.Hex("#F9CF72"), ArtCanvas.Hex("#C27A27"));
                    for (var i = 0; i < 9; i++)
                    {
                        var x = -0.6f + i * 0.15f;
                        a.FillInside(p => Mathf.Max(samosa(p), Sdf.Circle(p, V(x, -0.5f), 0.06f)), ArtCanvas.WithAlpha(ArtCanvas.Hex("#9E5A17"), 0.6f));
                    }

                    Speckle(a, samosa, 16, 0.55f, 0.03f, ArtCanvas.WithAlpha(ArtCanvas.Hex("#FFF2C4"), 0.8f), 3);
                    Face(a, V(0f, -0.2f), 1f);
                    break;
                }
                case 2:
                {
                    Func<Vector2, float> puri = p => Sdf.Circle(p, Vector2.zero, 0.88f);
                    a.Body(puri, 0.05f, Ink, ArtCanvas.Hex("#F7D58E"), ArtCanvas.Hex("#C4883A"));
                    a.Fill(p => Sdf.Ellipse(p, V(0f, 0.44f), V(0.4f, 0.17f)) - 0.035f, Ink);
                    a.Body(p => Sdf.Ellipse(p, V(0f, 0.44f), V(0.4f, 0.17f)), 0f, Ink, ArtCanvas.Hex("#9CC14A"), ArtCanvas.Hex("#4E7A1E"));
                    Gloss(a, V(-0.42f, 0.0f), V(0.18f, 0.1f), -60f, 0.4f);
                    Face(a, V(0f, -0.22f), 1.05f);
                    break;
                }
                case 3:
                {
                    Func<Vector2, float> bowl = p => Bowl(p, 0.05f, 0.92f, 0.8f);
                    a.Body(p => Sdf.Ellipse(p, V(0f, 0.12f), V(0.82f, 0.42f)), 0.05f, Ink, ArtCanvas.Hex("#F2D49B"), ArtCanvas.Hex("#C4964A"));
                    Speckle(a, p => Sdf.Ellipse(p, V(0f, 0.18f), V(0.7f, 0.34f)), 22, 0.5f, 0.08f, ArtCanvas.Hex("#E8B86A"), 17);
                    a.Fill(p => Sdf.Arc(p, V(0f, 0.6f), 0.5f, 225f * Mathf.Deg2Rad, 315f * Mathf.Deg2Rad, 0.06f), White);
                    Speckle(a, p => Sdf.Ellipse(p, V(0f, 0.22f), V(0.6f, 0.3f)), 10, 0.45f, 0.04f, ArtCanvas.Hex("#D7263D"), 41);
                    Speckle(a, p => Sdf.Ellipse(p, V(0f, 0.22f), V(0.6f, 0.3f)), 10, 0.45f, 0.04f, ArtCanvas.Hex("#4E9A2A"), 59);
                    a.Body(bowl, 0.05f, Ink, ArtCanvas.Hex("#EEF1F7"), ArtCanvas.Hex("#9AA6C0"));
                    Face(a, V(0f, -0.4f), 1f);
                    break;
                }
                case 4:
                {
                    a.Body(p => Sdf.Box(p, V(0f, -0.44f), V(0.66f, 0.16f), 0.12f), 0.05f, Ink, ArtCanvas.Hex("#F5BC77"), ArtCanvas.Hex("#B8702A"));
                    a.Fill(p => Sdf.Box(p, V(0f, -0.22f), V(0.76f, 0.06f), 0.05f) + 0.02f * Mathf.Sin(p.x * 30f), ArtCanvas.Hex("#5FB83A"));
                    a.Body(p => Sdf.Capsule(p, V(-0.62f, -0.08f), V(0.62f, -0.08f), 0.15f), 0.05f, Ink, ArtCanvas.Hex("#8A4B26"), ArtCanvas.Hex("#4A2310"));
                    Func<Vector2, float> top = p => Mathf.Max(Sdf.Ellipse(p, V(0f, 0.08f), V(0.8f, 0.72f)), -(p.y - 0.06f));
                    a.Body(top, 0.05f, Ink, ArtCanvas.Hex("#F7B467"), ArtCanvas.Hex("#B5621D"));
                    Speckle(a, p => Mathf.Max(top(p), -(p.y - 0.4f)), 14, 0.6f, 0.035f, ArtCanvas.Hex("#FFF2D0"), 23);
                    Face(a, V(0f, 0.3f), 0.95f);
                    break;
                }
                case 5:
                {
                    Func<Vector2, Vector2> tilt = p => Sdf.Rotate(p, Vector2.zero, -28f);
                    Func<Vector2, float> roll = p => Sdf.Capsule(tilt(p), V(-0.42f, 0f), V(0.32f, 0f), 0.4f);
                    a.Body(roll, 0.05f, Ink, ArtCanvas.Hex("#F7DCA6"), ArtCanvas.Hex("#C69548"));
                    Speckle(a, roll, 16, 0.75f, 0.06f, ArtCanvas.WithAlpha(ArtCanvas.Hex("#8A5A1E"), 0.5f), 13);
                    a.Body(p => Sdf.Circle(tilt(p), V(0.48f, 0f), 0.26f), 0.04f, Ink, ArtCanvas.Hex("#D9562B"), ArtCanvas.Hex("#8A2A10"));
                    a.FillInside(p => Sdf.Circle(tilt(p), V(0.48f, 0.08f), 0.09f), ArtCanvas.Hex("#5FB83A"));
                    Func<Vector2, float> paper = p => Mathf.Max(roll(p) - 0.04f, tilt(p).x + 0.05f);
                    a.Body(paper, 0.0f, Ink, White, ArtCanvas.Hex("#D6DCE6"));
                    a.FillInside(p => Mathf.Max(paper(p), Mathf.Abs(Mathf.Repeat(tilt(p).y + 0.08f, 0.32f) - 0.16f) - 0.04f), ArtCanvas.WithAlpha(ArtCanvas.Hex("#E0218A"), 0.8f));
                    a.Stroke(paper, 0.035f, Ink);
                    Face(a, V(-0.05f, 0.1f), 0.95f);
                    break;
                }
                case 6:
                {
                    Func<Vector2, Vector2> tilt = p => Sdf.Rotate(p, Vector2.zero, -35f);
                    a.Fill(p => Sdf.Capsule(tilt(p), V(-0.95f, 0f), V(0.95f, 0f), 0.04f), ArtCanvas.Hex("#8E99AE"));
                    foreach (var x in new[] { -0.38f, 0.4f })
                    {
                        a.Body(p => Sdf.Capsule(tilt(p), V(x - 0.28f, 0f), V(x + 0.28f, 0f), 0.26f), 0.05f, Ink, ArtCanvas.Hex("#B8592B"), ArtCanvas.Hex("#5E2510"));
                        for (var i = -1; i <= 1; i++)
                        {
                            var cx = x + i * 0.18f;
                            a.FillInside(p => Mathf.Max(Sdf.Capsule(tilt(p), V(x - 0.28f, 0f), V(x + 0.28f, 0f), 0.26f), Mathf.Abs(tilt(p).x - cx) - 0.025f), ArtCanvas.WithAlpha(ArtCanvas.Hex("#2E1206"), 0.6f));
                        }
                    }

                    a.Stroke(p => Sdf.Circle(p, V(0.05f, -0.62f), 0.2f), 0.06f, ArtCanvas.Hex("#C77DD8"));
                    Face(a, (Vector2)(Quaternion.Euler(0f, 0f, 35f) * new Vector3(-0.38f, -0.02f, 0f)), 0.75f);
                    break;
                }
                case 7:
                {
                    Func<Vector2, float> pot = p => Sdf.SmoothUnion(Sdf.Circle(p, V(0f, -0.2f), 0.72f), Sdf.Box(p, V(0f, 0.48f), V(0.5f, 0.14f), 0.06f), 0.15f);
                    a.Body(pot, 0.05f, Ink, ArtCanvas.Hex("#D97A3E"), ArtCanvas.Hex("#7A2E10"));
                    a.Body(p => Sdf.Box(p, V(0f, 0.62f), V(0.62f, 0.09f), 0.08f), 0.045f, Ink, ArtCanvas.Hex("#E69053"), ArtCanvas.Hex("#8C3A16"));
                    a.FillInside(p => Mathf.Max(pot(p), Mathf.Abs(p.y + 0.02f) - 0.05f), ArtCanvas.Hex("#FFC23D"));
                    for (var i = -1; i <= 1; i++)
                    {
                        var x = i * 0.25f;
                        a.Stroke(p => Sdf.Arc(p, V(x, 0.82f), 0.08f, 0f, Mathf.PI, 0.001f) - 0.02f * Mathf.Sin(p.y * 40f), 0.03f, ArtCanvas.WithAlpha(White, 0.6f));
                    }

                    Face(a, V(0f, -0.3f), 1.05f);
                    break;
                }
                case 8:
                {
                    a.Body(p => Sdf.Ellipse(p, V(0f, -0.34f), V(0.84f, 0.36f)), 0.05f, Ink, White, ArtCanvas.Hex("#BFC9D9"));
                    Func<Vector2, float> rice = p => Mathf.Max(Sdf.Ellipse(p, V(0f, -0.22f), V(0.76f, 0.88f)), -(p.y + 0.3f));
                    a.Body(rice, 0.05f, Ink, ArtCanvas.Hex("#FFEFC0"), ArtCanvas.Hex("#E0A946"));
                    Speckle(a, rice, 40, 0.7f, 0.06f, ArtCanvas.WithAlpha(White, 0.9f), 7);
                    Speckle(a, rice, 18, 0.7f, 0.06f, ArtCanvas.Hex("#F59E0B"), 101);
                    a.Body(p => Sdf.Circle(p, V(0.56f, -0.3f), 0.15f), 0.035f, Ink, ArtCanvas.Hex("#F5F07A"), ArtCanvas.Hex("#B9C43A"));
                    a.Body(p => Sdf.Ellipse(Sdf.Rotate(p, V(-0.5f, 0.35f), 40f), V(-0.5f, 0.35f), V(0.16f, 0.08f)), 0.03f, Ink, ArtCanvas.Hex("#6CC24A"), ArtCanvas.Hex("#2F7A1F"));
                    Face(a, V(0f, 0.0f), 1f);
                    break;
                }
                default:
                {
                    foreach (var x in new[] { -0.5f, 0.5f })
                    {
                        a.Body(p => Sdf.Circle(p, V(x * 0.84f, -0.62f), 0.17f), 0.04f, Ink, ArtCanvas.Hex("#4B4F6B"), ArtCanvas.Hex("#1B1F4B"));
                    }

                    Func<Vector2, float> body = p => Sdf.Box(p, V(0f, -0.24f), V(0.68f, 0.3f), 0.08f);
                    a.Body(body, 0.05f, Ink, ArtCanvas.Hex("#FF5FA2"), ArtCanvas.Hex("#B8125C"));
                    a.FillInside(p => Mathf.Max(body(p), Mathf.Abs(p.y + 0.42f) - 0.05f), ArtCanvas.Hex("#FFC23D"));
                    a.FillInside(p => Mathf.Max(body(p), Mathf.Abs(p.y + 0.42f) - 0.02f), ArtCanvas.Hex("#0E9F8A"));
                    a.Fill(p => Sdf.Capsule(p, V(0f, 0.08f), V(0f, 0.5f), 0.03f), ArtCanvas.Hex("#8E99AE"));
                    Func<Vector2, float> umbrella = p => Mathf.Max(Sdf.Ellipse(p, V(0f, 0.46f), V(0.72f, 0.36f)), -(p.y - 0.46f));
                    a.Body(umbrella, 0.05f, Ink, ArtCanvas.Hex("#3FD1B8"), ArtCanvas.Hex("#0B7A69"));
                    a.FillInside(p => Mathf.Max(umbrella(p), Mathf.Abs(Mathf.Atan2(p.y - 0.46f, p.x) - Mathf.PI * 0.5f) % 0.6f - 0.15f), ArtCanvas.WithAlpha(White, 0.5f));
                    a.Body(p => Sdf.Ellipse(p, V(-0.36f, 0.16f), V(0.2f, 0.12f)), 0.035f, Ink, ArtCanvas.Hex("#E69053"), ArtCanvas.Hex("#8C3A16"));
                    a.Body(p => Sdf.Ellipse(p, V(0.36f, 0.16f), V(0.2f, 0.12f)), 0.035f, Ink, ArtCanvas.Hex("#EEF1F7"), ArtCanvas.Hex("#9AA6C0"));
                    Face(a, V(0f, -0.26f), 1f);
                    break;
                }
            }
        }

        // ---------------- Truck art medallions ----------------

        private static readonly string[][] Palettes =
        {
            new[] { "#2BB673", "#0E7A4B", "#FFC23D" }, new[] { "#FFD23F", "#D9A400", "#E0218A" }, new[] { "#B04FD8", "#6B1F96", "#3FD1B8" },
            new[] { "#FF5E57", "#B8231C", "#FFD23F" }, new[] { "#FFB347", "#D9730D", "#1B1F4B" }, new[] { "#FF8A00", "#B85600", "#0E9F8A" },
            new[] { "#3FA9F5", "#1B5FA8", "#FFC23D" }, new[] { "#FF6FAE", "#B8125C", "#FFD23F" }, new[] { "#0E9F8A", "#05584C", "#FF5E57" },
            new[] { "#1B1F4B", "#0B0E2A", "#FFC23D" },
        };

        private static void DrawTruckArt(int level, ArtCanvas a)
        {
            var palette = Palettes[level];
            Func<Vector2, float> disc = p => Sdf.Circle(p, Vector2.zero, 0.92f);
            a.Body(disc, 0.05f, Ink, ArtCanvas.Hex(palette[0]), ArtCanvas.Hex(palette[1]));
            for (var i = 0; i < 14; i++)
            {
                var angle = i * Mathf.PI * 2f / 14f;
                var c = V(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.78f;
                a.Fill(p => Sdf.Circle(p, c, 0.075f), i % 2 == 0 ? ArtCanvas.Hex(palette[2]) : White);
                a.Stroke(p => Sdf.Circle(p, c, 0.075f), 0.02f, Ink);
            }

            a.Stroke(p => Sdf.Circle(p, Vector2.zero, 0.66f), 0.05f, ArtCanvas.Hex(palette[2]));
            a.Fill(p => Sdf.Circle(p, Vector2.zero, 0.62f), ArtCanvas.WithAlpha(White, 0.92f));
            var zoom = a.Zoom;
            var offset = a.Offset;
            a.Zoom = zoom * 0.6f;
            a.Offset = offset;
            ItemArtGenerator.Draw(DefaultIds[level], a);
            a.Zoom = zoom;
            a.Offset = offset;
            Gloss(a, V(-0.42f, 0.5f), V(0.22f, 0.1f), -40f, 0.45f);
        }

        // ---------------- Numbers ----------------

        private static readonly string[] NumberColours =
        {
            "#F2E3C6", "#FFD23F", "#FFB347", "#FF8A5B", "#FF5E57", "#E0218A", "#B04FD8", "#3FA9F5", "#0E9F8A", "#1B1F4B",
        };

        // Seven segments a..g as (from, to) in a unit cell (x ∈ [-1,1], y ∈ [-1,1]).
        private static readonly (Vector2, Vector2)[] Segments =
        {
            (V(-0.7f, 1f), V(0.7f, 1f)), (V(1f, 0.85f), V(1f, 0.15f)), (V(1f, -0.15f), V(1f, -0.85f)), (V(-0.7f, -1f), V(0.7f, -1f)),
            (V(-1f, -0.15f), V(-1f, -0.85f)), (V(-1f, 0.85f), V(-1f, 0.15f)), (V(-0.7f, 0f), V(0.7f, 0f)),
        };

        private static readonly string[] DigitSegments = { "abcdef", "bc", "abged", "abgcd", "fgbc", "afgcd", "afgedc", "abc", "abcdefg", "abcdfg" };

        private static void DrawNumber(int level, ArtCanvas a)
        {
            var colour = ArtCanvas.Hex(NumberColours[level]);
            a.Body(p => Sdf.Circle(p, Vector2.zero, 0.92f), 0.05f, Ink, colour, Color.Lerp(colour, Color.black, 0.45f));
            Gloss(a, V(-0.38f, 0.48f), V(0.3f, 0.14f), -35f, 0.5f);
            var text = (1 << (level + 1)).ToString();
            var cellWidth = text.Length <= 2 ? 0.26f : text.Length == 3 ? 0.2f : 0.15f;
            var cellHeight = text.Length <= 2 ? 0.34f : text.Length == 3 ? 0.27f : 0.21f;
            var stroke = cellWidth * 0.28f;
            var spacing = cellWidth * 2.7f;
            var start = -(text.Length - 1) * spacing * 0.5f;
            Func<Vector2, float> glyphs = p =>
            {
                var d = float.MaxValue;
                for (var i = 0; i < text.Length; i++)
                {
                    var centre = V(start + i * spacing, -0.02f);
                    foreach (var segment in DigitSegments[text[i] - '0'])
                    {
                        var (from, to) = Segments[segment - 'a'];
                        d = Mathf.Min(d, Sdf.Capsule(p, centre + V(from.x * cellWidth, from.y * cellHeight), centre + V(to.x * cellWidth, to.y * cellHeight), stroke));
                    }
                }

                return d;
            };
            a.Fill(p => glyphs(p) - 0.035f, Ink);
            a.Fill(glyphs, White);
        }
    }
}
