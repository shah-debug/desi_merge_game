using System.Collections.Generic;
using DesiMerge.UI.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.UI
{
    /// <summary>
    /// "+N" popups at merge positions. A fixed pool of Text objects is created up front; when all are busy the
    /// oldest is reused. Runs Update only while a popup is visible. Popups never receive raycasts.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class ScorePopupLayer : MonoBehaviour
    {
        [SerializeField] private Text popupTemplate;
        [SerializeField] private Camera worldCamera;
        [SerializeField, Range(1, 24)] private int poolSize = 10;
        [SerializeField] private float duration = 0.8f;
        [SerializeField] private float rise = 140f;

        private readonly Dictionary<int, string> _labels = new Dictionary<int, string>();
        private Text[] _texts;
        private float[] _age;
        private Vector2[] _start;
        private int _next;
        private int _activeCount;

        public int ActiveCount => _activeCount;

        private void Awake()
        {
            enabled = false;
            if (popupTemplate == null)
            {
                return;
            }

            popupTemplate.gameObject.SetActive(false);
            _texts = new Text[poolSize];
            _age = new float[poolSize];
            _start = new Vector2[poolSize];
            for (var i = 0; i < poolSize; i++)
            {
                _texts[i] = Instantiate(popupTemplate, transform);
                _texts[i].raycastTarget = false;
                _age[i] = -1f;
            }
        }

        /// <summary>Shows a popup at a world position. Does nothing if the layer isn't fully configured.</summary>
        public void Show(Vector3 worldPosition, int points, Color color)
        {
            if (_texts == null || worldCamera == null)
            {
                return;
            }

            var screen = worldCamera.WorldToScreenPoint(worldPosition);
            var canvas = GetComponentInParent<Canvas>();
            var uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, screen, uiCamera, out var local);

            var i = _next;
            _next = (_next + 1) % _texts.Length;
            if (_age[i] < 0f)
            {
                _activeCount++;
            }

            var text = _texts[i];
            text.text = Label(points);
            color.a = 1f;
            text.color = color;
            _start[i] = local;
            _age[i] = 0f;
            text.rectTransform.anchoredPosition = local;
            text.gameObject.SetActive(true);
            enabled = true;
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            for (var i = 0; i < _texts.Length; i++)
            {
                if (_age[i] < 0f)
                {
                    continue;
                }

                _age[i] += dt;
                var t = _age[i] / duration;
                var text = _texts[i];
                if (t >= 1f)
                {
                    _age[i] = -1f;
                    _activeCount--;
                    text.gameObject.SetActive(false);
                    continue;
                }

                text.rectTransform.anchoredPosition = _start[i] + new Vector2(0f, rise * Ease.OutCubic(t));
                text.rectTransform.localScale = Vector3.one * (t < 0.15f ? Mathf.Lerp(0.6f, 1.15f, t / 0.15f) : 1f);
                var color = text.color;
                color.a = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
                text.color = color;
            }

            if (_activeCount == 0)
            {
                enabled = false;
            }
        }

        private string Label(int points)
        {
            if (!_labels.TryGetValue(points, out var label))
            {
                label = "+" + points;
                _labels[points] = label;
            }

            return label;
        }
    }
}
