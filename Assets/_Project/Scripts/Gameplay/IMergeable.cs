namespace DesiMerge.Gameplay
{
    /// <summary>Minimal view of a board item needed to resolve merges. Lets merge logic be tested without physics.</summary>
    public interface IMergeable
    {
        int Level { get; }

        /// <summary>True once the item has been used by a merge (or removed). Consumed items never merge again.</summary>
        bool IsConsumed { get; }

        void MarkConsumed();
    }

    public interface IMergeHandler<in T> where T : class, IMergeable
    {
        void OnUpgrade(T first, T second, int resultLevel);

        void OnFinalCleared(T first, T second);
    }
}
