using System;
using System.Collections.Generic;
using DesiMerge.Analytics;
using DesiMerge.Audio;
using DesiMerge.Configuration;
using DesiMerge.Core;
using DesiMerge.Monetization;
using UnityEngine;

namespace DesiMerge.Gameplay
{
    /// <summary>
    /// Orchestrates one gameplay scene: turns drop requests into spawns, board events into score and feedback,
    /// the danger signal into a single game over, and handles pause, resume, restart, leaving to the menu,
    /// power-ups (effects live in <see cref="PowerUpController"/>) and the rewarded-ad revive.
    /// Plain C# so its lifetime and subscriptions are explicit. Created and disposed by <see cref="GameplayEntryPoint"/>.
    /// </summary>
    public sealed class GameplayLoop : IDisposable
    {
        private const float MergePitchStep = 0.06f;
        private const string HammerHint = "Tap an item to smash it";

        private readonly GameplayConfig _config;
        private readonly MergeProgression _progression;
        private readonly MergeBoard _board;
        private readonly DropController _drop;
        private readonly IGameplayView _view;
        private readonly GameServices _services;
        private readonly Func<float> _clock;
        private readonly Action<float> _setTimeScale;
        private readonly List<PowerUpButtonState> _powerUpStates = new List<PowerUpButtonState>();
        private float _nextDropAllowedAt;
        private bool _hasDroppedThisSession;
        private bool _leaving;
        private bool _disposed;
        private bool _revivePending;
        private readonly List<string> _sessionDiscoveries = new List<string>();
        private long _bestBeforeSession;
        private int _sessionAchievements;
        private int _sessionMissions;
        private string _sessionRank;
        /// <summary>Changes on every new session, so a late ad callback can't revive a different game.</summary>
        private int _sessionGeneration;

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
            PowerUps = new PowerUpController(config, board, services.PowerUps,
                new System.Random(config.SpawnSeed != 0 ? config.SpawnSeed : Environment.TickCount));

            Session.ScoreChanged += _view.SetScore;
            _board.ItemMerged += OnItemMerged;
            _board.FinalLevelCleared += OnFinalLevelCleared;
            _board.DangerTimeExceeded += OnDangerTimeExceeded;
            _drop.DropRequested += OnDropRequested;
            _drop.TargetRequested += OnTargetRequested;
            _drop.PointerFilter = _view.BlocksPointer;
            _services.Progress.ItemDiscovered += OnItemDiscovered;
            _services.Achievements.Unlocked += OnAchievementUnlocked;
            _services.DailyChallenges.Completed += OnDailyChallengeCompleted;
            _services.PowerUps.Changed += RefreshPowerUps;
            _services.Missions.Completed += OnMissionCompleted;
            _services.Milestones.Reached += OnMilestoneReached;
            _view.RestartRequested += OnRestartRequested;
            _view.PauseRequested += OnPauseRequested;
            _view.ResumeRequested += OnResumeRequested;
            _view.MainMenuRequested += OnMainMenuRequested;
            _view.PowerUpRequested += OnPowerUpRequested;
            _view.PowerUpCancelRequested += OnPowerUpCancelRequested;
            _view.ReviveRequested += OnReviveRequested;
        }

        public GameSession Session { get; }
        public SpawnQueue Queue { get; }
        public PowerUpController PowerUps { get; }
        public bool IsPaused => _services.GameState.Current == GameState.Paused;
        public bool IsLeaving => _leaving;
        public bool IsRevivePending => _revivePending;

        /// <summary>Power-ups only work during active play: not paused, over, leaving or waiting for an ad.</summary>
        public bool CanUsePowerUps => !_leaving && !_revivePending && !Session.IsOver && _services.GameState.Current == GameState.Playing;

        /// <summary>Starts a session (first start and every restart).</summary>
        public void Begin()
        {
            _sessionGeneration++;
            _bestBeforeSession = _services.Progress.HighScore;
            _sessionDiscoveries.Clear();
            _sessionAchievements = 0;
            _sessionMissions = 0;
            _sessionRank = null;
            EnterPlayingState();
            _setTimeScale(1f);
            _nextDropAllowedAt = _clock();
            _hasDroppedThisSession = false;
            _view.HidePause();
            _view.HideGameOver();
            _view.SetReviveOffer(ReviveOfferState.Hidden);
            _view.SetScore(Session.Score);
            _view.SetBestScore(_services.Progress.HighScore);
            _view.SetNextItem(_progression.Get(Queue.Next));
            _view.SetInstructionsVisible(true);
            _drop.InputEnabled = true;
            _drop.ShowPreview(_progression.Get(Queue.Current));
            // Free power-up uses reset with every new game (saved charges don't).
            _services.PowerUps.BeginGame();
            RefreshPowerUps();
            _services.Audio.PlayMusic(MusicTrack.Gameplay);
            _services.Analytics.TrackEvent(AnalyticsEvents.GameStarted);
        }

        /// <summary>Drops the current item at a board-local x. Returns false if dropping isn't allowed right now.</summary>
        public bool TryDrop(float localX)
        {
            var now = _clock();
            if (_leaving || PowerUps.IsTargeting || !DropRules.CanDrop(Session.IsOver, _services.GameState.Current, now, _nextDropAllowedAt))
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

            ExitTargeting(restoreInput: false);
            _setTimeScale(0f);
            _drop.InputEnabled = false;
            _services.Audio.SetMusicPaused(true);
            _services.Audio.PlaySfx(SfxCue.Pause);
            _view.ShowPause();
            RefreshPowerUps();
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
            _services.Audio.PlaySfx(SfxCue.Resume);
            _view.HidePause();
            RefreshPowerUps();
            return true;
        }

        /// <summary>
        /// Uses a power-up (Hammer starts target selection; pressing it again cancels). Returns what happened;
        /// failures change nothing and cost nothing.
        /// </summary>
        public PowerUpResult UsePowerUp(string id)
        {
            var result = PowerUps.Activate(id, CanUsePowerUps);
            switch (result)
            {
                case PowerUpResult.TargetingStarted:
                    _drop.InputEnabled = false;
                    _drop.TargetingEnabled = true;
                    _drop.HidePreview();
                    _view.SetTargeting(true, HammerHint);
                    break;
                case PowerUpResult.TargetingCancelled:
                    ExitTargeting(restoreInput: true);
                    break;
                case PowerUpResult.Used:
                    OnPowerUpUsed(id);
                    break;
                case PowerUpResult.NoCharges:
                    _view.ShowToast("No uses left this game");
                    break;
                case PowerUpResult.NothingToTarget:
                    _view.ShowToast("Nothing to use it on yet");
                    break;
                case PowerUpResult.NoRoom:
                    _view.ShowToast("Not enough room to shuffle");
                    break;
            }

            RefreshPowerUps();
            return result;
        }

        /// <summary>Hammer: the player tapped a board-local point while selecting.</summary>
        public PowerUpResult SelectPowerUpTarget(Vector2 boardLocalPoint)
        {
            var id = PowerUps.TargetingId;
            var result = PowerUps.SelectTarget(boardLocalPoint, CanUsePowerUps);
            if (result == PowerUpResult.Used)
            {
                ExitTargeting(restoreInput: true);
                OnPowerUpUsed(id);
                RefreshPowerUps();
            }

            return result;
        }

        /// <summary>Leaves Hammer selection without using a charge.</summary>
        public bool CancelPowerUp()
        {
            if (!PowerUps.IsTargeting)
            {
                return false;
            }

            ExitTargeting(restoreInput: CanUsePowerUps);
            RefreshPowerUps();
            return true;
        }

        /// <summary>
        /// Android back button: cancels Hammer selection, otherwise toggles pause. Does nothing on the game-over panel
        /// (the player chooses Continue, Restart or Main Menu) or while leaving. Returns true if it did something.
        /// </summary>
        public bool HandleBack()
        {
            if (_leaving || Session.IsOver)
            {
                return false;
            }

            if (PowerUps.IsTargeting)
            {
                return CancelPowerUp();
            }

            return IsPaused ? Resume() : Pause();
        }

        /// <summary>The app went to the background (home button, call, notification): pause so nothing happens unseen.</summary>
        public bool HandleAppInterrupted() => !IsPaused && Pause();

        /// <summary>Ends the session once. Returns false if it was already over.</summary>
        public bool EndGame()
        {
            if (!Session.TryEnd())
            {
                return false;
            }

            ExitTargeting(restoreInput: false);
            _board.SetFrozen(true);
            _drop.InputEnabled = false;
            _drop.HidePreview();

            var progress = _services.Progress;
            // A revived game was already counted at its first game over; only its best score can still change.
            var isNewBest = Session.RevivesUsed > 0 ? progress.SubmitRevivedFinalScore(Session.Score) : progress.SubmitFinalScore(Session.Score);
            // Session completion is a save point for everything it touched: stats, achievements, daily challenge.
            _services.FlushAll();

            _services.Analytics.TrackEvent(AnalyticsEvents.GameOver, new Dictionary<string, object>
            {
                { "score", Session.Score },
                { "merges", Session.MergeCount },
                { "revived", Session.RevivesUsed > 0 },
            });
            _services.Audio.StopMusic();
            _services.Audio.PlaySfx(isNewBest ? SfxCue.NewBest : SfxCue.GameOver);
            _services.GameState.TryTransitionTo(GameState.GameOver);
            _view.ShowGameOver(Session.Score, progress.HighScore, isNewBest);
            _view.ShowSessionSummary(new SessionSummary(Session.Score, _bestBeforeSession, Session.MergeCount, _sessionDiscoveries.ToArray(),
                _sessionAchievements, _sessionMissions, _services.Missions.ClaimableCount, _sessionRank));
            _view.SetReviveOffer(CurrentReviveOffer());
            RefreshPowerUps();
            return true;
        }

        /// <summary>
        /// Game over → asks for a rewarded ad; the game continues only after the provider confirms the reward.
        /// Returns false if no request was started (not game over, revive used, already waiting, or no ad ready).
        /// </summary>
        public bool TryRevive()
        {
            if (_leaving || _revivePending || !Session.IsOver || !ReviveAllowed())
            {
                return false;
            }

            var generation = _sessionGeneration;
            _revivePending = true;
            _view.SetReviveOffer(ReviveOfferState.Loading);
            if (!_services.Ads.TryShow(RewardedPlacement.ContinueAfterGameOver, result => OnReviveAdFinished(generation, result)))
            {
                _revivePending = false;
                _view.SetReviveOffer(ReviveOfferState.NotReady);
                return false;
            }

            return true;
        }

        /// <summary>Clears the board and starts a fresh session. Subscriptions are untouched, so nothing is duplicated.</summary>
        public bool Restart()
        {
            if (_leaving)
            {
                return false;
            }

            ExitTargeting(restoreInput: false);
            AbandonRevive();
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
            ExitTargeting(restoreInput: false);
            AbandonRevive();
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

            RefreshPowerUps();
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
            _drop.TargetRequested -= OnTargetRequested;
            _drop.TargetingEnabled = false;
            _drop.PointerFilter = null;
            _view.RestartRequested -= OnRestartRequested;
            _view.PauseRequested -= OnPauseRequested;
            _view.ResumeRequested -= OnResumeRequested;
            _view.MainMenuRequested -= OnMainMenuRequested;
            _view.PowerUpRequested -= OnPowerUpRequested;
            _view.PowerUpCancelRequested -= OnPowerUpCancelRequested;
            _view.ReviveRequested -= OnReviveRequested;
            _services.Progress.ItemDiscovered -= OnItemDiscovered;
            _services.Achievements.Unlocked -= OnAchievementUnlocked;
            _services.DailyChallenges.Completed -= OnDailyChallengeCompleted;
            _services.PowerUps.Changed -= RefreshPowerUps;
            _services.Missions.Completed -= OnMissionCompleted;
            _services.Milestones.Reached -= OnMilestoneReached;
            AbandonRevive();
        }

        private void OnDropRequested(float localX) => TryDrop(localX);

        private void OnTargetRequested(Vector2 boardLocalPoint) => SelectPowerUpTarget(boardLocalPoint);

        private void OnRestartRequested() => Restart();

        private void OnPauseRequested() => Pause();

        private void OnResumeRequested() => Resume();

        private void OnMainMenuRequested() => ReturnToMainMenu();

        private void OnPowerUpRequested(string id) => UsePowerUp(id);

        private void OnPowerUpCancelRequested() => CancelPowerUp();

        private void OnReviveRequested() => TryRevive();

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

        private void OnItemDiscovered(MergeItemDefinition item)
        {
            _sessionDiscoveries.Add(item.DisplayName);
            _services.Audio.PlaySfx(SfxCue.Unlock);
            _view.ShowDiscovery(item);
        }

        private void OnAchievementUnlocked(DesiMerge.Progression.AchievementDefinition achievement)
        {
            _sessionAchievements++;
            _services.Audio.PlaySfx(SfxCue.Unlock);
            _view.ShowToast($"Award unlocked: {achievement.title}");
        }

        private void OnMissionCompleted(DesiMerge.Progression.MissionDefinition mission)
        {
            _sessionMissions++;
            _services.Audio.PlaySfx(SfxCue.MissionComplete);
            _view.ShowToast($"Mission complete: {mission.title}");
        }

        private void OnMilestoneReached(DesiMerge.Progression.MilestoneDefinition milestone)
        {
            _sessionRank = milestone.title;
            _services.Audio.PlaySfx(SfxCue.Unlock);
            _view.ShowToast($"New rank: {milestone.title}!");
        }

        private void OnDailyChallengeCompleted(DesiMerge.Progression.DailyChallengeDefinition challenge) => _view.ShowToast("Daily challenge complete!");

        private void OnPowerUpUsed(string id)
        {
            var definition = _services.PowerUps.Find(id);
            _services.Audio.PlaySfx(SfxCue.PowerUp);
            _services.Analytics.TrackEvent(AnalyticsEvents.PowerUpUsed, new Dictionary<string, object> { { "id", id } });
            if (definition != null)
            {
                _view.ShowToast($"{definition.title}!");
            }
        }

        private void ExitTargeting(bool restoreInput)
        {
            if (!PowerUps.CancelTargeting() && !_drop.TargetingEnabled)
            {
                return;
            }

            _drop.TargetingEnabled = false;
            _view.SetTargeting(false, string.Empty);
            if (restoreInput)
            {
                _drop.InputEnabled = true;
                _drop.ShowPreview(_progression.Get(Queue.Current));
            }
        }

        private void RefreshPowerUps()
        {
            if (_disposed)
            {
                return;
            }

            _powerUpStates.Clear();
            var service = _services.PowerUps;
            var canUse = CanUsePowerUps;
            foreach (var definition in service.Definitions)
            {
                var available = service.Available(definition.id);
                var selected = PowerUps.TargetingId == definition.id;
                var interactable = selected || (canUse && !PowerUps.IsTargeting && available > 0);
                _powerUpStates.Add(new PowerUpButtonState(definition.id, definition.title, available, interactable, selected));
            }

            _view.SetPowerUps(_powerUpStates);
        }

        private bool ReviveAllowed() => _services.Ads.IsSupported && Session.RevivesUsed < _config.MaxRevivesPerGame;

        private ReviveOfferState CurrentReviveOffer()
        {
            if (!ReviveAllowed())
            {
                return ReviveOfferState.Hidden;
            }

            return _services.Ads.IsReady(RewardedPlacement.ContinueAfterGameOver) ? ReviveOfferState.Available : ReviveOfferState.NotReady;
        }

        private void OnReviveAdFinished(int generation, RewardedAdResult result)
        {
            // Only the request for this very game over may revive it. Anything else grants nothing.
            if (_disposed || _leaving || generation != _sessionGeneration || !_revivePending || !Session.IsOver)
            {
                return;
            }

            _revivePending = false;
            switch (result)
            {
                case RewardedAdResult.Completed:
                    ApplyRevive();
                    break;
                case RewardedAdResult.Skipped:
                    _view.SetReviveOffer(ReviveOfferState.Cancelled);
                    break;
                case RewardedAdResult.NetworkUnavailable:
                    _view.SetReviveOffer(ReviveOfferState.Offline);
                    break;
                case RewardedAdResult.NotAvailable:
                    _view.SetReviveOffer(ReviveOfferState.NotReady);
                    break;
                default:
                    _view.SetReviveOffer(ReviveOfferState.Failed);
                    break;
            }
        }

        /// <summary>
        /// Safe revive state: items near the top are removed (see <see cref="MergeBoard.ClearForRevive"/>), the rest get a
        /// fresh grace period, and play resumes with the same score and merges. The game stays counted once.
        /// </summary>
        private void ApplyRevive()
        {
            if (!Session.TryRevive())
            {
                return;
            }

            var now = _clock();
            _board.ClearForRevive(_config.ReviveClearance, now);
            _board.SetFrozen(false);
            EnterPlayingState();
            _setTimeScale(1f);
            _nextDropAllowedAt = now;
            _view.SetReviveOffer(ReviveOfferState.Hidden);
            _view.HideGameOver();
            _drop.InputEnabled = true;
            _drop.ShowPreview(_progression.Get(Queue.Current));
            _services.Audio.PlayMusic(MusicTrack.Gameplay);
            _services.Analytics.TrackEvent(AnalyticsEvents.Revived);
            _view.ShowToast("Revived! Keep going");
            RefreshPowerUps();
        }

        /// <summary>Stops waiting for a revive ad (restart, menu, scene unload). Its result is ignored.</summary>
        private void AbandonRevive()
        {
            if (!_revivePending)
            {
                return;
            }

            _revivePending = false;
            _sessionGeneration++;
            _services.Ads.Abandon();
        }

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
