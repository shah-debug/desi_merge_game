using UnityEngine;

namespace DesiMerge.Gameplay
{
    /// <summary>
    /// Visual-only glide of an item's art from where it was to its body (used by Shuffle). The physics body has
    /// already moved; only the child sprite catches up, so collisions and rules are unaffected.
    /// </summary>
    public sealed class ItemTravel : MonoBehaviour
    {
        [SerializeField] private float duration = 0.38f;

        private Vector3 _from;
        private float _elapsed;

        private void Awake() => enabled = false;

        public void Play(Vector3 worldFrom)
        {
            _from = transform.parent != null ? transform.parent.InverseTransformPoint(worldFrom) : worldFrom;
            _elapsed = 0f;
            transform.localPosition = _from;
            enabled = true;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(_elapsed / duration);
            var eased = 1f - Mathf.Pow(1f - t, 3f);
            transform.localPosition = Vector3.LerpUnclamped(_from, Vector3.zero, eased);
            if (t >= 1f)
            {
                enabled = false;
            }
        }

        private void OnDisable() => transform.localPosition = Vector3.zero;
    }
}
