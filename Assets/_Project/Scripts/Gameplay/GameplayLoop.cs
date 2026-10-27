using System;
using System.Collections.Generic;
using DesiMerge.Analytics;
using DesiMerge.Audio;
using DesiMerge.Configuration;
using DesiMerge.Core;
using UnityEngine;

namespace DesiMerge.Gameplay
{
    /// <summary>
    /// Orchestrates one gameplay scene: turns drop requests into spawns, board events into score and feedback,
    /// the danger signal into a single game over, and handles pause, resume, restart and leaving to the menu.
    /// Plain C# so its lifetime and subscriptions are explicit. Created and disposed by <see cref="GameplayEntryPoint"/>.
    /// </summary>
    public sealed class GameplayLoop : IDisposable
    {
        private const float MergePitchStep = 0.06f;

        private readonly GameplayConfig _config;
        private readonly MergeProgression _progression;
        private readonly MergeBoard _board;
        private readonly DropController _drop;
        private readonly IGameplayView _view;
        private readonly GameServices _services;
        private readonly Func<float> _clock;
        private readonly Action<float> _setTimeScale;
        private float _nextDropAllowedAt;
        private bool _hasDroppedThisSession;
        private bool _leaving;
        private bool _disposed;

        /// <param name="setTimeScale">Applies Time.timeScale; injectable for tests. Defaults to the real one.</param>
        public GameplayLoop(GameplayConfig config, MergeBoard board, DropController drop, IGameplayView view, GameServices services,
            Func<float> clock, Action<float> setTimeScale = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _drop = drop ?? throw new ArgumentNullException(nameof(drop));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _setTimeScale = setTimeScale ?? (scale => Time.timeScale = scale);
            _progression = config.Progression;

            Session = new GameSession();
            Queue = new SpawnQueue(_progression.SpawnableLevelCount, config.SpawnSeed);

            Session.ScoreChanged += _view.SetScore;
            _board.ItemMerged += OnItemMerged;
            _board.FinalLevelCleared += OnFinalLevelCleared;
            _board.DangerTimeExceeded += OnDangerTimeExceeded;
            _drop.DropRequested += OnDropRequested;
            _drop.PointerFilter = _view.BlocksPointer;
            _services.Progress.ItemDiscovered += OnItemDiscovered;
            _services.Achievements.Unlocked += OnAchievementUnlocked;
            _services.DailyChallenges.Completed += OnDailyChallengeCompleted;
            _view.RestartRequested += OnRestartRequested;
            _view.PauseRequested += OnPauseRequested;
            _view.ResumeRequested += OnResumeRequested;
            _view.MainMenuRequested += OnMainMenuRequested;
        }

        public GameSession Session { get; }
        public SpawnQueue Queue { get; }
        public bool IsPaused => _services.GameState.Current == GameState.Paused;
        public bool IsLeaving => _leaving;

        /// <summary>Starts a session (first start and every restart).</summary>
        public void Begin()
        {
            EnterPlayingState();
            _setTimeScale(1f);
            _nextDropAllowedAt = _clock();
            _hasDroppedThisSession = false;
            _view.HidePause();
            _view.HideGameOver();
            _view.SetScore(Session.Score);
            _view.SetBestScore(_services.Progress.HighScore);
            _view.SetNextItem(_progression.Get(Queue.Next));
            _view.SetInstructionsVisible(true);
            _drop.InputEnabled = true;
            _drop.ShowPreview(_progression.Get(Queue.Current));
            _services.Audio.PlayMusic(MusicTrack.Gameplay);
            _services.Analytics.TrackEvent(AnalyticsEvents.GameStarted);
        }

        /// <summary>Drops the current item at a board-local x. Returns false if dropping isn't allowed right now.</summary>
        public bool TryDrop(float localX)
        {
            var now = _clock();
            if (_leaving || !DropRules.CanDrop(Session.IsOver, _services.GameState.Current, now, _nextDropAllowedAt))
            {
                return false;
            }

            var level = Queue.Take();
            var x = DropRules.ClampX(localX, _config.HalfWidth, _progression.Get(level).Radius);
            _board.SpawnLocal(level, new Vector2(x, _config.DropHeight));
            _services.Progress.RecordSeen(_progression.Get(level));
            _nextDropAllowedAt = now + _config.DropCooldownSeconds;

            _drop.ShowPreviewAfterCooldown(_progression.Get(Queue.Current));
            _view.SetNextItem(_progression.Get(Queue.Next));
            if (!_hasDroppedThisSession)
            {
                _hasDroppedThisSession = true;
                _view.SetInstructionsVisible(false);
            }

            _services.Audio.PlaySfx(SfxCue.Drop);
            return true;
        }

        /// <summary>Pauses play: time stops (physics, merges, game-over timer), drops are disabled, music pauses.</summary>
        public bool Pause()
        {
            if (_leaving || Session.IsOver || !_services.GameState.TryTransitionTo(GameState.Paused))
            {
                return false;
            }

            _setTimeScale(0f);
            _drop.InputEnabled = false;
            _services.Audio.SetMusicPaused(true);
            _view.ShowPause();
            return true;
        }

        public bool Resume()
        {
            if (_leaving || _services.GameState.Current != GameState.Paused || !_services.GameState.TryTransitionTo(GameState.Playing))
            {
                return false;
            }

            _setTimeScale(1f);
            _drop.InputEnabled = true;
            _services.Audio.SetMusicPaused(false);
            _view.HidePause();
            return true;
        }

        /// <summary>Ends the session once. Returns false if it was already over.</summary>
        public bool EndGame()
        {
            if (!Session.TryEnd())
            {
                return false;
            }

            _board.SetFrozen(true);
            _drop.InputEnabled = false;
            _drop.HidePreview();

            var progress = _services.Progress;
            var isNewBest = progress.SubmitFinalScore(Session.Score);
            // Session completion is a save point for everything it touched: stats, achievements, daily challenge.
            _services.FlushAll();

            _services.Analytics.TrackEvent(AnalyticsEvents.GameOver, new Dictionary<string, object>
            {
                { "score", Session.Score },
                { "merges", Session.MergeCount },
            });
            _services.Audio.StopMusic();
            _services.Audio.PlaySfx(SfxCue.GameOver);
            _services.GameState.TryTransitionTo(GameState.GameOver);
            _view.ShowGameOver(Session.Score, progress.HighScore, isNewBest);
            return true;
        }

        /// <summary>Clears the board and starts a fresh session. Subscriptions are untouched, so nothing is duplicated.</summary>
        public bool Restart()
        {
            if (_leaving)
            {
                return false;
            }

            _board.Clear();
            Session.Restart();
            Queue.Reset(_config.SpawnSeed);
            Begin();
            return true;
        }

        /// <summary>
        /// Leaves to the main menu once: restores time, stops input, saves, and asks the navigator to load the menu.
        /// A run abandoned mid-game is not submitted as a final score.
        /// </summary>
        public bool ReturnToMainMenu()
        {
            if (_leaving)
            {
                return false;
            }

            _leaving = true;
            _setTimeScale(1f);
            _drop.InputEnabled = false;
            _board.SetFrozen(true);
            _services.Audio.SetMusicPaused(false);
            _services.FlushAll();
            _services.GameState.TryTransitionTo(GameState.MainMenu);
            if (!_services.Navigator.LoadMainMenu())
            {
                Debug.LogWarning("[Gameplay] Main menu could not be loaded.");
            }

            return true;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            // Never leave the game frozen if this scene is unloaded while paused.
            _setTimeScale(1f);
            Session.ScoreChanged -= _view.SetScore;
            _board.ItemMerged -= OnItemMerged;
            _board.FinalLevelCleared -= OnFinalLevelCleared;
            _board.DangerTimeExceeded -= OnDangerTimeExceeded;
            _drop.DropRequested -= OnDropRequested;
            _drop.PointerFilter = null;
            _view.RestartRequested -= OnRestartRequested;
            _view.PauseRequested -= OnPauseRequested;
            _view.ResumeRequested -= OnResumeRequested;
            _view.MainMenuRequested -= OnMainMenuRequested;
            _services.Progress.ItemDiscovered -= OnItemDiscovered;
            _services.Achievements.Unlocked -= OnAchievementUnlocked;
            _services.DailyChallenges.Completed -= OnDailyChallengeCompleted;
        }

        private void OnDropRequested(float localX) => TryDrop(localX);

        private void OnRestartRequested() => Restart();

        private void OnPauseRequested() => Pause();

        private void OnResumeRequested() => Resume();

        private void OnMainMenuRequested() => ReturnToMainMenu();

        private void OnItemMerged(int resultLevel, Vector2 position)
        {
            if (Session.IsOver)
            {
                return;
            }

            var created = _progression.Get(resultLevel);
            var points = ScoreRules.ForUpgrade(_progression, resultLevel);
            Session.AddMergeScore(points);
            // The board raises this exactly once per merge (see MergeResolver), so it is recorded exactly once.
            _services.Progress.RecordMerge(created);
            _view.ShowMergeFeedback(position, points, created);
            _services.Audio.PlaySfx(SfxCue.Merge, 1f + resultLevel * MergePitchStep);
        }

        private void OnFinalLevelCleared(Vector2 position)
        {
            if (Session.IsOver)
            {
                return;
            }

            var points = ScoreRules.ForFinalClear(_config);
            Session.AddMergeScore(points);
            _services.Progress.RecordMerge(null);
            _view.ShowMergeFeedback(position, points, _progression.Get(_progression.FinalLevel));
            _services.Audio.PlaySfx(SfxCue.Merge, 1f + _progression.FinalLevel * MergePitchStep);
        }

        private void OnDangerTimeExceeded() => EndGame();

        private void OnItemDiscovered(MergeItemDefinition item) => _view.ShowToast($"New discovery: {item.DisplayName}!");

        private void OnAchievementUnlocked(DesiMerge.Progression.AchievementDefinition achievement) => _view.ShowToast($"Achievement: {achievement.title}");

        private void OnDailyChallengeCompleted(DesiMerge.Progression.DailyChallengeDefinition challenge) => _view.ShowToast("Daily challenge complete!");

        private void EnterPlayingState()
        {
            var state = _services.GameState;
            if (state.Current == GameState.Playing || state.TryTransitionTo(GameState.Playing))
            {
                return;
            }

            // Started without the menu (Boot) or from a state without a direct edge: pass through MainMenu.
            state.TryTransitionTo(GameState.MainMenu);
            state.TryTransitionTo(GameState.Playing);
        }
    }
}
