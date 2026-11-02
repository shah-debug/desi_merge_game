using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.UI
{
    /// <summary>
    /// Keeps a sliced pill sprite a true capsule at any height. When an Image is thinner than the sprite's rounded
    /// border, Unity squeezes the corners into pointed, lens-like ends; scaling the sprite's pixels-per-unit so its
    /// height matches the Image gives perfect half-circle ends instead.
    /// </summary>
    [RequireComponent(typeof(Image))]
    [ExecuteAlways]
    public sealed class PillShape : MonoBehaviour
    {
        private Image _image;

        private void OnEnable()
        {
            _image = GetComponent<Image>();
            Apply();
        }

        private void OnRectTransformDimensionsChange() => Apply();

        public void Apply()
        {
            if (_image == null || _image.sprite == null)
            {
                return;
            }

            var height = _image.rectTransform.rect.height;
            if (height <= 0.5f)
            {
                return;
            }

            var canvas = _image.canvas;
            var reference = canvas != null ? canvas.referencePixelsPerUnit : 100f;
            var multiplier = _image.sprite.rect.height / height * (reference / _image.sprite.pixelsPerUnit);
            if (!Mathf.Approximately(_image.pixelsPerUnitMultiplier, multiplier))
            {
                _image.pixelsPerUnitMultiplier = multiplier;
            }
        }
    }
}
