using UnityEngine;

namespace DesiMerge.UI.Animation
{
    /// <summary>
    /// Fades a panel (and pops an optional card) in or out using unscaled time, so it works while paused.
    /// Input is blocked while hiding and the GameObject is deactivated at the end. Show/Hide are idempotent,
    /// and gameplay state never waits for the animation.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [DisallowMultipleComponent]
    public sealed class PanelTransition : MonoBehaviour
    {
        [SerializeField] private float duration = 0.22f;
        [Tooltip("Optional child (e.g. the card) that scales in with an overshoot.")]
        [SerializeField] private RectTransform card;
        [SerializeField] private float cardStartScale = 0.8f;

        private CanvasGroup _group;
        private float _elapsed;
        private bool _showing;

        public bool IsVisible => _showing && gameObject.activeSelf;

        private CanvasGroup Group => _group != null ? _group : _group = GetComponent<CanvasGroup>();

        public void Show()
        {
            if (_showing && gameObject.activeSelf)
            {
                return;
            }

            _showing = true;
            _elapsed = 0f;
            gameObject.SetActive(true);
            Group.alpha = 0f;
            Group.interactable = true;
            Group.blocksRaycasts = true;
            ApplyCard(0f);
            enabled = true;
        }

        public void Hide()
        {
            if (!_showing && !gameObject.activeSelf)
            {
                return;
            }

            _showing = false;
            Group.interactable = false;
            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(false);
                return;
            }

            _elapsed = 0f;
            enabled = true;
        }

        /// <summary>Jumps to the final state without animating (initial scene state, reduced effects).</summary>
        public void SetVisibleImmediate(bool visible)
        {
            _showing = visible;
            enabled = false;
            Group.alpha = visible ? 1f : 0f;
            Group.interactable = visible;
            Group.blocksRaycasts = visible;
            ApplyCard(1f);
            gameObject.SetActive(visible);
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            var t = duration > 0f ? Mathf.Clamp01(_elapsed / duration) : 1f;

            if (_showing)
            {
                Group.alpha = Ease.OutCubic(t);
                ApplyCard(t);
            }
            else
            {
                Group.alpha = 1f - Ease.InCubic(t);
            }

            if (t < 1f)
            {
                return;
            }

            enabled = false;
            if (!_showing)
            {
                Group.blocksRaycasts = false;
                gameObject.SetActive(false);
            }
        }

        private void ApplyCard(float t)
        {
            if (card != null)
            {
                card.localScale = Vector3.one * Mathf.LerpUnclamped(cardStartScale, 1f, Ease.OutBack(t));
            }
        }
    }
}
