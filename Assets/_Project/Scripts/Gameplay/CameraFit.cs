using UnityEngine;

namespace DesiMerge.Gameplay
{
    /// <summary>
    /// Sizes the orthographic camera so at least <see cref="minWorldWidth"/> x <see cref="minWorldHeight"/> world units
    /// are visible. On tall, narrow phones (19.5:9, 20:9) the view grows vertically so the whole board stays on screen.
    /// Applied once at startup (the game is portrait-only); call <see cref="Apply()"/> after a resolution change.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public sealed class CameraFit : MonoBehaviour
    {
        [Tooltip("Board width including walls plus a small margin.")]
        [SerializeField] private float minWorldWidth = 8f;
        [SerializeField] private float minWorldHeight = 16f;

        public static float RequiredOrthographicSize(float aspect, float minWidth, float minHeight)
        {
            if (aspect <= 0f)
            {
                return minHeight * 0.5f;
            }

            return Mathf.Max(minHeight * 0.5f, minWidth * 0.5f / aspect);
        }

        private void Awake() => Apply();

        public void Apply()
        {
            var targetCamera = GetComponent<Camera>();
            targetCamera.orthographicSize = RequiredOrthographicSize(targetCamera.aspect, minWorldWidth, minWorldHeight);
        }
    }
}
