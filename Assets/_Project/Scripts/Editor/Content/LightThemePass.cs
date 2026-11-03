using System.Collections.Generic;
using DesiMerge.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DesiMerge.Editor.Content
{
    /// <summary>
    /// Light "glass" look, applied to every generated scene and panel prefab just before it is saved: surfaces that
    /// were drawn in the dark navy (<see cref="UITheme.deep"/>) become frosted white glass, and the white or saffron
    /// text and white icons on them turn navy / deep amber so they stay readable. Dark text loses its heavy outline.
    /// Colourful buttons, item art and the modal dim are left as they are.
    /// </summary>
    internal static class LightThemePass
    {
        private static readonly HashSet<string> Keep = new HashSet<string> { "SkinString", "Hole", "CountBadge" };

        public static void Apply(Scene scene, UITheme theme)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                Apply(root, theme);
            }
        }

        public static void Apply(GameObject root, UITheme theme)
        {
            if (root == null || theme == null)
            {
                return;
            }

            var converted = new HashSet<Transform>();
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (Keep.Contains(image.name) || image.color.a < 0.5f || !IsDeep(image.color, theme.deep))
                {
                    continue;
                }

                image.color = new Color(theme.glass.r, theme.glass.g, theme.glass.b, theme.glass.a);
                converted.Add(image.transform);
                UiFactory.Record(image);

                // A thin navy edge so glass cards stand apart from the cream background and from each other
                // (panels that already have a coloured Rim keep just that).
                var hasRim = image.name == "Fill" && image.transform.parent != null && image.transform.parent.Find("Rim") != null;
                if (!hasRim && image.GetComponent<Outline>() == null)
                {
                    var edge = image.gameObject.AddComponent<Outline>();
                    edge.effectColor = new Color(theme.deep.r, theme.deep.g, theme.deep.b, 0.12f);
                    edge.effectDistance = new Vector2(2.5f, -2.5f);
                }
            }

            // Faint white tracks and wells (drawn for a navy surface) would vanish on glass: tint them navy instead.
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                var c = image.color;
                if (converted.Contains(image.transform) || c.a < 0.04f || c.a >= 0.5f || Luminance(c) < 0.85f || !converted.Contains(BackgroundOf(image.transform)))
                {
                    continue;
                }

                image.color = new Color(theme.deep.r, theme.deep.g, theme.deep.b, Mathf.Clamp(c.a * 0.8f, 0.08f, 0.2f));
                UiFactory.Record(image);
            }

            foreach (var text in root.GetComponentsInChildren<Text>(true))
            {
                var onGlass = converted.Contains(BackgroundOf(text.transform));
                if (onGlass)
                {
                    if (IsNear(text.color, theme.accent))
                    {
                        text.color = theme.accentText;
                        UiFactory.Record(text);
                    }
                    else if (Luminance(text.color) > 0.7f)
                    {
                        text.color = new Color(theme.deep.r, theme.deep.g, theme.deep.b, text.color.a);
                        UiFactory.Record(text);
                    }
                }

                // Heavy dark outlines and drop shadows make dark text look blotchy (e.g. a navy title on saffron).
                if (Luminance(text.color) < 0.4f)
                {
                    foreach (var effect in text.GetComponents<Shadow>())
                    {
                        if (effect.enabled)
                        {
                            effect.enabled = false;
                            UiFactory.Record(effect);
                        }
                    }
                }
            }

            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (converted.Contains(image.transform) || !IsWhiteIcon(image) || !converted.Contains(BackgroundOf(image.transform)))
                {
                    continue;
                }

                image.color = new Color(theme.deep.r, theme.deep.g, theme.deep.b, image.color.a);
                UiFactory.Record(image);
            }
        }

        /// <summary>The graphic a UI element is drawn on: the nearest visible ancestor image (or a panel's Fill).</summary>
        private static Transform BackgroundOf(Transform element)
        {
            for (var parent = element.parent; parent != null; parent = parent.parent)
            {
                var image = parent.GetComponent<Image>();
                if (image != null && image.enabled && image.color.a > 0.3f)
                {
                    return image.transform;
                }

                var fill = parent.Find("Fill");
                if (fill != null && fill != element && fill.GetComponent<Image>() is Image fillImage && fillImage.color.a > 0.3f)
                {
                    return fill;
                }
            }

            return null;
        }

        private static bool IsWhiteIcon(Image image)
        {
            if (image.sprite == null || Luminance(image.color) < 0.8f)
            {
                return false;
            }

            var path = AssetDatabase.GetAssetPath(image.sprite);
            return path.StartsWith(GameArtGenerator.IconFolder) && !path.EndsWith("Art.png");
        }

        private static bool IsDeep(Color color, Color deep) => IsNear(color, deep);

        private static bool IsNear(Color a, Color b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < 0.06f;

        private static float Luminance(Color c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
    }
}
