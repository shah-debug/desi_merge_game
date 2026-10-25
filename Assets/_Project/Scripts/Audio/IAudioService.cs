namespace DesiMerge.Audio
{
    /// <summary>
    /// Plays sound effects and music. Implementations are responsible for applying the
    /// player's volume settings; callers never read settings themselves.
    /// </summary>
    public interface IAudioService
    {
        void PlaySfx(SfxCue cue);

        void PlayMusic(MusicTrack track);

        void StopMusic();
    }
}
