using UnityEngine;

namespace DesiMerge.UI.Animation
{
    /// <summary>
    /// Plays a short attention animation on an icon every few seconds: <see cref="Style.Swing"/> lifts and strikes
    /// like a hammer; <see cref="Style.Hop"/> squashes, jumps and wobbles like a box being shaken. Visual only,
    /// unscaled time, returns the icon exactly to its rest pose between beats.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class IconBeat : MonoBehaviour
    {
        public enum Style
        {
            Swing = 0,
            Hop = 1,
        }

        [SerializeField] private Style style = Style.Swing;
        [SerializeField, Min(0.5f)] private float interval = 2f;
        [SerializeField, Min(0.1f)] private float duration = 0.6f;
        [SerializeField] private float strength = 1f;

        private RectTransform _rect;
        private Vector2 _restPosition;
        private Vector3 _restScale;
        private Quaternion _restRotation;
        private float _clock;

        /// <summary>True while a beat is playing (between beats the icon is at rest).</summary>
        public bool Playing => _clock < duration;

        public Style Kind => style;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _restPosition = _rect.anchoredPosition;
            _restScale = _rect.localScale;
            _restRotation = _rect.localRotation;
            _clock = duration;
        }

        private void OnDisable() => Rest();

        private void Update()
        {
            _clock += Time.unscaledDeltaTime;
            if (_clock >= interval)
            {
                _clock = 0f;
            }

            if (_clock >= duration)
            {
                Rest();
                return;
            }

            var t = _clock / duration;
            if (style == Style.Swing)
            {
                // Wind back, strike down past rest, settle.
                var angle = t < 0.35f ? Mathf.Lerp(0f, 28f, Ease(t / 0.35f))
                    : t < 0.55f ? Mathf.Lerp(28f, -22f, (t - 0.35f) / 0.2f)
                    : Mathf.Lerp(-22f, 0f, Ease((t - 0.55f) / 0.45f)) * Mathf.Cos((t - 0.55f) * 18f);
                _rect.localRotation = _restRotation * Quaternion.Euler(0f, 0f, angle * strength);
                var squash = t > 0.5f && t < 0.7f ? Mathf.Sin((t - 0.5f) / 0.2f * Mathf.PI) * 0.08f : 0f;
                _rect.localScale = new Vector3(_restScale.x * (1f + squash), _restScale.y * (1f - squash), _restScale.z);
            }
            else
            {
                // Squash, hop up with a wobble, land with a small squash.
                var hop = t > 0.15f && t < 0.75f ? Mathf.Sin((t - 0.15f) / 0.6f * Mathf.PI) : 0f;
                var squash = t < 0.15f ? Mathf.Sin(t / 0.15f * Mathf.PI) * 0.12f
                    : t > 0.75f ? Mathf.Sin((t - 0.75f) / 0.25f * Mathf.PI) * 0.1f
                    : -hop * 0.06f;
                _rect.anchoredPosition = _restPosition + new Vector2(0f, hop * 18f * strength);
                _rect.localScale = new Vector3(_restScale.x * (1f + squash), _restScale.y * (1f - squash), _restScale.z);
                _rect.localRotation = _restRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 4f) * 10f * hop * strength);
            }
        }

        private void Rest()
        {
            if (_rect == null)
            {
                return;
            }

            _rect.anchoredPosition = _restPosition;
            _rect.localScale = _restScale;
            _rect.localRotation = _restRotation;
        }

        private static float Ease(float t) => 1f - (1f - t) * (1f - t);
    }
}
