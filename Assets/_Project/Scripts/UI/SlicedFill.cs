using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.UI
{
    /// <summary>
    /// Rounded progress fill. A pill sprite drawn with Image.Type.Filled is cut and squashed, so its rounded ends look
    /// wrong. This keeps the Image sliced (perfect rounded ends at any width) and turns <see cref="Image.fillAmount"/>
    /// into the fill's width instead. Code keeps setting fillAmount as before; there is no polling.
    /// </summary>
    [RequireComponent(typeof(Image))]
    [ExecuteAlways]
    public sealed class SlicedFill : MonoBehaviour
    {
        private Image _image;
        private float _applied = -1f;

        private void OnEnable()
        {
            _image = GetComponent<Image>();
            _image.type = Image.Type.Sliced;
            _image.RegisterDirtyVerticesCallback(Apply);
            _applied = -1f;
            Apply();
        }

        private void OnDisable()
        {
            if (_image != null)
            {
                _image.UnregisterDirtyVerticesCallback(Apply);
            }
        }

        private void OnRectTransformDimensionsChange() => Apply();

        private void Apply()
        {
            if (_image == null)
            {
                return;
            }

            var fill = Mathf.Clamp01(_image.fillAmount);
            var rect = _image.rectTransform;
            var parent = rect.parent as RectTransform;
            var trackWidth = parent != null ? parent.rect.width : 0f;
            var height = rect.rect.height;
            _image.canvasRenderer.cull = fill <= 0.001f;
            if (Mathf.Approximately(fill, _applied) && trackWidth > 0f)
            {
                return;
            }

            _applied = fill;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(fill, 1f);
            var minimum = fill > 0f && trackWidth > 0f ? Mathf.Max(0f, height - fill * trackWidth) : 0f;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = new Vector2(minimum, 0f);
        }
    }
}
