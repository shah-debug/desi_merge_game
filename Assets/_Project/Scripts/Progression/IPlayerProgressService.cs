using System;

namespace DesiMerge.Progression
{
    public interface IPlayerProgressService
    {
        long HighScore { get; }
        long Coins { get; }
        int GamesPlayed { get; }

        event Action Changed;

        /// <summary>Records a finished run. Returns true if the score is a new high score.</summary>
        bool SubmitFinalScore(long score);

        void AddCoins(long amount);

        /// <summary>Spends coins if the balance allows it. Returns false and changes nothing otherwise.</summary>
        bool TrySpendCoins(long amount);

        /// <summary>Writes pending changes to storage. Cheap to call when nothing changed.</summary>
        void Flush();
    }
}
