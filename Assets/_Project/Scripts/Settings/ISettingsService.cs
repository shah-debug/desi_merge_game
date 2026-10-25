using System;

namespace DesiMerge.Settings
{
    /// <summary>Player-facing preferences. Volumes are normalized to [0, 1].</summary>
    public interface ISettingsService
    {
        float MusicVolume { get; set; }
        float SfxVolume { get; set; }
        bool HapticsEnabled { get; set; }

        event Action Changed;

        void Flush();
    }
}
