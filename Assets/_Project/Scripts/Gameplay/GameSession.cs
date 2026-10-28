using System;

namespace DesiMerge.Gameplay
{
    /// <summary>State of one play-through: score and whether it has ended. The only place score changes.</summary>
    public sealed class GameSession
    {
        public long Score { get; private set; }
        public int MergeCount { get; private set; }
        public bool IsOver { get; private set; }

        public event Action<long> ScoreChanged;

        /// <summary>Raised exactly once per session, when it ends.</summary>
        public event Action Ended;

        public event Action Restarted;

        /// <summary>Adds points for a merge. Ignored once the session is over.</summary>
        public void AddMergeScore(int points)
        {
            if (points < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(points), "Points cannot be negative.");
            }

            if (IsOver)
            {
                return;
            }

            MergeCount++;
            Score += points;
            ScoreChanged?.Invoke(Score);
        }

        /// <summary>Ends the session. Returns false (and raises nothing) if it had already ended.</summary>
        public bool TryEnd()
        {
            if (IsOver)
            {
                return false;
            }

            IsOver = true;
            Ended?.Invoke();
            return true;
        }

        /// <summary>Revives this ended session: play continues with the same score and merge count.</summary>
        public bool TryRevive()
        {
            if (!IsOver)
            {
                return false;
            }

            IsOver = false;
            RevivesUsed++;
            return true;
        }

        /// <summary>Revives used in this session (reset by <see cref="Restart"/>).</summary>
        public int RevivesUsed { get; private set; }

        public void Restart()
        {
            Score = 0;
            MergeCount = 0;
            RevivesUsed = 0;
            IsOver = false;
            Restarted?.Invoke();
            ScoreChanged?.Invoke(Score);
        }
    }
}
