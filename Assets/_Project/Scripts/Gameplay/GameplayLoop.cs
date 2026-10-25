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
    /// Orchestrates one gameplay scene: turns drop requests into spawns, board events into score,
    /// and the danger signal into a single game over; resets everything on restart.
    /// Plain C# so its lifetime and subscriptions are explicit. Created and disposed by <see cref="GameplayEntryPoint"/>.
    /// </summary>
    public sealed class GameplayLoop : IDisposable
    {
        private readonly GameplayConfig _config;
        private readonly MergeProgression _progression;
        private readonly MergeBoard _board;
        private readonly DropController _drop;
        private readonly IGameplayView _view;
        private readonly GameServices _services;
        private readonly Func<float> _clock;
        private float _nextDropAllowedAt;
        private bool _disposed;

        public GameplayLoop(GameplayConfig config, MergeBoard board, DropController drop, IGameplayView view, GameServices services, Func<float> clock)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _drop = drop ?? throw new ArgumentNullException(nameof(drop));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _progression = config.Progression;

            Session = new GameSession();
            Queue = new SpawnQueue(_progression.SpawnableLevelCount, config.SpawnSeed);

            Session.ScoreChanged += _view.SetScore;
            _board.ItemMerged += OnItemMerged;
            _board.FinalLevelCleared += OnFinalLevelCleared;
            _board.DangerTimeExceeded += OnDangerTimeExceeded;
            _drop.DropRequested += OnDropRequested;
            _view.RestartRequested += Restart;
        }

        public GameSession Session { get; }
        public SpawnQueue Queue { get; }

        /// <summary>Starts the first session. Call once after construction.</summary>
        public void Begin()
        {
            EnterPlayingState();
            _nextDropAllowedAt = _clock();
            _view.HideGameOver();
            _view.SetScore(Session.Score);
            _view.SetBestScore(_services.Progress.HighScore);
            _view.SetNextItem(_progression.Get(Queue.Next));
            _drop.InputEnabled = true;
            _drop.ShowPreview(_progression.Get(Queue.Current));
            _services.Analytics.TrackEvent(AnalyticsEvents.GameStarted);
        }

        /// <summary>Drops the current item at a board-local x. Returns false if dropping isn't allowed right now.</summary>
        public bool TryDrop(float localX)
        {
            var now = _clock();
            if (!DropRules.CanDrop(Session.IsOver, _services.GameState.Current, now, _nextDropAllowedAt))
            {
                return false;
            }

            var level = Queue.Take();
            var x = DropRules.ClampX(localX, _config.HalfWidth, _progression.Get(level).Radius);
            _board.SpawnLocal(level, new Vector2(x, _config.DropHeight));
            _nextDropAllowedAt = now + _config.DropCooldownSeconds;

            _drop.ShowPreviewAfterCooldown(_progression.Get(Queue.Current));
            _view.SetNextItem(_progression.Get(Queue.Next));
            _services.Audio.PlaySfx(SfxCue.Drop);
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
            progress.Flush();

            _services.Analytics.TrackEvent(AnalyticsEvents.GameOver, new Dictionary<string, object>
            {
                { "score", Session.Score },
                { "merges", Session.MergeCount },
            });
            _services.Audio.PlaySfx(SfxCue.GameOver);
            _services.GameState.TryTransitionTo(GameState.GameOver);
            _view.ShowGameOver(Session.Score, progress.HighScore, isNewBest);
            return true;
        }

        /// <summary>Clears the board and starts a fresh session. Subscriptions are untouched, so nothing is duplicated.</summary>
        public void Restart()
        {
            _board.Clear();
            Session.Restart();
            Queue.Reset(_config.SpawnSeed);
            Begin();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Session.ScoreChanged -= _view.SetScore;
            _board.ItemMerged -= OnItemMerged;
            _board.FinalLevelCleared -= OnFinalLevelCleared;
            _board.DangerTimeExceeded -= OnDangerTimeExceeded;
            _drop.DropRequested -= OnDropRequested;
            _view.RestartRequested -= Restart;
        }

        private void OnDropRequested(float localX) => TryDrop(localX);

        private void OnItemMerged(int resultLevel, Vector2 position)
        {
            Session.AddMergeScore(ScoreRules.ForUpgrade(_progression, resultLevel));
            _services.Audio.PlaySfx(SfxCue.Merge);
        }

        private void OnFinalLevelCleared(Vector2 position)
        {
            Session.AddMergeScore(ScoreRules.ForFinalClear(_config));
            _services.Audio.PlaySfx(SfxCue.Merge);
        }

        private void OnDangerTimeExceeded() => EndGame();

        private void EnterPlayingState()
        {
            var state = _services.GameState;
            if (state.Current == GameState.Playing || state.TryTransitionTo(GameState.Playing))
            {
                return;
            }

            // From Boot there is no menu yet (Phase 3), so pass through MainMenu.
            state.TryTransitionTo(GameState.MainMenu);
            state.TryTransitionTo(GameState.Playing);
        }
    }
}
