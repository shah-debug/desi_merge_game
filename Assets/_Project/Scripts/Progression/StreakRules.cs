namespace DesiMerge.Progression
{
    /// <summary>
    /// Daily streak = consecutive UTC calendar days on which the player <b>completed</b> at least one game.
    /// - First completed game ever: streak 1.
    /// - Another game on the same day: no change.
    /// - A game on the next day: +1.
    /// - A game after one or more missed days: back to 1.
    /// - Device clock earlier than the last recorded day: ignored (no change), so moving the clock back and forth
    ///   can't add days. Moving it forward is not detected (local clocks can't be trusted against manipulation).
    /// For display, a streak whose last day is more than one day ago is shown as 0 (broken), but the stored value
    /// only resets on the next completed game.
    /// </summary>
    public static class StreakRules
    {
        /// <returns>True if the stored streak changed.</returns>
        public static bool RecordCompletedGame(ref int current, ref int best, ref int lastDay, int today)
        {
            if (lastDay >= 0 && today <= lastDay)
            {
                return false;
            }

            current = lastDay >= 0 && today == lastDay + 1 ? current + 1 : 1;
            lastDay = today;
            if (current > best)
            {
                best = current;
            }

            return true;
        }

        public static int Displayed(int current, int lastDay, int today)
        {
            if (lastDay < 0)
            {
                return 0;
            }

            return today - lastDay <= 1 ? current : 0;
        }
    }
}
