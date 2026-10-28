using UnityEngine;

namespace DesiMerge.UI.Animation
{
    /// <summary>
    /// Briefly scales its transform up and back. Only runs Update while playing.
    /// Put it on a visual-only transform: never on an object whose collider must keep a fixed size.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScalePunch : MonoBehaviour
    {
        [SerializeField] private float amplitude = 0.25f;
        [SerializeField] private float duration = 0.25f;
        [Tooltip("Use for UI that must animate while the game is paused (Time.timeScale = 0).")]
        [SerializeField] private bool unscaledTime;
        [SerializeField] private float squash;

        private Vector3 _baseScale = Vector3.one;
        private float _elapsed;
        private float _currentAmplitude;

        public bool IsPlaying => enabled;

        private void Awake()
        {
            _baseScale = transform.localScale;
            enabled = false;
        }

        public void Play() => Play(amplitude);

        public void Play(float customAmplitude)
        {
            _currentAmplitude = customAmplitude;
            _elapsed = 0f;
            enabled = true;
        }

        private void Update()
        {
            _elapsed += unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            var t = duration > 0f ? _elapsed / duration : 1f;
            if (t >= 1f)
            {
                enabled = false;
                return;
            }

            var punch = Ease.Punch(t);
            var scale = _baseScale * (1f + _currentAmplitude * punch);
            scale.x *= 1f + squash * punch;
            scale.y *= 1f - squash * punch;
            transform.localScale = scale;
        }

        private void OnDisable()
        {
            transform.localScale = _baseScale;
        }
    }
}
