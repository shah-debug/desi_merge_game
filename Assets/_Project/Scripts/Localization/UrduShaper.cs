using System.Collections.Generic;
using System.Text;

namespace DesiMerge.Localization
{
    /// <summary>
    /// Makes Urdu/Arabic text display correctly in Unity's legacy Text, which neither joins letters nor lays text out
    /// right-to-left. Each letter is replaced by its Unicode presentation form (isolated, final, initial or medial,
    /// from Arabic Presentation Forms-A/B, which the bundled Noto Naskh Arabic font contains), then each line is
    /// reversed into visual order while runs of digits and Latin text keep their own left-to-right order.
    /// </summary>
    public static class UrduShaper
    {
        // letter → (isolated, final, initial, medial); 0 = that form doesn't exist (the letter doesn't join forward).
        private static readonly Dictionary<char, char[]> Forms = new Dictionary<char, char[]>
        {
            { 'ا', new[] { 'ﺍ', 'ﺎ', '\0', '\0' } }, { 'آ', new[] { 'ﺁ', 'ﺂ', '\0', '\0' } },
            { 'ب', new[] { 'ﺏ', 'ﺐ', 'ﺑ', 'ﺒ' } }, { 'پ', new[] { 'ﭖ', 'ﭗ', 'ﭘ', 'ﭙ' } },
            { 'ت', new[] { 'ﺕ', 'ﺖ', 'ﺗ', 'ﺘ' } }, { 'ٹ', new[] { 'ﭦ', 'ﭧ', 'ﭨ', 'ﭩ' } },
            { 'ث', new[] { 'ﺙ', 'ﺚ', 'ﺛ', 'ﺜ' } }, { 'ج', new[] { 'ﺝ', 'ﺞ', 'ﺟ', 'ﺠ' } },
            { 'چ', new[] { 'ﭺ', 'ﭻ', 'ﭼ', 'ﭽ' } }, { 'ح', new[] { 'ﺡ', 'ﺢ', 'ﺣ', 'ﺤ' } },
            { 'خ', new[] { 'ﺥ', 'ﺦ', 'ﺧ', 'ﺨ' } }, { 'د', new[] { 'ﺩ', 'ﺪ', '\0', '\0' } },
            { 'ڈ', new[] { 'ﮈ', 'ﮉ', '\0', '\0' } }, { 'ذ', new[] { 'ﺫ', 'ﺬ', '\0', '\0' } },
            { 'ر', new[] { 'ﺭ', 'ﺮ', '\0', '\0' } }, { 'ڑ', new[] { 'ﮌ', 'ﮍ', '\0', '\0' } },
            { 'ز', new[] { 'ﺯ', 'ﺰ', '\0', '\0' } }, { 'ژ', new[] { 'ﮊ', 'ﮋ', '\0', '\0' } },
            { 'س', new[] { 'ﺱ', 'ﺲ', 'ﺳ', 'ﺴ' } }, { 'ش', new[] { 'ﺵ', 'ﺶ', 'ﺷ', 'ﺸ' } },
            { 'ص', new[] { 'ﺹ', 'ﺺ', 'ﺻ', 'ﺼ' } }, { 'ض', new[] { 'ﺽ', 'ﺾ', 'ﺿ', 'ﻀ' } },
            { 'ط', new[] { 'ﻁ', 'ﻂ', 'ﻃ', 'ﻄ' } }, { 'ظ', new[] { 'ﻅ', 'ﻆ', 'ﻇ', 'ﻈ' } },
            { 'ع', new[] { 'ﻉ', 'ﻊ', 'ﻋ', 'ﻌ' } }, { 'غ', new[] { 'ﻍ', 'ﻎ', 'ﻏ', 'ﻐ' } },
            { 'ف', new[] { 'ﻑ', 'ﻒ', 'ﻓ', 'ﻔ' } }, { 'ق', new[] { 'ﻕ', 'ﻖ', 'ﻗ', 'ﻘ' } },
            { 'ک', new[] { 'ﮎ', 'ﮏ', 'ﮐ', 'ﮑ' } }, { 'ك', new[] { 'ﻙ', 'ﻚ', 'ﻛ', 'ﻜ' } },
            { 'گ', new[] { 'ﮒ', 'ﮓ', 'ﮔ', 'ﮕ' } }, { 'ل', new[] { 'ﻝ', 'ﻞ', 'ﻟ', 'ﻠ' } },
            { 'م', new[] { 'ﻡ', 'ﻢ', 'ﻣ', 'ﻤ' } }, { 'ن', new[] { 'ﻥ', 'ﻦ', 'ﻧ', 'ﻨ' } },
            { 'ں', new[] { 'ﮞ', 'ﮟ', '\0', '\0' } }, { 'و', new[] { 'ﻭ', 'ﻮ', '\0', '\0' } },
            { 'ؤ', new[] { 'ﺅ', 'ﺆ', '\0', '\0' } }, { 'ہ', new[] { 'ﮦ', 'ﮧ', 'ﮨ', 'ﮩ' } },
            { 'ه', new[] { 'ﻩ', 'ﻪ', 'ﻫ', 'ﻬ' } }, { 'ھ', new[] { 'ﮪ', 'ﮫ', 'ﮬ', 'ﮭ' } },
            { 'ی', new[] { 'ﯼ', 'ﯽ', 'ﯾ', 'ﯿ' } }, { 'ي', new[] { 'ﻱ', 'ﻲ', 'ﻳ', 'ﻴ' } },
            { 'ئ', new[] { 'ﺉ', 'ﺊ', 'ﺋ', 'ﺌ' } }, { 'ے', new[] { 'ﮮ', 'ﮯ', '\0', '\0' } },
            { 'ۓ', new[] { 'ﮰ', 'ﮱ', '\0', '\0' } }, { 'ة', new[] { 'ﺓ', 'ﺔ', '\0', '\0' } },
            { 'ء', new[] { 'ﺀ', '\0', '\0', '\0' } },
        };

        public static bool IsArabicLetter(char c) => c >= '؀' && c <= 'ۿ';

        /// <summary>Shapes and reorders one line (no '\n').</summary>
        public static string ShapeLine(string logical)
        {
            if (string.IsNullOrEmpty(logical))
            {
                return logical;
            }

            var shaped = new StringBuilder(logical.Length);
            for (var i = 0; i < logical.Length; i++)
            {
                var c = logical[i];
                if (!Forms.TryGetValue(c, out var forms))
                {
                    shaped.Append(c);
                    continue;
                }

                // Lam + Alef ligature.
                if (c == 'ل' && i + 1 < logical.Length && (logical[i + 1] == 'ا' || logical[i + 1] == 'آ'))
                {
                    var joinedBefore = JoinsForward(Previous(logical, i));
                    shaped.Append(logical[i + 1] == 'ا' ? (joinedBefore ? 'ﻼ' : 'ﻻ') : (joinedBefore ? 'ﻶ' : 'ﻵ'));
                    i++;
                    continue;
                }

                var before = JoinsForward(Previous(logical, i));
                var after = forms[2] != '\0' && Forms.ContainsKey(Next(logical, i));
                var form = before && after ? forms[3] : before ? forms[1] : after ? forms[2] : forms[0];
                shaped.Append(form != '\0' ? form : forms[0]);
            }

            return Visual(shaped.ToString());
        }

        /// <summary>Shapes multi-line text: each line separately, lines stay top-to-bottom.</summary>
        public static string Shape(string logical)
        {
            if (string.IsNullOrEmpty(logical) || logical.IndexOf('\n') < 0)
            {
                return ShapeLine(logical);
            }

            var lines = logical.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                lines[i] = ShapeLine(lines[i]);
            }

            return string.Join("\n", lines);
        }

        private static char Previous(string s, int i) => i > 0 ? s[i - 1] : '\0';

        private static char Next(string s, int i) => i + 1 < s.Length ? s[i + 1] : '\0';

        private static bool JoinsForward(char c) => Forms.TryGetValue(c, out var forms) && forms[2] != '\0';

        /// <summary>Reverses to visual (right-to-left) order, keeping digit/Latin runs left-to-right and mirroring brackets.</summary>
        private static string Visual(string s)
        {
            var output = new StringBuilder(s.Length);
            var i = s.Length - 1;
            while (i >= 0)
            {
                if (IsLtr(s[i]))
                {
                    var end = i;
                    while (i >= 0 && (IsLtr(s[i]) || (s[i] == ' ' && i > 0 && IsLtr(s[i - 1]) && i < end)))
                    {
                        i--;
                    }

                    output.Append(s, i + 1, end - i);
                    continue;
                }

                var c = s[i];
                output.Append(c == '(' ? ')' : c == ')' ? '(' : c);
                i--;
            }

            return output.ToString();
        }

        private static bool IsLtr(char c) => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '%' || c == ':' || c == '/' || c == '+' || c == '×' || c == '.';
    }
}
