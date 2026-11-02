using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.Localization
{
    /// <summary>
    /// UI translations. The key of every string is its English text, so English needs no table entry and a missing
    /// translation simply shows English. The table is a tab-separated file (one column per language) loaded once by
    /// the bootstrapper. Urdu is shaped and laid out right-to-left (<see cref="UrduShaper"/>) and uses the bundled
    /// Noto Naskh Arabic font; Chinese uses the phone's own Chinese font. Static because every Text in every scene
    /// needs it and the selected language is a single app-wide setting.
    /// </summary>
    public static class Loc
    {
        public sealed class LanguageInfo
        {
            public string Code;
            public string NativeName;
            public bool RightToLeft;
        }

        public static readonly LanguageInfo[] Languages =
        {
            new LanguageInfo { Code = "en", NativeName = "English" },
            new LanguageInfo { Code = "ur", NativeName = "اردو", RightToLeft = true },
            new LanguageInfo { Code = "zh", NativeName = "中文" },
            new LanguageInfo { Code = "es", NativeName = "Español" },
            new LanguageInfo { Code = "pt", NativeName = "Português" },
            new LanguageInfo { Code = "fr", NativeName = "Français" },
            new LanguageInfo { Code = "de", NativeName = "Deutsch" },
            new LanguageInfo { Code = "it", NativeName = "Italiano" },
            new LanguageInfo { Code = "ms", NativeName = "Bahasa Melayu" },
        };

        private static readonly Dictionary<string, Dictionary<string, string>> Tables = new Dictionary<string, Dictionary<string, string>>();
        private static Font _urduFont;
        private static Font _chineseFont;

        public static string Current { get; private set; } = "en";
        public static bool IsRightToLeft => Current == "ur";

        public static event Action Changed;

        /// <summary>Loads the table (header: key, then language codes). Safe to call again (e.g. tests).</summary>
        public static void Initialize(string table, Font urduFont, string language)
        {
            _urduFont = urduFont;
            Tables.Clear();
            if (!string.IsNullOrEmpty(table))
            {
                var lines = table.Split('\n');
                var header = lines[0].TrimEnd('\r').Split('\t');
                for (var column = 1; column < header.Length; column++)
                {
                    Tables[header[column]] = new Dictionary<string, string>();
                }

                for (var row = 1; row < lines.Length; row++)
                {
                    var cells = lines[row].TrimEnd('\r').Split('\t');
                    if (cells.Length < 2 || string.IsNullOrEmpty(cells[0]))
                    {
                        continue;
                    }

                    var key = Unescape(cells[0]);
                    for (var column = 1; column < header.Length && column < cells.Length; column++)
                    {
                        if (!string.IsNullOrEmpty(cells[column]))
                        {
                            Tables[header[column]][key] = Unescape(cells[column]);
                        }
                    }
                }
            }

            Current = IsKnown(language) ? language : "en";
            Changed?.Invoke();
        }

        public static bool IsKnown(string code) => Array.Exists(Languages, l => l.Code == code);

        public static LanguageInfo Info(string code) => Array.Find(Languages, l => l.Code == code) ?? Languages[0];

        public static void SetLanguage(string code)
        {
            if (!IsKnown(code) || code == Current)
            {
                return;
            }

            Current = code;
            Changed?.Invoke();
        }

        /// <summary>The translation of an English UI string (shaped for Urdu). Unknown keys stay English.</summary>
        public static string T(string english)
        {
            if (string.IsNullOrEmpty(english))
            {
                return english;
            }

            var text = Raw(english);
            return IsRightToLeft ? UrduShaper.Shape(text) : text;
        }

        /// <summary>Translates a template like "Need {0} diamonds." and fills it in.</summary>
        public static string F(string englishTemplate, params object[] args)
        {
            var text = string.Format(Raw(englishTemplate), args);
            return IsRightToLeft ? UrduShaper.Shape(text) : text;
        }

        /// <summary>Unshaped translation (for measuring and wrapping before shaping).</summary>
        public static string Raw(string english)
        {
            if (string.IsNullOrEmpty(english) || Current == "en")
            {
                return english;
            }

            return Tables.TryGetValue(Current, out var table) && table.TryGetValue(english, out var value) ? value : english;
        }

        /// <summary>The font a Text should use for the current language (its own font for Latin languages).</summary>
        public static Font FontFor(Font original) => FontForLanguage(Current, original);

        /// <summary>The font that can draw a given language (used where a label is written in another language).</summary>
        public static Font FontForLanguage(string code, Font original)
        {
            if (code == "ur" && _urduFont != null)
            {
                return _urduFont;
            }

            if (code == "zh")
            {
                if (_chineseFont == null)
                {
                    _chineseFont = Font.CreateDynamicFontFromOSFont(new[] { "Noto Sans CJK SC", "NotoSansCJK-Regular", "Noto Sans SC", "Microsoft YaHei", "PingFang SC", "Droid Sans Fallback", "sans-serif" }, 64);
                }

                return _chineseFont != null ? _chineseFont : original;
            }

            return original;
        }

        /// <summary>
        /// Sets translated text on a Text. For Urdu in a wrapping box, lines are broken by width first (in reading
        /// order) and then each line is shaped, so long sentences read top-to-bottom correctly.
        /// </summary>
        public static void Set(Text target, string english, params object[] args)
        {
            if (target == null)
            {
                return;
            }

            var logical = args != null && args.Length > 0 ? string.Format(Raw(english), args) : Raw(english);
            if (!IsRightToLeft)
            {
                target.text = logical;
                return;
            }

            target.text = target.horizontalOverflow == HorizontalWrapMode.Wrap ? WrapRightToLeft(target, logical) : UrduShaper.Shape(logical);
        }

        private static string WrapRightToLeft(Text target, string logical)
        {
            var width = target.rectTransform.rect.width;
            if (width <= 1f)
            {
                return UrduShaper.Shape(logical);
            }

            var settings = target.GetGenerationSettings(new Vector2(float.MaxValue, float.MaxValue));
            var generator = target.cachedTextGeneratorForLayout;
            var lines = new List<string>();
            foreach (var paragraph in logical.Split('\n'))
            {
                var line = string.Empty;
                foreach (var word in paragraph.Split(' '))
                {
                    var candidate = line.Length == 0 ? word : line + " " + word;
                    var measured = generator.GetPreferredWidth(UrduShaper.ShapeLine(candidate), settings) / target.pixelsPerUnit;
                    if (measured > width && line.Length > 0)
                    {
                        lines.Add(line);
                        line = word;
                    }
                    else
                    {
                        line = candidate;
                    }
                }

                lines.Add(line);
            }

            for (var i = 0; i < lines.Count; i++)
            {
                lines[i] = UrduShaper.ShapeLine(lines[i]);
            }

            return string.Join("\n", lines);
        }

        private static string Unescape(string cell) => cell.Replace("\\n", "\n");
    }
}
