using System;
using UnityEngine;

namespace DesiMerge.Editor.Content
{
    internal sealed class ArtCanvas
    {
        private readonly int _width;
        private readonly int _height;
        private readonly Color[] _pixels;

        public ArtCanvas(int width, int height)
        {
            _width = width;
            _height = height;
            _pixels = new Color[width * height];
        }

        public Vector2 Offset { get; set; }
        public float Zoom { get; set; } = 1f;

        public float PixelSize => 2f / _width;
        public float HalfHeight => (float)_height / _width;

        public Vector2 ToSpace(int x, int y)
        {
            return new Vector2((x + 0.5f) / _width * 2f - 1f, ((y + 0.5f) / _height * 2f - 1f) * HalfHeight);
        }

        private Vector2 Local(int x, int y) => (ToSpace(x, y) - Offset) / Zoom;

        public void Fill(Func<Vector2, float> sdf, Color color, float softness = 0f) => Fill(sdf, _ => color, softness);

        public void Fill(Func<Vector2, float> sdf, Func<Vector2, Color> paint, float softness = 0f)
        {
            var edge = PixelSize + softness * Zoom;
            for (var y = 0; y < _height; y++)
            {
                for (var x = 0; x < _width; x++)
                {
                    var p = Local(x, y);
                    var d = sdf(p) * Zoom;
                    var coverage = Mathf.Clamp01(0.5f - d / edge);
                    if (coverage <= 0f)
                    {
                        continue;
                    }

                    var color = paint(p);
                    color.a *= coverage;
                    var index = y * _width + x;
                    _pixels[index] = Over(_pixels[index], color);
                }
            }
        }

        public void FillInside(Func<Vector2, float> sdf, Color color, float softness = 0f)
        {
            var edge = PixelSize + softness * Zoom;
            for (var y = 0; y < _height; y++)
            {
                for (var x = 0; x < _width; x++)
                {
                    var index = y * _width + x;
                    var under = _pixels[index];
                    if (under.a <= 0f)
                    {
                        continue;
                    }

                    var coverage = Mathf.Clamp01(0.5f - sdf(Local(x, y)) * Zoom / edge);
                    if (coverage <= 0f)
                    {
                        continue;
                    }

                    var over = color;
                    over.a *= coverage;
                    var result = Over(under, over);
                    result.a = under.a;
                    _pixels[index] = result;
                }
            }
        }

        public void Stroke(Func<Vector2, float> sdf, float width, Color color) => Fill(p => Mathf.Abs(sdf(p)) - width * 0.5f, color);

        public void Body(Func<Vector2, float> sdf, float outline, Color ink, Color light, Color dark, float rim = 0.22f)
        {
            Fill(p => sdf(p) - outline, ink);
            Fill(sdf, p => Shade(p, sdf(p), light, dark, rim));
        }

        public static Color Shade(Vector2 p, float d, Color light, Color dark, float rim)
        {
            var t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.9f, 0.9f, p.x * 0.55f - p.y * 0.85f));
            var color = Color.Lerp(light, dark, t * 0.75f);
            var edge = 1f - Mathf.Clamp01(-d / rim);
            return Color.Lerp(color, dark, edge * edge * 0.55f);
        }

        public Texture2D ToTexture(bool mipmaps = false)
        {
            var texture = new Texture2D(_width, _height, TextureFormat.RGBA32, mipmaps);
            texture.SetPixels(_pixels);
            texture.Apply();
            return texture;
        }

        public static Color Over(Color under, Color over)
        {
            var alpha = over.a + under.a * (1f - over.a);
            if (alpha <= 0f)
            {
                return new Color(0f, 0f, 0f, 0f);
            }

            var r = (over.r * over.a + under.r * under.a * (1f - over.a)) / alpha;
            var g = (over.g * over.a + under.g * under.a * (1f - over.a)) / alpha;
            var b = (over.b * over.a + under.b * under.a * (1f - over.a)) / alpha;
            return new Color(r, g, b, alpha);
        }

        public static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var color) ? color : Color.magenta;
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }

    internal static class Sdf
    {
        public static float Circle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;

        public static float Ellipse(Vector2 p, Vector2 c, Vector2 r)
        {
            var q = new Vector2((p.x - c.x) / r.x, (p.y - c.y) / r.y);
            var k0 = q.magnitude;
            var k1 = new Vector2(q.x / r.x, q.y / r.y).magnitude;
            return k1 > 1e-5f ? k0 * (k0 - 1f) / k1 : -Mathf.Min(r.x, r.y);
        }

        public static float Box(Vector2 p, Vector2 c, Vector2 half, float radius)
        {
            var q = new Vector2(Mathf.Abs(p.x - c.x) - half.x + radius, Mathf.Abs(p.y - c.y) - half.y + radius);
            var outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        public static float Capsule(Vector2 p, Vector2 a, Vector2 b, float r)
        {
            var pa = p - a;
            var ba = b - a;
            var h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - r;
        }

        public static float TaperedCapsule(Vector2 p, Vector2 a, Vector2 b, float ra, float rb)
        {
            var pa = p - a;
            var ba = b - a;
            var h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - Mathf.Lerp(ra, rb, h);
        }

        public static float Polygon(Vector2 p, Vector2[] v)
        {
            var d = Vector2.Dot(p - v[0], p - v[0]);
            var s = 1f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                var e = v[j] - v[i];
                var w = p - v[i];
                var b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                var c1 = p.y >= v[i].y;
                var c2 = p.y < v[j].y;
                var c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3))
                {
                    s = -s;
                }
            }

            return s * Mathf.Sqrt(d);
        }

        public static float Arc(Vector2 p, Vector2 c, float r, float from, float to, float width)
        {
            var q = p - c;
            var angle = Mathf.Atan2(q.y, q.x);
            var mid = (from + to) * 0.5f;
            var half = Mathf.Abs(to - from) * 0.5f;
            var delta = Mathf.Abs(Mathf.DeltaAngle(mid * Mathf.Rad2Deg, angle * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
            if (delta <= half)
            {
                return Mathf.Abs(q.magnitude - r) - width * 0.5f;
            }

            var a = c + new Vector2(Mathf.Cos(from), Mathf.Sin(from)) * r;
            var b = c + new Vector2(Mathf.Cos(to), Mathf.Sin(to)) * r;
            return Mathf.Min((p - a).magnitude, (p - b).magnitude) - width * 0.5f;
        }

        public static float Union(float a, float b) => Mathf.Min(a, b);

        public static float Intersect(float a, float b) => Mathf.Max(a, b);

        public static float Subtract(float a, float b) => Mathf.Max(a, -b);

        public static float SmoothUnion(float a, float b, float k)
        {
            var h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
            return Mathf.Lerp(b, a, h) - k * h * (1f - h);
        }

        public static Vector2 Rotate(Vector2 p, Vector2 pivot, float degrees)
        {
            var radians = -degrees * Mathf.Deg2Rad;
            var cos = Mathf.Cos(radians);
            var sin = Mathf.Sin(radians);
            var q = p - pivot;
            return pivot + new Vector2(q.x * cos - q.y * sin, q.x * sin + q.y * cos);
        }
    }
}
