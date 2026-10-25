namespace DesiMerge.Audio
{
    /// <summary>No-op audio used until real clips and an AudioSource-backed implementation exist (planned for a later phase).</summary>
    public sealed class SilentAudioService : IAudioService
    {
        public MusicTrack CurrentTrack { get; private set; }

        public void PlaySfx(SfxCue cue)
        {
        }

        public void PlayMusic(MusicTrack track)
        {
            CurrentTrack = track;
        }

        public void StopMusic()
        {
            CurrentTrack = MusicTrack.None;
        }
    }
}
