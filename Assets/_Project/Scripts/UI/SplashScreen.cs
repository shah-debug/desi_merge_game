using System;
using DesiMerge.Core;
using DesiMerge.UI.Animation;
using UnityEngine;

namespace DesiMerge.UI
{
    /// <summary>
    /// Branded 2-second intro in the Bootstrap scene, shown only at application start while the main menu loads in
    /// the background. Sequence: the glow and truck-art flower bloom, the mango drops in and bounces, the title
    /// plate pops, the tagline rises, small items float around, and a loading bar fills to the end. The menu is
    /// activated once both the intro and the load are finished. Unscaled time; every part is optional.
    /// </summary>
    public sealed class SplashScreen : MonoBehaviour, ISplashScreen
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform logo;
        [SerializeField] private RectTransform spinner;
        [SerializeField] private RectTransform glow;
        [SerializeField] private RectTransform flower;
        [SerializeField] private RectTransform hero;
        [SerializeField] private RectTransform plate;
        [SerializeField] private CanvasGroup tagline;
        [SerializeField] private RectTransform loadingFill;
        [SerializeField] private RectTransform[] floaters = Array.Empty<RectTransform>();
        [SerializeField] private float introSeconds = 2f;

        private Action _onIntroFinished;
        private float _elapsed;
        private bool _introDone;
        private Vector2 _heroRest;
        private Vector2 _taglineRest;
        private Vector2[] _floaterRest = Array.Empty<Vector2>();

        public float IntroSeconds => introSeconds;
        public bool IntroDone => _introDone;

        private void Awake()
        {
            if (spinner != null)
            {
                spinner.gameObject.SetActive(false);
            }

            if (hero != null)
            {
                _heroRest = hero.anchoredPosition;
            }

            if (tagline != null)
            {
                _taglineRest = ((RectTransform)tagline.transform).anchoredPosition;
            }

            _floaterRest = new Vector2[floaters.Length];
            for (var i = 0; i < floaters.Length; i++)
            {
                _floaterRest[i] = floaters[i] != null ? floaters[i].anchoredPosition : Vector2.zero;
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
            Apply(_elapsed);
            if (!_introDone && _elapsed >= introSeconds)
            {
                _introDone = true;
                var callback = _onIntroFinished;
                _onIntroFinished = null;
                callback?.Invoke();
            }
        }

        /// <summary>0..1 progress of a step that starts at <paramref name="from"/> seconds and lasts <paramref name="length"/>.</summary>
        private static float Step(float time, float from, float length) => Mathf.Clamp01((time - from) / length);

        private void Apply(float time)
        {
            if (group != null)
            {
                group.alpha = Ease.OutCubic(Step(time, 0f, 0.25f));
            }

            if (logo != null)
            {
                // A gentle breathing scale once everything has landed.
                logo.localScale = Vector3.one * (1f + Mathf.Sin(Mathf.Max(0f, time - 1.1f) * 3f) * 0.012f);
            }

            if (glow != null)
            {
                var bloom = Ease.OutCubic(Step(time, 0f, 0.6f));
                glow.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f + Mathf.Sin(time * 4f) * 0.04f, bloom);
            }

            if (flower != null)
            {
                flower.localScale = Vector3.one * Mathf.LerpUnclamped(0.2f, 1f, Ease.OutBack(Step(time, 0.05f, 0.6f)));
                flower.localRotation = Quaternion.Euler(0f, 0f, -time * 25f);
            }

            if (hero != null)
            {
                // Drops from above, squashes on landing, settles.
                var fall = Step(time, 0.15f, 0.45f);
                var y = Mathf.Lerp(700f, 0f, fall * fall);
                var landing = Step(time, 0.6f, 0.35f);
                var squash = landing > 0f && landing < 1f ? Mathf.Sin(landing * Mathf.PI) * 0.16f * (1f - landing) : 0f;
                var bounce = landing > 0f && landing < 1f ? Mathf.Sin(landing * Mathf.PI) * 40f * (1f - landing) : 0f;
                hero.anchoredPosition = _heroRest + new Vector2(0f, y + bounce);
                hero.localScale = new Vector3(1f + squash, 1f - squash, 1f);
                hero.gameObject.SetActive(time >= 0.15f);
            }

            if (plate != null)
            {
                var pop = Step(time, 0.5f, 0.45f);
                plate.localScale = Vector3.one * (pop <= 0f ? 0f : Mathf.LerpUnclamped(0.4f, 1f, Ease.OutBack(pop)));
            }

            if (tagline != null)
            {
                var rise = Ease.OutCubic(Step(time, 0.85f, 0.4f));
                tagline.alpha = rise;
                ((RectTransform)tagline.transform).anchoredPosition = _taglineRest + new Vector2(0f, (1f - rise) * -40f);
            }

            for (var i = 0; i < floaters.Length; i++)
            {
                if (floaters[i] == null)
                {
                    continue;
                }

                var appear = Ease.OutBack(Step(time, 0.3f + i * 0.12f, 0.4f));
                floaters[i].localScale = Vector3.one * Mathf.Max(0f, appear);
                floaters[i].anchoredPosition = _floaterRest[i] + new Vector2(0f, Mathf.Sin(time * 2.2f + i * 1.7f) * 14f);
                floaters[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 1.6f + i) * 8f);
            }

            if (loadingFill != null)
            {
                // Width grows from a round dot to the full track.
                var amount = Ease.OutCubic(Step(time, 0.2f, introSeconds - 0.3f));
                loadingFill.anchorMax = new Vector2(Mathf.Lerp(0.06f, 1f, amount), 1f);
            }
        }
    }
}
