namespace DesiMerge.Core
{
    /// <summary>High-level application states. Gameplay sub-states belong to the gameplay layer, not here.</summary>
    public enum GameState
    {
        Boot = 0,
        MainMenu = 1,
        Playing = 2,
        Paused = 3,
        GameOver = 4,
    }
}
