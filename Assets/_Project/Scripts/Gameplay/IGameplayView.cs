using System;
using DesiMerge.Configuration;

namespace DesiMerge.Gameplay
{
    /// <summary>What the gameplay loop needs from the HUD. Keeps presentation out of gameplay code.</summary>
    public interface IGameplayView
    {
        event Action RestartRequested;

        void SetScore(long score);

        void SetBestScore(long bestScore);

        void SetNextItem(MergeItemDefinition definition);

        void ShowGameOver(long finalScore, long bestScore, bool isNewBest);

        void HideGameOver();

        void ShowConfigurationError(string message);
    }
}
