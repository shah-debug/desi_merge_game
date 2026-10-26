namespace DesiMerge.Core
{
    /// <summary>
    /// Top-level scene changes. While a load is in progress further requests are ignored, so double taps on
    /// Play or Main Menu cannot start duplicate loads.
    /// </summary>
    public interface ISceneNavigator
    {
        bool IsLoading { get; }

        /// <returns>False if a load is already in progress or the scene is not in Build Settings.</returns>
        bool LoadMainMenu();

        /// <returns>False if a load is already in progress or the scene is not in Build Settings.</returns>
        bool LoadGameplay();
    }
}
