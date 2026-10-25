namespace DesiMerge.Core
{
    /// <summary>
    /// Implemented by one MonoBehaviour per scene that wires that scene's objects to the services.
    /// <see cref="Initialize"/> runs after the scene's Awake/OnEnable and before Start,
    /// so do not use services from Awake.
    /// </summary>
    public interface ISceneEntryPoint
    {
        void Initialize(GameServices services);
    }
}
