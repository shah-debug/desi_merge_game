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

        /// <summary>Rounded panel with the painted truck-art frame on top.</summary>
        public Image FramedPanel(Transform parent, string name, Color fill, Color frame)
        {
            var panel = Image(parent, name, _theme.roundedPanel, fill);
            var border = Image(panel.transform, "Frame", _theme.paintedFrame, frame);
            Stretch(border.rectTransform);
            return panel;
        }

        public Text Text(Transform parent, string name, string content, int size, Color color, TextAnchor alignment = TextAnchor.MiddleCenter, bool outline = false)
        {
            var text = Rect(name, parent).gameObject.AddComponent<Text>();
            text.font = _theme.font;
            text.text = content;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
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
        public Button Button(Transform parent, string name, string label, Color color, Vector2 size, int fontSize = 0)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ContentPaths.ButtonPrefabPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            var rect = (RectTransform)instance.transform;
            rect.sizeDelta = size;
            var image = instance.GetComponent<Image>();
            image.color = color;
            var text = instance.GetComponentInChildren<Text>(true);
            text.text = label;
            text.fontSize = fontSize > 0 ? fontSize : _theme.buttonSize;
            Record(rect, image, text);
            return instance.GetComponent<Button>();
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

        public static ScalePunch Punch(GameObject target, float amplitude, float duration, bool unscaled)
        {
            var punch = target.AddComponent<ScalePunch>();
            EditorRefs.Set(punch, ("amplitude", amplitude), ("duration", duration), ("unscaledTime", unscaled));
            punch.enabled = false;
            return punch;
        }

        /// <summary>Full-screen dim + centred framed card. Returns the transition (on the root) and the card.</summary>
        public (PanelTransition Transition, RectTransform Card) Modal(Transform parent, string name, Vector2 cardSize, Color cardColor)
        {
            var root = Stretch(Rect(name, parent));
            root.gameObject.AddComponent<CanvasGroup>();
            var dim = Image(root, "Dim", null, _theme.overlay, raycast: true);
            Stretch(dim.rectTransform);
            var card = FramedPanel(root, "Card", cardColor, _theme.accent).rectTransform;
            Place(card, new Vector2(0.5f, 0.5f), Vector2.zero, cardSize);
            var transition = root.gameObject.AddComponent<PanelTransition>();
            EditorRefs.Set(transition, ("card", card));
            return (transition, card);
        }

        public Slider Slider(Transform parent, string name, Vector2 position, float width)
        {
            var root = Place(Rect(name, parent), new Vector2(0.5f, 0.5f), position, new Vector2(width, 90f));
            var background = Image(root, "Background", _theme.roundedPanel, new Color(0f, 0f, 0f, 0.35f));
            Stretch(background.rectTransform, 0f, 0f, 28f, 28f);

            var fillArea = Stretch(Rect("Fill Area", root), 24f, 24f, 28f, 28f);
            var fill = Image(fillArea, "Fill", _theme.roundedPanel, _theme.accent);
            Stretch(fill.rectTransform, -24f, 0f);

            var handleArea = Stretch(Rect("Handle Slide Area", root), 45f, 45f);
            var handle = Image(handleArea, "Handle", _theme.circle, Color.white, raycast: true);
            handle.rectTransform.sizeDelta = new Vector2(90f, 0f);
            handle.rectTransform.anchorMin = new Vector2(0f, 0f);
            handle.rectTransform.anchorMax = new Vector2(0f, 1f);

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
            var box = Image(root, "Box", _theme.roundedPanel, Color.white, raycast: true);
            Place(box.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(100f, 100f));
            var check = Image(box.transform, "Check", _theme.roundedPanel, _theme.primary);
            Stretch(check.rectTransform, 18f, 18f, 18f, 18f);
            var label = Text(root, "Label", caption, _theme.bodySize, _theme.textLight, TextAnchor.MiddleLeft);
            Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(120f, 0f), new Vector2(130f, 100f));

            var toggle = root.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = box;
            toggle.graphic = check;
            toggle.isOn = false;
            return toggle;
        }
    }
}
