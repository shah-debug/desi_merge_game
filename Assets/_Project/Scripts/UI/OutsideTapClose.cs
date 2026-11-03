using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DesiMerge.UI
{
    /// <summary>
    /// On a modal's dimmed backdrop: tapping outside the card presses the modal's own close button (Close, Done,
    /// Back or Resume), so it closes exactly as if that button were tapped (saves, sounds, resume all still run).
    /// Modals without such a button (Game Over, reward popups) ignore outside taps.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public sealed class OutsideTapClose : MonoBehaviour, IPointerClickHandler
    {
        private static readonly string[] CloseNames = { "CloseButton", "BackButton", "DoneButton", "ResumeButton" };

        [SerializeField] private Button target;

        public Button Target => target != null ? target : target = FindCloseButton();

        public void OnPointerClick(PointerEventData eventData) => TryClose();

        /// <summary>Presses the close button if it is showing and enabled. Returns whether it did.</summary>
        public bool TryClose()
        {
            var button = Target;
            if (button == null || !button.isActiveAndEnabled || !button.interactable)
            {
                return false;
            }

            button.onClick.Invoke();
            return true;
        }

        private Button FindCloseButton()
        {
            var modal = transform.parent;
            if (modal == null)
            {
                return null;
            }

            foreach (var name in CloseNames)
            {
                foreach (var button in modal.GetComponentsInChildren<Button>(true))
                {
                    if (button.name == name && button.transform != transform)
                    {
                        return button;
                    }
                }
            }

            return null;
        }
    }
}
