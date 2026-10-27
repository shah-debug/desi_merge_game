using System;
using DesiMerge.Configuration;
using UnityEngine;

namespace DesiMerge.Gameplay
{
    /// <summary>
    /// What the gameplay loop needs from the HUD. The view only presents and raises requests; the loop decides
    /// whether a request is valid (e.g. pausing after game over is ignored).
    /// </summary>
    public interface IGameplayView
    {
        event Action RestartRequested;
        event Action PauseRequested;
        event Action ResumeRequested;
        event Action MainMenuRequested;

        void SetScore(long score);

        void SetBestScore(long bestScore);

        void SetNextItem(MergeItemDefinition definition);

        /// <summary>Points awarded by a merge at a world position (popup, sparkles). Presentation only.</summary>
        void ShowMergeFeedback(Vector2 worldPosition, int points, MergeItemDefinition createdItem);

        void SetInstructionsVisible(bool visible);

        /// <summary>Short, non-blocking message (discoveries, achievements, daily challenge). Queued if one is showing.</summary>
        void ShowToast(string message);

        void ShowPause();

        void HidePause();

        void ShowGameOver(long finalScore, long bestScore, bool isNewBest);

        void HideGameOver();

        void ShowConfigurationError(string message);

        /// <summary>True if a press at this screen position is on HUD controls and must not aim or drop.</summary>
        bool BlocksPointer(Vector2 screenPosition);
    }
}
