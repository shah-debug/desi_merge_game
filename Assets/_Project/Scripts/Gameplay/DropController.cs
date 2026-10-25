using System;
using System.Collections;
using DesiMerge.Configuration;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DesiMerge.Gameplay
{
    /// <summary>
    /// Player aiming and dropping. Pointer (mouse or touch) moves the aim; releasing the press requests a drop.
    /// Input is event-driven (no per-frame polling). Shows a non-physical preview of the item about to drop.
    /// Whether a drop is actually allowed is decided by <see cref="GameplayLoop"/>, not here.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DropController : MonoBehaviour
    {
        private const float PreviewAlpha = 0.6f;

        [SerializeField] private Camera worldCamera;
        [Tooltip("Visual-only preview: a SpriteRenderer with no Rigidbody2D or Collider2D.")]
        [SerializeField] private SpriteRenderer preview;

        private InputAction _pointAction;
        private InputAction _pressAction;
        private Transform _board;
        private float _halfWidth;
        private float _dropHeight;
        private float _previewRadius;
        private bool _pressArmed;
        private WaitForSeconds _previewDelay;
        private Coroutine _pendingPreview;

        /// <summary>Raised with the board-local x when the player releases a press.</summary>
        public event Action<float> DropRequested;

        /// <summary>When false, presses are ignored (game over, paused, configuration error).</summary>
        public bool InputEnabled { get; set; }

        public float AimX { get; private set; }

        public void Configure(GameplayConfig config, Transform board)
        {
            _board = board;
            _halfWidth = config.HalfWidth;
            _dropHeight = config.DropHeight;
            _previewDelay = new WaitForSeconds(config.DropCooldownSeconds);
            AimAt(0f);
        }

        /// <summary>Moves the aim to a board-local x, clamped so the current item fits between the walls.</summary>
        public void AimAt(float localX)
        {
            AimX = DropRules.ClampX(localX, _halfWidth, _previewRadius);
            if (preview != null && _board != null)
            {
                preview.transform.position = _board.TransformPoint(new Vector3(AimX, _dropHeight, 0f));
            }
        }

        public void ShowPreview(MergeItemDefinition definition)
        {
            CancelPendingPreview();
            _previewRadius = definition.Radius;
            if (preview != null)
            {
                preview.sprite = definition.Sprite;
                var tint = definition.Tint;
                tint.a = PreviewAlpha;
                preview.color = tint;
                preview.transform.localScale = Vector3.one * (definition.Radius * 2f);
                preview.enabled = true;
            }

            AimAt(AimX);
        }

        /// <summary>Shows the preview after the drop cooldown, so it doesn't overlap the item just dropped.</summary>
        public void ShowPreviewAfterCooldown(MergeItemDefinition definition)
        {
            HidePreview();
            if (isActiveAndEnabled && _previewDelay != null)
            {
                _pendingPreview = StartCoroutine(ShowPreviewLater(definition));
            }
            else
            {
                ShowPreview(definition);
            }
        }

        public void HidePreview()
        {
            CancelPendingPreview();
            if (preview != null)
            {
                preview.enabled = false;
            }
        }

        private IEnumerator ShowPreviewLater(MergeItemDefinition definition)
        {
            yield return _previewDelay;
            _pendingPreview = null;
            ShowPreview(definition);
        }

        private void CancelPendingPreview()
        {
            if (_pendingPreview != null)
            {
                StopCoroutine(_pendingPreview);
                _pendingPreview = null;
            }
        }

        private void OnEnable()
        {
            if (_pointAction == null)
            {
                _pointAction = new InputAction("Point", InputActionType.Value, "<Pointer>/position");
                _pressAction = new InputAction("Press", InputActionType.Button, "<Pointer>/press");
            }

            _pointAction.performed += OnPoint;
            _pressAction.started += OnPressStarted;
            _pressAction.canceled += OnPressReleased;
            _pointAction.Enable();
            _pressAction.Enable();
        }

        private void OnDisable()
        {
            _pointAction.performed -= OnPoint;
            _pressAction.started -= OnPressStarted;
            _pressAction.canceled -= OnPressReleased;
            _pointAction.Disable();
            _pressAction.Disable();
            _pressArmed = false;
        }

        private void OnDestroy()
        {
            _pointAction?.Dispose();
            _pressAction?.Dispose();
        }

        private void OnPoint(InputAction.CallbackContext context)
        {
            if (InputEnabled)
            {
                AimAtScreen(context.ReadValue<Vector2>());
            }
        }

        private void OnPressStarted(InputAction.CallbackContext context)
        {
            // Only a press that began during play can drop; this stops the Restart tap from also dropping an item.
            _pressArmed = InputEnabled;
            if (_pressArmed)
            {
                AimAtScreen(_pointAction.ReadValue<Vector2>());
            }
        }

        private void OnPressReleased(InputAction.CallbackContext context)
        {
            var armed = _pressArmed;
            _pressArmed = false;
            if (armed && InputEnabled)
            {
                DropRequested?.Invoke(AimX);
            }
        }

        private void AimAtScreen(Vector2 screenPosition)
        {
            if (worldCamera == null || _board == null)
            {
                return;
            }

            var world = worldCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -worldCamera.transform.position.z));
            AimAt(_board.InverseTransformPoint(world).x);
        }
    }
}
