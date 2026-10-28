using System;
using System.Collections.Generic;
using DesiMerge.Audio;
using DesiMerge.Configuration;
using DesiMerge.Gameplay;
using DesiMerge.UI.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.UI
{
    /// <summary>
    /// Gameplay HUD: score, best, next item, pause, pause/game-over panels and merge feedback. Presentation only:
    /// it raises requests and displays what <see cref="GameplayLoop"/> tells it; it never reads or changes game rules.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplayHud : MonoBehaviour, IGameplayView
    {
        [Header("Top bar")]
        [SerializeField] private Text scoreText;
        [SerializeField] private ScalePunch scorePunch;
        [SerializeField] private Text bestText;
        [SerializeField] private Image nextImage;
        [SerializeField] private Text nextLabel;
        [SerializeField] private ScalePunch nextPunch;
        [SerializeField] private Button pauseButton;
        [SerializeField] private GameObject instructions;

        [Header("Pause")]
        [SerializeField] private PanelTransition pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseMenuButton;
        [SerializeField] private Button pauseRestartButton;
        [SerializeField] private GameObject pauseActions;
        [SerializeField] private GameObject restartConfirm;
        [SerializeField] private Button confirmRestartButton;
        [SerializeField] private Button cancelRestartButton;

        [Header("Game over")]
        [SerializeField] private PanelTransition gameOverPanel;
        [SerializeField] private Text gameOverScoreText;
        [SerializeField] private Text gameOverBestText;
        [SerializeField] private GameObject newBestBadge;
        [SerializeField] private ScalePunch gameOverTitlePunch;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button gameOverMenuButton;

        [Header("Revive")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Text reviveStatusText;

        [Header("Power-ups")]
        [SerializeField] private PowerUpButton[] powerUpButtons = Array.Empty<PowerUpButton>();
        [SerializeField] private GameObject targetingBanner;
        [SerializeField] private Text targetingText;
        [SerializeField] private Button targetingCancelButton;

        [Header("Feedback")]
        [SerializeField] private ScorePopupLayer popupLayer;
        [SerializeField] private MergeEffects mergeEffects;
        [SerializeField] private ToastPresenter toast;

        [Header("Input")]
        [Tooltip("Screen regions where presses belong to the HUD and must not aim or drop items.")]
        [SerializeField] private RectTransform[] pointerBlockers = Array.Empty<RectTransform>();

        [SerializeField] private Text errorText;

        private long _shownScore = -1;

        public event Action RestartRequested;
        public event Action PauseRequested;
        public event Action ResumeRequested;
        public event Action MainMenuRequested;
        public event Action<string> PowerUpRequested;
        public event Action PowerUpCancelRequested;
        public event Action ReviveRequested;

        public ReviveOfferState ReviveOffer { get; private set; }
        public bool ContinueVisible => continueButton != null && continueButton.gameObject.activeSelf;
        public bool ContinueInteractable => ContinueVisible && continueButton.interactable;
        public string ReviveStatus => reviveStatusText != null && reviveStatusText.gameObject.activeSelf ? reviveStatusText.text : string.Empty;
        public bool TargetingVisible => targetingBanner != null && targetingBanner.activeSelf;
        public PowerUpButton[] PowerUpButtons => powerUpButtons;
        public bool RestartConfirmVisible => restartConfirm != null && restartConfirm.activeSelf;
        public Sprite NextItemSprite => nextImage != null ? nextImage.sprite : null;
        internal Button PauseRestartButton => pauseRestartButton;
        internal Button ConfirmRestartButton => confirmRestartButton;
        internal Button CancelRestartButton => cancelRestartButton;

        private void Awake()
        {
            AddListener(pauseButton, OnPauseClicked);
            AddListener(resumeButton, OnResumeClicked);
            AddListener(pauseMenuButton, OnMainMenuClicked);
            AddListener(pauseRestartButton, OnPauseRestartClicked);
            AddListener(confirmRestartButton, OnConfirmRestartClicked);
            AddListener(cancelRestartButton, OnCancelRestartClicked);
            AddListener(restartButton, OnRestartClicked);
            AddListener(gameOverMenuButton, OnMainMenuClicked);
            AddListener(continueButton, OnContinueClicked);
            AddListener(targetingCancelButton, OnTargetingCancelClicked);
            foreach (var powerUp in powerUpButtons)
            {
                if (powerUp != null)
                {
                    powerUp.Clicked += OnPowerUpClicked;
                }
            }

            SetTargeting(false, string.Empty);
            SetReviveOffer(ReviveOfferState.Hidden);
            SetRestartConfirm(false);

            if (pausePanel != null)
            {
                pausePanel.SetVisibleImmediate(false);
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetVisibleImmediate(false);
            }

            if (errorText != null)
            {
                errorText.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            RemoveListener(pauseButton, OnPauseClicked);
            RemoveListener(resumeButton, OnResumeClicked);
            RemoveListener(pauseMenuButton, OnMainMenuClicked);
            RemoveListener(pauseRestartButton, OnPauseRestartClicked);
            RemoveListener(confirmRestartButton, OnConfirmRestartClicked);
            RemoveListener(cancelRestartButton, OnCancelRestartClicked);
            RemoveListener(restartButton, OnRestartClicked);
            RemoveListener(gameOverMenuButton, OnMainMenuClicked);
            RemoveListener(continueButton, OnContinueClicked);
            RemoveListener(targetingCancelButton, OnTargetingCancelClicked);
            foreach (var powerUp in powerUpButtons)
            {
                if (powerUp != null)
                {
                    powerUp.Clicked -= OnPowerUpClicked;
                }
            }
        }

        /// <summary>Gives every button in this HUD its tap sound.</summary>
        public void BindAudio(IAudioService audio)
        {
            foreach (var feedback in GetComponentsInChildren<ButtonFeedback>(true))
            {
                feedback.Bind(audio);
            }
        }

        public void SetScore(long score)
        {
            if (scoreText != null)
            {
                scoreText.text = score.ToString();
            }

            if (score > _shownScore && _shownScore >= 0 && scorePunch != null)
            {
                scorePunch.Play();
            }

            _shownScore = score;
        }

        public void SetBestScore(long bestScore)
        {
            if (bestText != null)
            {
                bestText.text = bestScore.ToString();
            }
        }

        public void SetNextItem(MergeItemDefinition definition)
        {
            if (nextImage != null)
            {
                nextImage.sprite = definition.Sprite;
                nextImage.color = definition.SpriteColor;
            }

            if (nextLabel != null)
            {
                nextLabel.text = definition.DisplayName;
            }

            if (nextPunch != null)
            {
                nextPunch.Play();
            }
        }

        public void ShowMergeFeedback(Vector2 worldPosition, int points, MergeItemDefinition createdItem)
        {
            var color = createdItem != null ? createdItem.Tint : Color.white;
            if (popupLayer != null)
            {
                popupLayer.Show(worldPosition, points, Color.Lerp(color, Color.white, 0.15f));
            }

            if (mergeEffects != null)
            {
                mergeEffects.PlayMerge(worldPosition, color, createdItem != null ? createdItem.Radius : 0.5f);
            }
        }

        public void SetInstructionsVisible(bool visible)
        {
            if (instructions != null)
            {
                instructions.SetActive(visible);
            }
        }

        public void ShowToast(string message)
        {
            if (toast != null)
            {
                toast.Show(message);
            }
        }

        public void ShowPause()
        {
            SetRestartConfirm(false);
            if (pausePanel != null)
            {
                pausePanel.Show();
            }
        }

        public void HidePause()
        {
            if (pausePanel != null)
            {
                pausePanel.Hide();
            }
        }

        public void ShowGameOver(long finalScore, long bestScore, bool isNewBest)
        {
            if (gameOverScoreText != null)
            {
                gameOverScoreText.text = finalScore.ToString();
            }

            if (gameOverBestText != null)
            {
                gameOverBestText.text = $"Best  {bestScore}";
            }

            if (newBestBadge != null)
            {
                newBestBadge.SetActive(isNewBest);
            }

            SetBestScore(bestScore);
            if (gameOverPanel != null)
            {
                gameOverPanel.Show();
            }

            if (gameOverTitlePunch != null)
            {
                gameOverTitlePunch.Play();
            }
        }

        public void HideGameOver()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.Hide();
            }
        }

        public void ShowConfigurationError(string message)
        {
            if (errorText != null)
            {
                errorText.text = message;
                errorText.gameObject.SetActive(true);
            }
        }

        public void SetPowerUps(IReadOnlyList<PowerUpButtonState> states)
        {
            for (var i = 0; i < powerUpButtons.Length; i++)
            {
                var button = powerUpButtons[i];
                if (button == null)
                {
                    continue;
                }

                if (i < states.Count)
                {
                    button.Show(states[i]);
                }
                else
                {
                    button.Hide();
                }
            }
        }

        public void SetTargeting(bool active, string hint)
        {
            if (targetingText != null)
            {
                targetingText.text = hint;
            }

            if (targetingBanner != null)
            {
                targetingBanner.SetActive(active);
            }

            // The banner sits where the first-drop hint is; never show both.
            if (active && instructions != null)
            {
                instructions.SetActive(false);
            }
        }

        public void SetReviveOffer(ReviveOfferState state)
        {
            ReviveOffer = state;
            var visible = state != ReviveOfferState.Hidden;
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(visible);
                continueButton.interactable = visible && state != ReviveOfferState.Loading;
            }

            if (reviveStatusText != null)
            {
                reviveStatusText.text = ReviveStatusFor(state);
                reviveStatusText.gameObject.SetActive(visible);
            }
        }

        /// <summary>Player-facing text for each revive state. Never claims a reward that wasn't confirmed.</summary>
        internal static string ReviveStatusFor(ReviveOfferState state)
        {
            switch (state)
            {
                case ReviveOfferState.Available: return "Watch a short ad to keep playing (once per game)";
                case ReviveOfferState.Loading: return "Loading ad...";
                case ReviveOfferState.NotReady: return "No ad available right now. Try again soon.";
                case ReviveOfferState.Cancelled: return "Ad closed early, so no revive. You can try again.";
                case ReviveOfferState.Failed: return "The ad couldn't be shown. You can try again.";
                case ReviveOfferState.Offline: return "No internet connection. Restart is always free.";
                default: return string.Empty;
            }
        }

        public bool BlocksPointer(Vector2 screenPosition)
        {
            foreach (var blocker in pointerBlockers)
            {
                if (blocker != null && blocker.gameObject.activeInHierarchy &&
                    RectTransformUtility.RectangleContainsScreenPoint(blocker, screenPosition, null))
                {
                    return true;
                }
            }

            return false;
        }

        private void OnPauseClicked() => PauseRequested?.Invoke();

        private void OnResumeClicked() => ResumeRequested?.Invoke();

        private void OnRestartClicked() => RestartRequested?.Invoke();

        private void OnMainMenuClicked() => MainMenuRequested?.Invoke();

        private void OnContinueClicked() => ReviveRequested?.Invoke();

        private void OnPauseRestartClicked() => SetRestartConfirm(true);

        private void OnCancelRestartClicked() => SetRestartConfirm(false);

        private void OnConfirmRestartClicked()
        {
            SetRestartConfirm(false);
            RestartRequested?.Invoke();
        }

        private void SetRestartConfirm(bool visible)
        {
            if (restartConfirm != null)
            {
                restartConfirm.SetActive(visible);
            }

            if (pauseActions != null)
            {
                pauseActions.SetActive(!visible);
            }
        }

        private void OnTargetingCancelClicked() => PowerUpCancelRequested?.Invoke();

        private void OnPowerUpClicked(string id) => PowerUpRequested?.Invoke(id);

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(action);
            }
        }
    }
}
