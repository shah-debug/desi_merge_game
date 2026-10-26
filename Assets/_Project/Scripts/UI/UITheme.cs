using UnityEngine;

namespace DesiMerge.UI
{
    /// <summary>
    /// Shared look of the UI: truck-art inspired palette, generated placeholder sprites and font.
    /// Screens are generated from this by the editor setup tool, so changing a colour here and re-running
    /// "Rebuild Generated Scenes" restyles every screen consistently.
    /// </summary>
    [CreateAssetMenu(fileName = "UITheme", menuName = "Desi Merge/UI Theme")]
    public sealed class UITheme : ScriptableObject
    {
        [Header("Palette")]
        public Color background = new Color(1f, 0.953f, 0.84f);
        public Color primary = new Color(0.85f, 0.11f, 0.38f);     // truck-art magenta
        public Color secondary = new Color(0f, 0.54f, 0.48f);      // teal
        public Color accent = new Color(1f, 0.70f, 0f);            // saffron
        public Color deep = new Color(0.10f, 0.14f, 0.49f);        // royal blue
        public Color danger = new Color(0.90f, 0.22f, 0.21f);
        public Color textDark = new Color(0.17f, 0.11f, 0.07f);
        public Color textLight = Color.white;
        public Color overlay = new Color(0.08f, 0.05f, 0.12f, 0.72f);

        [Header("Sprites (generated placeholders)")]
        public Sprite roundedPanel;
        public Sprite paintedFrame;
        public Sprite pattern;
        public Sprite flower;
        public Sprite circle;
        public Sprite pauseIcon;

        [Header("Typography")]
        public Font font;
        public int titleSize = 150;
        public int headingSize = 84;
        public int buttonSize = 64;
        public int bodySize = 40;

        [Header("Spacing (reference 1080 x 1920)")]
        public float margin = 40f;
        public float minTouchTarget = 120f;
    }
}
