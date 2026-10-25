namespace DesiMerge.Gameplay
{
    /// <summary>
    /// Initial game-over rule: the game ends when any settled item's top edge stays above the danger line
    /// for <c>delay</c> seconds without interruption. Items younger than <c>grace</c> seconds
    /// (just dropped, or just created by a merge) are ignored, so normal drops and merges never end the game.
    /// </summary>
    public static class GameOverRule
    {
        public const float NotAbove = -1f;

        /// <param name="aboveSince">Per-item timer; start at <see cref="NotAbove"/>. Updated in place.</param>
        /// <returns>True when this item has exceeded the danger time.</returns>
        public static bool Evaluate(float now, float itemTop, float dangerLine, float spawnTime, float grace, float delay, ref float aboveSince)
        {
            if (now - spawnTime < grace || itemTop <= dangerLine)
            {
                aboveSince = NotAbove;
                return false;
            }

            if (aboveSince < 0f)
            {
                aboveSince = now;
            }

            return now - aboveSince >= delay;
        }
    }
}
