using System.Collections.Generic;
using System.IO;
using DesiMerge.Configuration;
using DesiMerge.Gameplay;
using DesiMerge.UI;
using DesiMerge.UI.Animation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DesiMerge.Tests.EditMode
{
    public class VisualContentTests
    {
        private const string ProgressionPath = "Assets/_Project/ScriptableObjects/MergeProgression.asset";
        private const string PlaceholderCirclePath = "Assets/_Project/Art/Placeholder/Circle.png";
        private const string ItemArtFolder = "Assets/_Project/Art/Sprites/Items/";
        private const string ThemePath = "Assets/_Project/Settings/UI/UITheme.asset";
        private const string ItemPrefabPath = "Assets/_Project/Prefabs/MergeItem.prefab";

        private static readonly (string Id, float Radius, int Score)[] ShippedProgression =
        {
            ("patti", 0.3f, 1), ("nimbu", 0.4f, 3), ("pyaz", 0.5f, 6), ("tamatar", 0.6f, 10), ("roti", 0.72f, 15),
            ("aam", 0.85f, 21), ("chai", 1f, 28), ("mango_shake", 1.15f, 36), ("rickshaw", 1.3f, 45), ("bus", 1.45f, 55),
        };

        private static MergeProgression LoadProgression()
        {
            var progression = AssetDatabase.LoadAssetAtPath<MergeProgression>(ProgressionPath);
            if (progression == null)
            {
                Assert.Ignore("Generated content is missing. Run Desi Merge > Setup > Create All Content.");
            }

            return progression;
        }

        [Test]
        public void Redesign_KeepsMergeOrderSizesAndScores()
        {
            var progression = LoadProgression();
            Assert.AreEqual(ShippedProgression.Length, progression.Count);
            for (var level = 0; level < progression.Count; level++)
            {
                var definition = progression.Get(level);
                Assert.AreEqual(ShippedProgression[level].Id, definition.Id, $"Level {level} id");
                Assert.AreEqual(ShippedProgression[level].Radius, definition.Radius, 1e-4f, $"{definition.Id} radius");
                Assert.AreEqual(ShippedProgression[level].Score, definition.ScoreValue, $"{definition.Id} score");
            }
        }

        [Test]
        public void EveryLevel_HasItsOwnIllustratedSprite_DrawnUntinted()
        {
            var progression = LoadProgression();
            var placeholder = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderCirclePath);
            var seen = new HashSet<Sprite>();
            for (var level = 0; level < progression.Count; level++)
            {
                var definition = progression.Get(level);
                Assert.IsNotNull(definition.Sprite, $"{definition.Id} has no sprite");
                Assert.AreNotSame(placeholder, definition.Sprite, $"{definition.Id} still uses the placeholder circle");
                StringAssert.StartsWith(ItemArtFolder, AssetDatabase.GetAssetPath(definition.Sprite), definition.Id);
                Assert.IsTrue(seen.Add(definition.Sprite), $"{definition.Id} shares its sprite with another level");
                Assert.AreEqual(Color.white, definition.SpriteColor, $"{definition.Id} artwork must not be tinted");
            }
        }

        [Test]
        public void ItemArtwork_IsOneUnitWide_Centred_AndStaysInsideTheColliderCircle()
        {
            var progression = LoadProgression();
            var failures = new List<string>();
            for (var level = 0; level < progression.Count; level++)
            {
                var sprite = progression.Get(level).Sprite;
                var name = progression.Get(level).Id;
                Assert.AreEqual(1f, sprite.rect.width / sprite.pixelsPerUnit, 1e-3f, $"{name} width in units");
                Assert.AreEqual(new Vector2(0.5f, 0.5f), sprite.pivot / sprite.rect.size, $"{name} pivot");

                var texture = new Texture2D(2, 2);
                texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));
                var size = texture.width;
                var limit = size * 0.5f + 1f;
                var pixels = texture.GetPixels32();
                var outside = 0;
                for (var i = 0; i < pixels.Length; i++)
                {
                    var x = i % size + 0.5f - size * 0.5f;
                    var y = i / size + 0.5f - size * 0.5f;
                    if (x * x + y * y > limit * limit && pixels[i].a > 8)
                    {
                        outside++;
                    }
                }

                Object.DestroyImmediate(texture);
                if (outside > 0)
                {
                    failures.Add($"{name}: {outside} visible pixels outside its collider circle");
                }
            }

            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        [Test]
        public void ItemPrefab_AnimatesOnlyTheVisualChild_AndHasNoTextLabel()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ItemPrefabPath);
            Assert.IsNotNull(prefab);
            Assert.IsNull(prefab.GetComponent<ScalePunch>(), "The physics root must never be scaled by an animation.");
            Assert.AreEqual(0.5f, prefab.GetComponent<CircleCollider2D>().radius);
            var visual = prefab.GetComponentInChildren<SpriteRenderer>(true);
            Assert.AreNotSame(prefab, visual.gameObject);
            Assert.IsNotNull(visual.GetComponent<ScalePunch>());
            Assert.IsNull(visual.GetComponent<Collider2D>());
            Assert.IsEmpty(prefab.GetComponentsInChildren<TextMesh>(true));
            Assert.IsNotNull(prefab.GetComponent<MergeItem>());
        }

        [Test]
        public void Theme_HasEverySpriteAndBothFonts_AndTheDisplayFontShipsWithItsLicense()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            Assert.IsNotNull(theme);
            foreach (var (name, sprite) in new[]
                     {
                         ("panel", theme.panel), ("button", theme.button), ("pill", theme.pill), ("disc", theme.disc), ("glow", theme.glow),
                         ("ornament", theme.ornament), ("borderStrip", theme.borderStrip), ("backgroundGradient", theme.backgroundGradient),
                         ("pauseIcon", theme.pauseIcon),
                     })
            {
                Assert.IsNotNull(sprite, $"Theme sprite '{name}' is missing");
            }

            Assert.IsNotNull(theme.font);
            Assert.IsNotNull(theme.bodyFont);
            var fontPath = AssetDatabase.GetAssetPath(theme.font);
            var license = Path.Combine(Path.GetDirectoryName(fontPath) ?? string.Empty, "LilitaOne-OFL.txt");
            Assert.IsTrue(File.Exists(license), "The bundled font must ship with its SIL Open Font License text.");
            StringAssert.Contains("SIL Open Font License", File.ReadAllText(license));
        }

        [Test]
        public void IconSet_IsComplete()
        {
            foreach (var icon in new[] { "Pause", "Play", "Hammer", "Shuffle", "Gear", "Home", "Restart", "Trophy", "Grid", "Close", "Crown", "Star", "Music", "Speaker", "Lock", "Check", "Video" })
            {
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/_Project/Art/UI/Icons/{icon}.png"), icon);
            }
        }
    }
}
