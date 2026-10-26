using UnityEngine;

namespace DesiMerge.UI.Animation
{
    /// <summary>
    /// Slides and fades an element in when it is enabled (e.g. menu title and buttons, staggered by delay).
    /// Purely visual: the element is interactable immediately.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public sealed class EntranceAnimation : MonoBehaviour
    {
        [SerializeField] private float delay;
        [SerializeField] private float duration = 0.45f;
        [SerializeField] private Vector2 offset = new Vector2(0f, -80f);
        [Tooltip("Optional; fades from 0 to 1 when present.")]
        [SerializeField] private CanvasGroup fade;

        private RectTransform _rect;
        private Vector2 _restPosition;
        private bool _captured;
        private float _elapsed;

        private void OnEnable()
        {
            _rect = (RectTransform)transform;
            if (!_captured)
            {
                _restPosition = _rect.anchoredPosition;
                _captured = true;
            }

            _elapsed = -delay;
            Apply(0f);
        }

        private void OnDisable()
        {
            if (_captured)
            {
                Apply(1f);
            }
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < 0f)
            {
                return;
            }

            var t = duration > 0f ? Mathf.Clamp01(_elapsed / duration) : 1f;
            Apply(t);
            if (t >= 1f)
            {
                enabled = false;
            }
        }

        private void Apply(float t)
        {
            _rect.anchoredPosition = _restPosition + offset * (1f - Ease.OutBack(t));
            if (fade != null)
            {
                fade.alpha = Ease.OutCubic(t);
            }
        }

        internal void SetForTests(float startDelay, float length)
        {
            delay = startDelay;
            duration = length;
        }
    }
}
