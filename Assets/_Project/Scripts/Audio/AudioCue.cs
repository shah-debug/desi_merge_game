namespace DesiMerge.Audio
{
    /// <summary>Logical sound identifiers. Gameplay code asks for a cue; the audio implementation maps it to clips.</summary>
    public enum SfxCue
    {
        ButtonTap = 0,
        Drop = 1,
        Merge = 2,
        GameOver = 3,
    }

    public enum MusicTrack
    {
        None = 0,
        Menu = 1,
        Gameplay = 2,
    }
}
