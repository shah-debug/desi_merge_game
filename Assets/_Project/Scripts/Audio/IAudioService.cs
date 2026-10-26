namespace DesiMerge.Audio
{
    /// <summary>
    /// Plays sound effects and music. Implementations are responsible for applying the
    /// player's volume and mute settings; callers never read settings themselves.
    /// Missing clips must be tolerated (no exceptions, no per-call warnings).
    /// </summary>
    public interface IAudioService
    {
        /// <param name="pitch">Playback pitch multiplier, e.g. to rise with merge level.</param>
        void PlaySfx(SfxCue cue, float pitch = 1f);

        /// <summary>Starts a track. Calling it again with the track already playing does not restart it.</summary>
        void PlayMusic(MusicTrack track);

        void StopMusic();

        /// <summary>Pauses/resumes music in place (used by the gameplay pause screen).</summary>
        void SetMusicPaused(bool paused);
    }
}
