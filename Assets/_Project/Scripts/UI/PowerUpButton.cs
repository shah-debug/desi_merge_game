using System;
using DesiMerge.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.UI
{
    /// <summary>One power-up control: title, uses left and a highlight while it waits for a target. Presentation only.</summary>
    [DisallowMultipleComponent]
    public sealed class PowerUpButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Text titleText;
        [SerializeField] private Text countText;
        [SerializeField] private GameObject selectedHighlight;
        [SerializeField] private CanvasGroup contentGroup;
        [SerializeField, Range(0f, 1f)] private float unavailableAlpha = 0.5f;

        public event Action<string> Clicked;

        public string PowerUpId { get; private set; }
        public bool Interactable => button != null && button.interactable;
        public string CountText => countText != null ? countText.text : string.Empty;
        public bool ShowsSelected => selectedHighlight != null && selectedHighlight.activeSelf;
        public bool ShowsUnavailable => contentGroup != null && contentGroup.alpha < 1f;

        private void Awake()
        {
            if (button != null)
            {
                button.onClick.AddListener(OnClicked);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnClicked);
            }
        }

        public void Show(PowerUpButtonState state)
        {
            PowerUpId = state.Id;
            gameObject.SetActive(true);
            if (titleText != null)
            {
                titleText.text = state.Title.ToUpperInvariant();
            }

            if (countText != null)
            {
                countText.text = state.Available.ToString();
            }

            if (button != null)
            {
                button.interactable = state.Interactable;
            }

            if (selectedHighlight != null)
            {
                selectedHighlight.SetActive(state.Selected);
            }

            if (contentGroup != null)
            {
                contentGroup.alpha = state.Interactable || state.Selected ? 1f : unavailableAlpha;
            }
        }

        public void Hide()
        {
            PowerUpId = null;
            gameObject.SetActive(false);
        }

        private void OnClicked()
        {
            if (PowerUpId != null)
            {
                Clicked?.Invoke(PowerUpId);
            }
        }

        internal void SetReferencesForTests(Button target, Text title, Text count, GameObject highlight)
        {
            button = target;
            titleText = title;
            countText = count;
            selectedHighlight = highlight;
        }
    }
}
