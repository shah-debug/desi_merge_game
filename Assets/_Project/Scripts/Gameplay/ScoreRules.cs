using DesiMerge.Configuration;

namespace DesiMerge.Gameplay
{
    /// <summary>Score formulas. A merge awards the score value of the item it creates.</summary>
    public static class ScoreRules
    {
        public static int ForUpgrade(MergeProgression progression, int resultLevel)
        {
            return progression.Get(resultLevel).ScoreValue;
        }

        public static int ForFinalClear(GameplayConfig config)
        {
            return config.FinalMergeBonus;
        }
    }
}
