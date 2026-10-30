using System;
using DesiMerge.Audio;
using DesiMerge.UI.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.UI
{
    /// <summary>Base for a full-screen menu panel: animated open/close, a close button and tap sounds.</summary>
    public abstract class MenuPanel : MonoBehaviour
    {
        [SerializeField] private PanelTransition transition;
        [SerializeField] private Button closeButton;

        private bool _opening;

        public event Action Closed;

        public bool IsOpen => transition != null ? transition.IsVisible : gameObject.activeSelf;

        protected virtual void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            if (_opening)
            {
                return;
            }

            if (transition != null)
            {
                transition.SetVisibleImmediate(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        protected virtual void OnDestroy()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }
        }

        protected void BindAudio(IAudioService audio)
        {
            foreach (var feedback in GetComponentsInChildren<ButtonFeedback>(true))
            {
                feedback.Bind(audio);
            }
        }

        public void Open()
        {
            _opening = true;
            OnOpening();
            if (transition != null)
            {
                transition.Show();
            }
            else
            {
                gameObject.SetActive(true);
            }
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            if (transition != null)
            {
                transition.Hide();
            }
            else
            {
                gameObject.SetActive(false);
            }

            Closed?.Invoke();
        }

        protected abstract void OnOpening();
    }
}
