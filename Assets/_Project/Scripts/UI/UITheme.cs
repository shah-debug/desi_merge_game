using UnityEngine;

namespace DesiMerge.UI
{
    /// <summary>
    /// Shared look of the UI: truck-art inspired palette, generated sprites, fonts, spacing and motion.
    /// Screens are generated from this by the editor setup tool, so changing a colour here and re-running
    /// "Rebuild Generated Scenes" restyles every screen consistently. See Documentation/VisualDesignSystem.md.
    /// </summary>
    [CreateAssetMenu(fileName = "UITheme", menuName = "Desi Merge/UI Theme")]
    public sealed class UITheme : ScriptableObject
    {
        [Header("Palette")]
        public Color background = new Color(1f, 0.957f, 0.871f);
        public Color primary = new Color(0.878f, 0.129f, 0.541f);
        public Color secondary = new Color(0.055f, 0.624f, 0.541f);
        public Color accent = new Color(1f, 0.714f, 0.153f);
        public Color deep = new Color(0.106f, 0.122f, 0.294f);
        public Color danger = new Color(1f, 0.369f, 0.341f);
        public Color textDark = new Color(0.165f, 0.102f, 0.2f);
        public Color textLight = Color.white;
        public Color overlay = new Color(0.08f, 0.05f, 0.12f, 0.72f);
        public Color card = new Color(1f, 0.976f, 0.933f);
        public Color mutedText = new Color(0.36f, 0.33f, 0.45f);

        [Header("Sprites (generated)")]
        public Sprite roundedPanel;
        public Sprite paintedFrame;
        public Sprite pattern;
        public Sprite flower;
        public Sprite circle;
        public Sprite pauseIcon;
        public Sprite panel;
        public Sprite button;
        public Sprite pill;
        public Sprite disc;
        public Sprite glow;
        public Sprite ornament;
        public Sprite borderStrip;
        public Sprite backgroundGradient;

        [Header("Typography")]
        public Font font;
        public Font bodyFont;
        public int titleSize = 150;
        public int headingSize = 84;
        public int buttonSize = 64;
        public int bodySize = 40;

        [Header("Spacing (reference 1080 x 1920)")]
        public float margin = 40f;
        public float minTouchTarget = 120f;
        public float buttonLip = 10f;

        [Header("Motion")]
        public float pressedScale = 0.92f;
        public float panelDuration = 0.22f;
        public float punchDuration = 0.25f;
    }
}
