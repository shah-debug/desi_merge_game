namespace DesiMerge.Audio
{
    /// <summary>No-op audio for tests and for when no <see cref="AudioLibrary"/> is configured.</summary>
    public sealed class SilentAudioService : IAudioService
    {
        public MusicTrack CurrentTrack { get; private set; }
        public bool MusicPaused { get; private set; }

        public void PlaySfx(SfxCue cue, float pitch = 1f)
        {
        }

        public void PlayMusic(MusicTrack track)
        {
            CurrentTrack = track;
            MusicPaused = false;
        }

        public void StopMusic()
        {
            CurrentTrack = MusicTrack.None;
        }

        public void SetMusicPaused(bool paused)
        {
            MusicPaused = paused;
        }
    }
}
