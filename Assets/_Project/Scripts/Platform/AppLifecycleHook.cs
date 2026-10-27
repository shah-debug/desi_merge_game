using System;
using UnityEngine;

namespace DesiMerge.Platform
{
    /// <summary>Forwards Unity's application callbacks as <see cref="IAppLifecycle"/> events. Added by the bootstrapper.</summary>
    [DisallowMultipleComponent]
    public sealed class AppLifecycleHook : MonoBehaviour, IAppLifecycle
    {
        public event Action Paused;
        public event Action Resumed;
        public event Action Quitting;
        public event Action FocusLost;

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Paused?.Invoke();
            }
            else
            {
                Resumed?.Invoke();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                FocusLost?.Invoke();
            }
        }

        private void OnApplicationQuit()
        {
            Quitting?.Invoke();
        }
    }
}
