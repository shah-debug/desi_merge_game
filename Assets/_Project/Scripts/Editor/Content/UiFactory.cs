using DesiMerge.UI;
using DesiMerge.UI.Animation;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DesiMerge.Editor.Content
{
    /// <summary>
    /// Builds themed uGUI elements. Every screen is assembled from these few functions (and the ThemedButton /
    /// SettingsPanel prefabs), so spacing, colours and fonts stay consistent. Reference resolution 1080 x 1920,
    /// canvas matched to width (portrait-first; taller phones get more vertical room).
    /// </summary>
    internal sealed class UiFactory
    {
        private readonly UITheme _theme;

        public UiFactory(UITheme theme)
        {
            _theme = theme;
        }

        public UITheme Theme => _theme;

        public Canvas Canvas(string name, int sortingOrder = 0)
        {
            var canvasObject = new GameObject(name, typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static void EventSystem()
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        public RectTransform SafeArea(Transform parent)
        {
            var rect = Stretch(Rect("SafeArea", parent));
            rect.gameObject.AddComponent<SafeAreaFitter>();
            return rect;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        public static RectTransform Stretch(RectTransform rect, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        /// <summary>Anchors and pivots at the same normalized point, then offsets by position.</summary>
        public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public Image Image(Transform parent, string name, Sprite sprite, Color color, bool raycast = false)
        {
            var image = Rect(name, parent).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            if (sprite != null && sprite.border != Vector4.zero)
            {
                image.type = UnityEngine.UI.Image.Type.Sliced;
            }

            return image;
        }

        public Image Panel(Transform parent, string name, Color fill, Color? rim = null, float rimWidth = 8f)
        {
            var panel = Image(parent, name, _theme.panel, fill);
            if (rim.HasValue)
            {
                var border = Image(panel.transform, "Rim", _theme.panel, rim.Value);
                Stretch(border.rectTransform, -rimWidth, -rimWidth, -rimWidth, -rimWidth);
                border.transform.SetAsFirstSibling();
                var fillImage = Image(panel.transform, "Fill", _theme.panel, fill);
                Stretch(fillImage.rectTransform);
                fillImage.transform.SetSiblingIndex(1);
                panel.enabled = false;
            }

            return panel;
        }

        public Image Strip(Transform parent, string name, float height)
        {
            var strip = Image(parent, name, _theme.borderStrip, Color.white);
            strip.type = UnityEngine.UI.Image.Type.Tiled;
            strip.rectTransform.sizeDelta = new Vector2(0f, height);
            return strip;
        }

        public Image Ornament(Transform parent, string name, Vector2 anchor, Vector2 position, float size, float alpha = 1f)
        {
            var ornament = Image(parent, name, _theme.ornament, new Color(1f, 1f, 1f, alpha));
            Place(ornament.rectTransform, anchor, position, new Vector2(size, size));
            return ornament;
        }

        public Text Text(Transform parent, string name, string content, int size, Color color, TextAnchor alignment = TextAnchor.MiddleCenter, bool outline = false, bool body = false)
        {
            var text = Rect(name, parent).gameObject.AddComponent<Text>();
            var display = !body && _theme.font != null;
            text.font = display ? _theme.font : _theme.bodyFont != null ? _theme.bodyFont : _theme.font;
            text.text = content;
            text.fontSize = size;
            text.fontStyle = display ? FontStyle.Normal : FontStyle.Bold;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            if (outline)
            {
                var effect = text.gameObject.AddComponent<Outline>();
                effect.effectColor = new Color(_theme.textDark.r, _theme.textDark.g, _theme.textDark.b, 0.85f);
                effect.effectDistance = new Vector2(4f, -4f);
            }

            return text;
        }

        /// <summary>Instance of the ThemedButton prefab, recoloured and relabelled (recorded as prefab overrides).</summary>
        public Button Button(Transform parent, string name, string label, Color color, Vector2 size, int fontSize = 0, Sprite icon = null, Color? iconColor = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ContentPaths.ButtonPrefabPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            var rect = (RectTransform)instance.transform;
            rect.sizeDelta = size;
            var image = instance.GetComponent<Image>();
            image.color = color;
            var text = instance.transform.Find("Label").GetComponent<Text>();
            text.text = label;
            text.fontSize = fontSize > 0 ? fontSize : _theme.buttonSize;
            Record(rect, image, text);

            if (icon != null)
            {
                var iconImage = instance.transform.Find("Icon").GetComponent<Image>();
                iconImage.gameObject.SetActive(true);
                iconImage.sprite = icon;
                iconImage.color = iconColor ?? Color.white;
                var iconSize = Mathf.Min(size.y * (string.IsNullOrEmpty(label) ? 0.56f : 0.46f), 96f);
                var lift = _theme.buttonLip * 0.5f;
                if (string.IsNullOrEmpty(label))
                {
                    Place(iconImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, lift), new Vector2(iconSize, iconSize));
                }
                else
                {
                    const float gap = 20f;
                    var labelWidth = Mathf.Min(text.preferredWidth, size.x - iconSize - gap - 40f);
                    var left = -(iconSize + gap + labelWidth) * 0.5f;
                    Place(iconImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(left + iconSize * 0.5f, lift), new Vector2(iconSize, iconSize));
                    text.alignment = TextAnchor.MiddleLeft;
                    Stretch(text.rectTransform, size.x * 0.5f + left + iconSize + gap, 10f, 6f, 6f + _theme.buttonLip);
                }

                Record(iconImage.gameObject, iconImage, iconImage.rectTransform, text, text.rectTransform);
            }

            return instance.GetComponent<Button>();
        }

        public Text Caption(Transform parent, string name, string content, Vector2 position, Color color, int size = 34)
        {
            var caption = Text(parent, name, content, size, color);
            Place(caption.rectTransform, new Vector2(0.5f, 0.5f), position, new Vector2(320f, 50f));
            return caption;
        }

        public static void Record(params Object[] modified)
        {
            foreach (var target in modified)
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            }
        }

        public static T Add<T>(GameObject target) where T : Component => target.AddComponent<T>();

        public static EntranceAnimation Entrance(GameObject target, float delay, Vector2 offset)
        {
            // Not "??": in the Editor GetComponent returns a fake-null object for missing components.
            if (!target.TryGetComponent<CanvasGroup>(out var fade))
            {
                fade = target.AddComponent<CanvasGroup>();
            }

            var entrance = target.AddComponent<EntranceAnimation>();
            EditorRefs.Set(entrance, ("delay", delay), ("offset", offset), ("fade", fade));
            return entrance;
        }

        public static ScalePunch Punch(GameObject target, float amplitude, float duration, bool unscaled, float squash = 0f)
        {
            var punch = target.AddComponent<ScalePunch>();
            EditorRefs.Set(punch, ("amplitude", amplitude), ("duration", duration), ("unscaledTime", unscaled), ("squash", squash));
            punch.enabled = false;
            return punch;
        }

        /// <summary>Full-screen dim + centred framed card. Returns the transition (on the root) and the card.</summary>
        public (PanelTransition Transition, RectTransform Card) Modal(Transform parent, string name, Vector2 cardSize, string title, Color? header = null)
        {
            var root = Stretch(Rect(name, parent));
            root.gameObject.AddComponent<CanvasGroup>();
            var dim = Image(root, "Dim", null, _theme.overlay, raycast: true);
            Stretch(dim.rectTransform);
            var cardImage = Panel(root, "Card", _theme.card, _theme.accent, 10f);
            var card = cardImage.rectTransform;
            Place(card, new Vector2(0.5f, 0.5f), Vector2.zero, cardSize);
            var shadow = card.Find("Rim").gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.3f);
            shadow.effectDistance = new Vector2(0f, -16f);

            var ribbon = Panel(card, "Header", header ?? _theme.deep).rectTransform;
            ribbon.anchorMin = new Vector2(0f, 1f);
            ribbon.anchorMax = new Vector2(1f, 1f);
            ribbon.pivot = new Vector2(0.5f, 1f);
            ribbon.offsetMin = new Vector2(16f, -166f);
            ribbon.offsetMax = new Vector2(-16f, -16f);
            var strip = Strip(ribbon, "Strip", 26f).rectTransform;
            strip.anchorMin = new Vector2(0f, 0f);
            strip.anchorMax = new Vector2(1f, 0f);
            strip.pivot = new Vector2(0.5f, 1f);
            strip.offsetMin = new Vector2(46f, -13f);
            strip.offsetMax = new Vector2(-46f, 13f);
            var heading = Text(ribbon, "Title", title, _theme.headingSize, _theme.accent, outline: true);
            Stretch(heading.rectTransform, 100f, 100f, 0f, 16f);
            Ornament(ribbon, "OrnamentLeft", new Vector2(0f, 0.5f), new Vector2(18f, 8f), 76f);
            Ornament(ribbon, "OrnamentRight", new Vector2(1f, 0.5f), new Vector2(-18f, 8f), 76f);

            var transition = root.gameObject.AddComponent<PanelTransition>();
            EditorRefs.Set(transition, ("card", card), ("duration", _theme.panelDuration));
            return (transition, card);
        }

        public Slider Slider(Transform parent, string name, Vector2 position, float width)
        {
            var root = Place(Rect(name, parent), new Vector2(0.5f, 0.5f), position, new Vector2(width, 90f));
            var background = Image(root, "Background", _theme.pill, new Color(_theme.deep.r, _theme.deep.g, _theme.deep.b, 0.14f));
            Stretch(background.rectTransform, 0f, 0f, 26f, 26f);

            var fillArea = Stretch(Rect("Fill Area", root), 24f, 24f, 26f, 26f);
            var fill = Image(fillArea, "Fill", _theme.pill, _theme.accent);
            Stretch(fill.rectTransform, -24f, 0f);

            var handleArea = Stretch(Rect("Handle Slide Area", root), 45f, 45f);
            var handle = Image(handleArea, "Handle", _theme.disc, _theme.deep, raycast: true);
            handle.rectTransform.sizeDelta = new Vector2(90f, 0f);
            handle.rectTransform.anchorMin = new Vector2(0f, 0f);
            handle.rectTransform.anchorMax = new Vector2(0f, 1f);
            var knob = Image(handle.transform, "Knob", _theme.disc, Color.white);
            Stretch(knob.rectTransform, 12f, 12f, 12f, 12f);

            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;
            return slider;
        }

        /// <summary>Square checkbox (min touch target) with a caption to its right.</summary>
        public Toggle Toggle(Transform parent, string name, string caption, Vector2 position)
        {
            var root = Place(Rect(name, parent), new Vector2(0.5f, 0.5f), position, new Vector2(240f, 100f));
            var box = Image(root, "Box", _theme.panel, new Color(_theme.deep.r, _theme.deep.g, _theme.deep.b, 0.14f), raycast: true);
            Place(box.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(100f, 100f));
            var check = Image(box.transform, "Check", GameArtGenerator.Icon("Check"), _theme.primary);
            Stretch(check.rectTransform, 16f, 16f, 16f, 16f);
            var label = Text(root, "Label", caption, _theme.bodySize, _theme.textDark, TextAnchor.MiddleLeft);
            Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(120f, 0f), new Vector2(130f, 100f));

            var toggle = root.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = box;
            toggle.graphic = check;
            toggle.isOn = false;
            return toggle;
        }
    }
}
