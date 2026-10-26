using DesiMerge.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DesiMerge.UI.Animation
{
    /// <summary>
    /// Touch feedback for a Button: shrinks while pressed, springs back on release, plays the tap sound on click.
    /// Uses unscaled time so it works on the pause screen. Audio is bound by the screen that owns the button.
    /// </summary>
    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    public sealed class ButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float pressedScale = 0.92f;
        [SerializeField] private float speed = 18f;

        private Button _button;
        private IAudioService _audio;
        private float _target = 1f;
        private float _current = 1f;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClicked);
            enabled = false;
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnClicked);
            }
        }

        public void Bind(IAudioService audio) => _audio = audio;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_button.IsInteractable())
            {
                SetTarget(pressedScale);
            }
        }

        public void OnPointerUp(PointerEventData eventData) => SetTarget(1f);

        public void OnPointerExit(PointerEventData eventData) => SetTarget(1f);

        private void OnClicked() => _audio?.PlaySfx(SfxCue.ButtonTap);

        private void SetTarget(float target)
        {
            _target = target;
            enabled = true;
        }

        private void Update()
        {
            _current = Mathf.MoveTowards(_current, _target, speed * Time.unscaledDeltaTime);
            transform.localScale = Vector3.one * _current;
            if (Mathf.Approximately(_current, _target))
            {
                enabled = false;
            }
        }

        private void OnDisable()
        {
            // The component disables itself when settled; only reset when the button is actually hidden.
            if (!gameObject.activeInHierarchy)
            {
                _current = _target = 1f;
                transform.localScale = Vector3.one;
            }
        }
    }
}
