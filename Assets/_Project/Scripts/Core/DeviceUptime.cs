using System;
using UnityEngine;

namespace DesiMerge.Core
{
    /// <summary>
    /// Time since the phone booted, including deep sleep (Android <c>SystemClock.elapsedRealtime</c>). Unlike the date it
    /// can't be changed in Settings, so comparing the two shows whether the device clock was moved by hand. Returns -1
    /// where it isn't available (Editor, desktop, JNI failure); callers then simply trust the date.
    /// </summary>
    public static class DeviceUptime
    {
        public static long ElapsedRealtimeMilliseconds()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var clock = new AndroidJavaClass("android.os.SystemClock"))
                {
                    return clock.CallStatic<long>("elapsedRealtime");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Clock] elapsedRealtime unavailable: {e.Message}");
                return -1;
            }
#else
            return -1;
#endif
        }
    }
}
