namespace DesiMerge.Configuration
{
    /// <summary>What happens when two final-level items touch.</summary>
    public enum FinalLevelMergeRule
    {
        /// <summary>Both items are removed and the final-merge bonus is awarded.</summary>
        ClearBoth = 0,
        /// <summary>Final-level items never merge; they just stack.</summary>
        NoMerge = 1,
    }
}
