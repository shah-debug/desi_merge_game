using System;

namespace DesiMerge.Settings
{
    /// <summary>Player-facing preferences. Volumes are normalized to [0, 1]; mute is separate so the level is kept.</summary>
    public interface ISettingsService
    {
        float MusicVolume { get; set; }
        float SfxVolume { get; set; }
        bool MusicMuted { get; set; }
        bool SfxMuted { get; set; }

        /// <summary>Stored for a later phase; no haptic feedback is implemented yet, so no UI exposes it.</summary>
        bool HapticsEnabled { get; set; }

        /// <summary>The first-game tutorial was finished or skipped.</summary>
        bool TutorialCompleted { get; set; }

        event Action Changed;

        void Flush();
    }
}
