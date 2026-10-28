using System;
using UnityEngine.InputSystem;

namespace DesiMerge.Platform
{
    /// <summary>
    /// The Android back button (reported by the Input System as the Escape key; Escape on desktop). Event-driven,
    /// owned by a scene entry point: create in OnEnable, dispose in OnDisable.
    /// </summary>
    public sealed class BackButton : IDisposable
    {
        private readonly InputAction _action = new InputAction("Back", InputActionType.Button, "<Keyboard>/escape");
        private readonly Action _onPressed;

        public BackButton(Action onPressed)
        {
            _onPressed = onPressed ?? throw new ArgumentNullException(nameof(onPressed));
            _action.performed += OnPerformed;
            _action.Enable();
        }

        public void Dispose()
        {
            _action.performed -= OnPerformed;
            _action.Disable();
            _action.Dispose();
        }

        private void OnPerformed(InputAction.CallbackContext context) => _onPressed();
    }
}
