using UnityEngine;

namespace DesiMerge.Audio
{
    public static class AudioMix
    {
        /// <summary>Final AudioSource volume: the clip's base volume scaled by the player's channel volume, or 0 when muted.</summary>
        public static float Effective(float baseVolume, float channelVolume, bool muted)
        {
            return muted ? 0f : Mathf.Clamp01(baseVolume) * Mathf.Clamp01(channelVolume);
        }
    }
}
