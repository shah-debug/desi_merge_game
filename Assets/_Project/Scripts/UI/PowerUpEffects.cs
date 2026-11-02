using DesiMerge.Gameplay;
using UnityEngine;

namespace DesiMerge.UI
{
    /// <summary>
    /// Power-up presentation, purely visual: the camera zooms in while Hammer waits for a target and briefly during a
    /// Shuffle; a hammer swings onto the smashed item (a stand-in copy of its art is squashed, because the real item
    /// has already been removed by the rules). Never touches physics, scores or input.
    /// </summary>
    public sealed class PowerUpEffects : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private MergeBoard board;
        [SerializeField] private MergeEffects mergeEffects;
        [SerializeField] private SpriteRenderer hammer;
        [SerializeField] private SpriteRenderer ghost;
        [SerializeField, Range(0.6f, 1f)] private float focusZoom = 0.88f;
        [SerializeField] private float zoomSharpness = 10f;
        [SerializeField] private float shuffleFocusSeconds = 0.6f;
        [SerializeField] private float hammerSeconds = 0.42f;
        [SerializeField] private float impactAt = 0.12f;

        private float _baseSize = -1f;
        private float _zoom = 1f;
        private bool _focus;
        private float _pulseUntil;
        private float _hammerElapsed = -1f;
        private bool _impacted;
        private Vector3 _target;
        private float _radius;
        private Vector3 _ghostScale;
        private Color _ghostColor;

        public float CurrentZoom => _zoom;
        public bool HammerPlaying => _hammerElapsed >= 0f;

        /// <summary>Subscribes once here: the component disables itself when idle, so OnEnable is not a reliable place.</summary>
        private void Awake()
        {
            SetVisible(hammer, false);
            SetVisible(ghost, false);
            if (board != null)
            {
                board.ItemRemoving += OnSmash;
                board.Shuffled += OnShuffled;
            }

            enabled = false;
        }

        private void OnDestroy()
        {
            if (board != null)
            {
                board.ItemRemoving -= OnSmash;
                board.Shuffled -= OnShuffled;
            }

            if (worldCamera != null && _baseSize > 0f)
            {
                worldCamera.orthographicSize = _baseSize;
            }
        }

        /// <summary>Zoom in while Hammer waits for a target.</summary>
        public void SetFocus(bool focus)
        {
            _focus = focus;
            enabled = true;
        }

        private void OnShuffled()
        {
            _pulseUntil = Time.unscaledTime + shuffleFocusSeconds;
            enabled = true;
        }

        private void OnSmash(MergeItem item)
        {
            if (hammer == null || ghost == null)
            {
                return;
            }

            _target = item.transform.position;
            _radius = item.Radius;
            ghost.sprite = item.VisualSprite;
            _ghostColor = item.VisualColor;
            ghost.color = _ghostColor;
            _ghostScale = item.VisualScale;
            ghost.transform.position = _target;
            ghost.transform.rotation = item.VisualRotation;
            ghost.transform.localScale = _ghostScale;
            SetVisible(ghost, true);
            SetVisible(hammer, true);
            hammer.color = Color.white;
            hammer.transform.localScale = Vector3.one * Mathf.Max(0.9f, _radius * 1.6f);
            _hammerElapsed = 0f;
            _impacted = false;
            enabled = true;
        }

        private void Update()
        {
            var dt = Time.unscaledDeltaTime;
            UpdateZoom(dt);
            UpdateHammer(dt);
            if (!HammerPlaying && Mathf.Approximately(_zoom, 1f) && !_focus && Time.unscaledTime >= _pulseUntil)
            {
                enabled = false;
            }
        }

        private void UpdateZoom(float dt)
        {
            if (worldCamera == null)
            {
                return;
            }

            var wanted = _focus || Time.unscaledTime < _pulseUntil ? focusZoom : 1f;
            if (Mathf.Approximately(_zoom, 1f))
            {
                if (Mathf.Approximately(wanted, 1f))
                {
                    return;
                }

                _baseSize = worldCamera.orthographicSize;
            }

            _zoom = Mathf.Lerp(_zoom, wanted, 1f - Mathf.Exp(-zoomSharpness * dt));
            if (Mathf.Abs(_zoom - wanted) < 0.002f)
            {
                _zoom = wanted;
            }

            worldCamera.orthographicSize = _baseSize * _zoom;
        }

        private void UpdateHammer(float dt)
        {
            if (!HammerPlaying)
            {
                return;
            }

            _hammerElapsed += dt;
            var reach = _radius * 0.55f + 0.25f;
            if (_hammerElapsed < impactAt)
            {
                var t = _hammerElapsed / impactAt;
                var eased = t * t;
                hammer.transform.position = _target + Vector3.Lerp(new Vector3(reach + 0.7f, reach + 0.9f, 0f), new Vector3(reach * 0.55f, reach * 0.6f, 0f), eased);
                hammer.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(55f, -20f, eased));
                return;
            }

            if (!_impacted)
            {
                _impacted = true;
                if (mergeEffects != null)
                {
                    mergeEffects.PlayMerge(_target, _ghostColor, _radius);
                }
            }

            var after = Mathf.Clamp01((_hammerElapsed - impactAt) / Mathf.Max(0.01f, hammerSeconds - impactAt));
            var squash = Mathf.Clamp01(after * 2.2f);
            ghost.transform.localScale = new Vector3(_ghostScale.x * (1f + 0.35f * squash), _ghostScale.y * (1f - 0.85f * squash), _ghostScale.z);
            ghost.color = new Color(_ghostColor.r, _ghostColor.g, _ghostColor.b, _ghostColor.a * (1f - squash));
            hammer.transform.position = _target + new Vector3(reach * 0.55f + after * 0.4f, reach * 0.6f + after * 0.5f, 0f);
            hammer.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-20f, 20f, after));
            hammer.color = new Color(1f, 1f, 1f, 1f - after);
            if (after >= 1f)
            {
                _hammerElapsed = -1f;
                SetVisible(hammer, false);
                SetVisible(ghost, false);
            }
        }

        private static void SetVisible(SpriteRenderer renderer, bool visible)
        {
            if (renderer != null)
            {
                renderer.enabled = visible;
            }
        }
    }
}
