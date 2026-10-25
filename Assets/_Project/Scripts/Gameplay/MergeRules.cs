using DesiMerge.Configuration;

namespace DesiMerge.Gameplay
{
    public enum MergeKind
    {
        None = 0,
        /// <summary>Both items are replaced by one item of <see cref="MergeDecision.ResultLevel"/>.</summary>
        Upgrade = 1,
        /// <summary>Two final-level items are removed (see <see cref="FinalLevelMergeRule.ClearBoth"/>).</summary>
        ClearFinal = 2,
    }

    public readonly struct MergeDecision
    {
        public static readonly MergeDecision None = new MergeDecision(MergeKind.None, -1);

        public MergeDecision(MergeKind kind, int resultLevel)
        {
            Kind = kind;
            ResultLevel = resultLevel;
        }

        public MergeKind Kind { get; }
        public int ResultLevel { get; }
    }

    /// <summary>The single place that decides whether two levels merge and into what.</summary>
    public static class MergeRules
    {
        public static MergeDecision Decide(MergeProgression progression, int levelA, int levelB, FinalLevelMergeRule finalRule)
        {
            if (levelA != levelB || !progression.IsValidLevel(levelA))
            {
                return MergeDecision.None;
            }

            if (progression.TryGetNextLevel(levelA, out var next))
            {
                return new MergeDecision(MergeKind.Upgrade, next);
            }

            return finalRule == FinalLevelMergeRule.ClearBoth
                ? new MergeDecision(MergeKind.ClearFinal, -1)
                : MergeDecision.None;
        }
    }
}
