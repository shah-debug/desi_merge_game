namespace DesiMerge.Audio
{
    /// <summary>Logical sound identifiers. Gameplay code asks for a cue; the audio implementation maps it to clips.</summary>
    public enum SfxCue
    {
        ButtonTap = 0,
        Drop = 1,
        Merge = 2,
        GameOver = 3,
        PowerUp = 4,
        RewardClaim = 5,
        MissionComplete = 6,
        Unlock = 7,
        Pause = 8,
        Resume = 9,
        NewBest = 10,
    }

    public enum MusicTrack
    {
        None = 0,
        Menu = 1,
        Gameplay = 2,
    }
}
