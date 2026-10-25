using System;
using DesiMerge.Configuration;
using DesiMerge.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.UI
{
    /// <summary>Minimal test HUD: score, best, next item, game-over panel with restart. Presentation only.</summary>
    [DisallowMultipleComponent]
    public sealed class GameplayHud : MonoBehaviour, IGameplayView
    {
        [SerializeField] private Text scoreText;
        [SerializeField] private Text bestText;
        [SerializeField] private Image nextImage;
        [SerializeField] private Text nextLabel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Text gameOverText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Text errorText;

        public event Action RestartRequested;

        private void Awake()
        {
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            if (errorText != null)
            {
                errorText.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(OnRestartClicked);
            }
        }

        public void SetScore(long score)
        {
            if (scoreText != null)
            {
                scoreText.text = score.ToString();
            }
        }

        public void SetBestScore(long bestScore)
        {
            if (bestText != null)
            {
                bestText.text = $"Best {bestScore}";
            }
        }

        public void SetNextItem(MergeItemDefinition definition)
        {
            if (nextImage != null)
            {
                nextImage.sprite = definition.Sprite;
                nextImage.color = definition.Tint;
            }

            if (nextLabel != null)
            {
                nextLabel.text = definition.DisplayName;
            }
        }

        public void ShowGameOver(long finalScore, long bestScore, bool isNewBest)
        {
            if (gameOverText != null)
            {
                gameOverText.text = isNewBest
                    ? $"Game Over\n\nNew best!\n{finalScore}"
                    : $"Game Over\n\nScore {finalScore}\nBest {bestScore}";
            }

            SetBestScore(bestScore);
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
        }

        public void HideGameOver()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
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

        private void OnRestartClicked() => RestartRequested?.Invoke();
    }
}
