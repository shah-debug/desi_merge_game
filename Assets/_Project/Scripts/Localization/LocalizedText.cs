using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.Localization
{
    /// <summary>
    /// On every Text: keeps the right font and alignment for the current language, and translates the Text's fixed
    /// English content (its key). Texts whose content is set by code have an empty key; code translates them through
    /// <see cref="Loc"/>, and this component only switches their font and mirrors their alignment for Urdu.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string key;

        private Text _text;
        private Font _baseFont;
        private TextAnchor _baseAlignment;
        private bool _captured;

        public string Key => key;

        internal void SetKey(string englishKey) => key = englishKey;

        private void Awake()
        {
            Capture();
            Loc.Changed += Apply;
            Apply();
        }

        private void OnDestroy() => Loc.Changed -= Apply;

        private void Capture()
        {
            if (_captured)
            {
                return;
            }

            _text = GetComponent<Text>();
            _baseFont = _text.font;
            _baseAlignment = _text.alignment;
            _captured = true;
        }

        public void Apply()
        {
            Capture();
            _text.font = Loc.FontFor(_baseFont);
            _text.alignment = Loc.IsRightToLeft ? Mirror(_baseAlignment) : _baseAlignment;
            if (!string.IsNullOrEmpty(key))
            {
                Loc.Set(_text, key);
            }
        }

        private static TextAnchor Mirror(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft: return TextAnchor.UpperRight;
                case TextAnchor.UpperRight: return TextAnchor.UpperLeft;
                case TextAnchor.MiddleLeft: return TextAnchor.MiddleRight;
                case TextAnchor.MiddleRight: return TextAnchor.MiddleLeft;
                case TextAnchor.LowerLeft: return TextAnchor.LowerRight;
                case TextAnchor.LowerRight: return TextAnchor.LowerLeft;
                default: return anchor;
            }
        }
    }
}
