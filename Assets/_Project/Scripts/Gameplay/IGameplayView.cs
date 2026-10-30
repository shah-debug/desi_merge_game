using System;
using System.Collections.Generic;
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

        /// <summary>A power-up button was pressed (also used to cancel Hammer selection by pressing it again).</summary>
        event Action<string> PowerUpRequested;

        /// <summary>The Cancel button of the Hammer selection banner was pressed.</summary>
        event Action PowerUpCancelRequested;

        /// <summary>Continue (watch a rewarded ad to revive) was pressed on the game-over panel.</summary>
        event Action ReviveRequested;

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

        /// <summary>Fills the results card after <see cref="ShowGameOver"/>.</summary>
        void ShowSessionSummary(SessionSummary summary);

        /// <summary>First discovery of an item during play.</summary>
        void ShowDiscovery(MergeItemDefinition item);

        void HideGameOver();

        void ShowConfigurationError(string message);

        /// <summary>Power-up buttons, in display order. The list is reused by the caller; copy what you keep.</summary>
        void SetPowerUps(IReadOnlyList<PowerUpButtonState> states);

        /// <summary>Shows or hides the "tap an item" banner (with its Cancel button) while Hammer waits for a target.</summary>
        void SetTargeting(bool active, string hint);

        void SetReviveOffer(ReviveOfferState state);

        /// <summary>True if a press at this screen position is on HUD controls and must not aim or drop.</summary>
        bool BlocksPointer(Vector2 screenPosition);
    }
}
