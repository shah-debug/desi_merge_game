using System;
using DesiMerge.Core;
using DesiMerge.UI.Animation;
using UnityEngine;

namespace DesiMerge.UI
{
    /// <summary>
    /// Branded intro in the Bootstrap scene, shown only at application start. The logo scales and fades in while the
    /// main menu loads in the background; the menu is activated once both the intro and the load are finished, so the
    /// splash is never longer than <see cref="IntroSeconds"/> or the real load time.
    /// </summary>
    public sealed class SplashScreen : MonoBehaviour, ISplashScreen
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform logo;
        [SerializeField] private RectTransform spinner;
        [SerializeField] private float introSeconds = 0.9f;
        [SerializeField] private float spinnerDelay = 1.2f;

        private Action _onIntroFinished;
        private float _elapsed;
        private bool _introDone;

        public float IntroSeconds => introSeconds;
        public bool IntroDone => _introDone;

        private void Awake()
        {
            if (spinner != null)
            {
                spinner.gameObject.SetActive(false);
            }

            enabled = false;
        }

        public void Play(Action onIntroFinished)
        {
            _onIntroFinished = onIntroFinished;
            _elapsed = 0f;
            _introDone = false;
            enabled = true;
            Apply(0f);
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            Apply(introSeconds <= 0f ? 1f : Mathf.Clamp01(_elapsed / introSeconds));
            if (!_introDone && _elapsed >= introSeconds)
            {
                _introDone = true;
                var callback = _onIntroFinished;
                _onIntroFinished = null;
                callback?.Invoke();
            }

            if (spinner != null && _elapsed >= spinnerDelay)
            {
                spinner.gameObject.SetActive(true);
                spinner.Rotate(0f, 0f, -360f * Time.unscaledDeltaTime);
            }
        }

        private void Apply(float t)
        {
            if (logo != null)
            {
                logo.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, Ease.OutBack(t));
            }

            if (group != null)
            {
                group.alpha = Ease.OutCubic(Mathf.Clamp01(t * 1.6f));
            }
        }
    }
}
