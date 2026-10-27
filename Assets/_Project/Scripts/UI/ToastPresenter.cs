using System.Collections.Generic;
using DesiMerge.UI.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.UI
{
    /// <summary>
    /// Shows short messages one at a time (slide/fade in, hold, fade out), queuing the rest. Never takes input.
    /// Uses unscaled time so it isn't frozen by pause, and runs Update only while a message is showing.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ToastPresenter : MonoBehaviour
    {
        private const int MaxQueued = 6;

        [SerializeField] private Text label;
        [SerializeField] private float fadeSeconds = 0.2f;
        [SerializeField] private float holdSeconds = 1.6f;

        private readonly Queue<string> _queue = new Queue<string>();
        private CanvasGroup _group;
        private float _elapsed;
        private bool _showing;

        public int QueuedCount => _queue.Count;
        public bool IsShowing => _showing;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;
            enabled = false;
        }

        public void Show(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            if (_queue.Count >= MaxQueued)
            {
                _queue.Dequeue();
            }

            _queue.Enqueue(message);
            if (!_showing)
            {
                Next();
            }
        }

        private void Next()
        {
            if (_queue.Count == 0)
            {
                _showing = false;
                _group.alpha = 0f;
                enabled = false;
                return;
            }

            if (label != null)
            {
                label.text = _queue.Dequeue();
            }
            else
            {
                _queue.Dequeue();
            }

            _showing = true;
            _elapsed = 0f;
            enabled = true;
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            var total = fadeSeconds * 2f + holdSeconds;
            if (_elapsed >= total)
            {
                Next();
                return;
            }

            var alpha = _elapsed < fadeSeconds
                ? Ease.OutCubic(_elapsed / fadeSeconds)
                : _elapsed > fadeSeconds + holdSeconds
                    ? 1f - (_elapsed - fadeSeconds - holdSeconds) / fadeSeconds
                    : 1f;
            _group.alpha = alpha;
        }
    }
}
