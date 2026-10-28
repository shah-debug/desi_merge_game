using System.Collections.Generic;
using DesiMerge.Core;
using DesiMerge.Gameplay;
using DesiMerge.Monetization;
using NUnit.Framework;
using UnityEngine;

namespace DesiMerge.Tests.PlayMode
{
    /// <summary>
    /// Phase 5 power-ups and the rewarded-ad revive, on a real board with script-driven physics, the in-memory store
    /// and a deterministic fake ad provider (no ad network).
    /// </summary>
    public class PowerUpAndReviveTests
    {
        private const string Hammer = "hammer";
        private const string Shuffle = "shuffle";

        private GameplayTestRig _rig;

        [SetUp]
        public void SetUp()
        {
            _rig = new GameplayTestRig();
            _rig.CreateLoop();
        }

        [TearDown]
        public void TearDown() => _rig.Dispose();

        private GameplayLoop Loop => _rig.Loop;
        private GameServices Services => _rig.Services;

        private Vector2 LocalOf(MergeItem item) => _rig.Board.WorldToLocal(item.Body.position);

        // ---------- Hammer ----------

        [Test]
        public void Hammer_RemovesExactlyTheTappedItem_WithoutScoreOrMerge()
        {
            var keep = _rig.SpawnOnFloor(0, -2f);
            var smash = _rig.SpawnOnFloor(1, 2f);
            _rig.Simulate(5);

            Assert.AreEqual(PowerUpResult.TargetingStarted, Loop.UsePowerUp(Hammer));
            Assert.IsTrue(_rig.View.TargetingShown);
            Assert.IsFalse(Loop.TryDrop(0f), "No drops while choosing a target.");
            Assert.AreEqual(1, Services.PowerUps.Available(Hammer), "Nothing is used before a target is chosen.");

            Assert.AreEqual(PowerUpResult.Used, Loop.SelectPowerUpTarget(LocalOf(smash)));

            Assert.AreEqual(1, _rig.Board.ActiveCount);
            Assert.AreSame(keep, _rig.Board.ActiveItems[0]);
            Assert.AreEqual(0, Loop.Session.Score);
            Assert.AreEqual(0, Loop.Session.MergeCount);
            Assert.AreEqual(0, Services.Progress.TotalMerges, "Removing an item is not a merge.");
            Assert.AreEqual(0, Services.PowerUps.Available(Hammer));
            Assert.IsFalse(_rig.View.TargetingShown);
            Assert.IsTrue(_rig.Drop.InputEnabled, "Dropping works again.");
            _rig.Simulate(20);
            Assert.AreEqual(1, _rig.Board.ActiveCount, "Physics carries on normally with the item gone.");
        }

        [Test]
        public void Hammer_MissKeepsWaiting_AndCancelCostsNothing()
        {
            _rig.SpawnOnFloor(0, -2f);
            _rig.Simulate(5);
            Loop.UsePowerUp(Hammer);

            Assert.AreEqual(PowerUpResult.MissedTarget, Loop.SelectPowerUpTarget(new Vector2(2.5f, 6f)));
            Assert.IsTrue(Loop.PowerUps.IsTargeting);
            Assert.IsTrue(Loop.CancelPowerUp());

            Assert.AreEqual(1, _rig.Board.ActiveCount);
            Assert.AreEqual(1, Services.PowerUps.Available(Hammer));
            Assert.IsFalse(_rig.View.TargetingShown);
            Assert.IsTrue(_rig.Drop.InputEnabled);
        }

        [Test]
        public void PowerUps_RepeatedOrSecondActivationIsHandled()
        {
            _rig.SpawnOnFloor(0, -2f);
            _rig.SpawnOnFloor(1, 2f);
            _rig.Simulate(5);

            Assert.AreEqual(PowerUpResult.TargetingStarted, Loop.UsePowerUp(Hammer));
            Assert.AreEqual(PowerUpResult.NotAllowedNow, Loop.UsePowerUp(Shuffle), "One power-up at a time.");
            Assert.IsFalse(_rig.View.PowerUp(Shuffle).Interactable);
            Assert.AreEqual(PowerUpResult.TargetingCancelled, Loop.UsePowerUp(Hammer), "Pressing Hammer again cancels it.");
            Assert.AreEqual(1, Services.PowerUps.Available(Hammer));
            Assert.AreEqual(1, Services.PowerUps.Available(Shuffle));
        }

        [Test]
        public void PowerUps_RejectedWhilePausedAndAfterGameOver()
        {
            _rig.SpawnOnFloor(0, -2f);
            _rig.SpawnOnFloor(1, 2f);
            _rig.Simulate(5);

            Loop.Pause();
            Assert.AreEqual(PowerUpResult.NotAllowedNow, Loop.UsePowerUp(Hammer));
            Assert.AreEqual(PowerUpResult.NotAllowedNow, Loop.UsePowerUp(Shuffle));
            Assert.IsFalse(_rig.View.PowerUp(Hammer).Interactable);
            Loop.Resume();
            Assert.IsTrue(_rig.View.PowerUp(Hammer).Interactable);

            Loop.EndGame();
            Assert.AreEqual(PowerUpResult.NotAllowedNow, Loop.UsePowerUp(Hammer));
            Assert.AreEqual(PowerUpResult.NotAllowedNow, Loop.UsePowerUp(Shuffle));
            Assert.IsFalse(_rig.View.PowerUp(Shuffle).Interactable);
            Assert.AreEqual(2, _rig.Board.ActiveCount);
            Assert.AreEqual(1, Services.PowerUps.Available(Hammer));
            Assert.AreEqual(1, Services.PowerUps.Available(Shuffle));
        }

        [Test]
        public void Pause_WhileChoosingATarget_CancelsWithoutCost()
        {
            _rig.SpawnOnFloor(0, 0f);
            _rig.Simulate(5);
            Loop.UsePowerUp(Hammer);

            Loop.Pause();

            Assert.IsFalse(Loop.PowerUps.IsTargeting);
            Assert.IsFalse(_rig.Drop.TargetingEnabled);
            Assert.AreEqual(PowerUpResult.NotAllowedNow, Loop.SelectPowerUpTarget(Vector2.zero));
            Assert.AreEqual(1, _rig.Board.ActiveCount);
            Assert.AreEqual(1, Services.PowerUps.Available(Hammer));
        }

        [Test]
        public void PowerUps_EmptyBoardAndUsedUpAreRefused()
        {
            Assert.AreEqual(PowerUpResult.NothingToTarget, Loop.UsePowerUp(Hammer));
            Assert.AreEqual(PowerUpResult.NothingToTarget, Loop.UsePowerUp(Shuffle));

            var item = _rig.SpawnOnFloor(0, 0f);
            _rig.SpawnOnFloor(1, 2f);
            _rig.Simulate(5);
            Loop.UsePowerUp(Hammer);
            Loop.SelectPowerUpTarget(LocalOf(item));

            Assert.AreEqual(PowerUpResult.NoCharges, Loop.UsePowerUp(Hammer));
            Assert.AreEqual(0, _rig.View.PowerUp(Hammer).Available);
            Assert.IsFalse(_rig.View.PowerUp(Hammer).Interactable);
            Assert.AreEqual(PowerUpResult.UnknownPowerUp, Loop.UsePowerUp("undo"));
        }

        [Test]
        public void FreeUses_ResetOnRestart_SavedChargesPersist()
        {
            Assert.IsTrue(Services.PowerUps.TryGrant("test:reward", Hammer, 1));
            var first = _rig.SpawnOnFloor(0, -2f);
            var second = _rig.SpawnOnFloor(1, 2f);
            _rig.Simulate(5);

            Loop.UsePowerUp(Hammer);
            Loop.SelectPowerUpTarget(LocalOf(first));
            Loop.UsePowerUp(Hammer);
            Assert.AreEqual(PowerUpResult.Used, Loop.SelectPowerUpTarget(LocalOf(second)), "The saved charge is used after the free one.");
            Assert.AreEqual(0, Services.PowerUps.Available(Hammer));
            Loop.EndGame(); // save point

            Loop.Restart();
            Assert.AreEqual(1, Services.PowerUps.Available(Hammer), "A new game brings back the free use only.");
            Assert.AreEqual(0, Services.PowerUps.SavedCharges(Hammer));

            var reopened = _rig.BuildServices();
            Assert.AreEqual(0, reopened.PowerUps.SavedCharges(Hammer), "The used saved charge stays used after a restart.");
            reopened.Dispose();
        }

        // ---------- Shuffle ----------

        [Test]
        public void Shuffle_KeepsEveryItemInsideAndApart_AndKeepsTheirLevels()
        {
            var levels = new[] { 0, 1, 2, 3, 0, 1, 2, 3 };
            for (var i = 0; i < levels.Length; i++)
            {
                _rig.Board.SpawnLocal(levels[i], new Vector2(-2.5f + 0.7f * i, 0.8f + 1.3f * (i % 3)));
            }

            var before = LevelsOnBoard();
            Assert.AreEqual(PowerUpResult.Used, Loop.UsePowerUp(Shuffle));

            var centres = new List<Vector2>();
            var radii = new List<float>();
            foreach (var item in _rig.Board.ActiveItems)
            {
                centres.Add(LocalOf(item));
                radii.Add(item.Radius);
                Assert.AreEqual(Vector2.zero, item.Body.linearVelocity);
            }

            Assert.IsFalse(ShufflePlanner.HasOverlap(centres, radii), "No two items overlap.");
            Assert.IsTrue(ShufflePlanner.AllInside(centres, radii, _rig.Config.HalfWidth, _rig.Config.DangerLineHeight - _rig.Config.ShuffleTopMargin));
            CollectionAssert.AreEquivalent(before, LevelsOnBoard());
            Assert.AreEqual(0, Loop.Session.Score, "Shuffling itself scores nothing.");
            Assert.AreEqual(0, Services.PowerUps.Available(Shuffle));

            _rig.Simulate(100);
            foreach (var item in _rig.Board.ActiveItems)
            {
                var local = LocalOf(item);
                Assert.LessOrEqual(Mathf.Abs(local.x) + item.Radius, _rig.Config.HalfWidth + 0.05f, "Still inside the walls after settling.");
                Assert.GreaterOrEqual(local.y - item.Radius, -0.05f);
            }

            Assert.IsFalse(Loop.Session.IsOver);
        }

        [Test]
        public void Shuffle_RefusedWhenItCannotFit_NothingMovesAndNothingIsUsed()
        {
            for (var i = 0; i < 40; i++)
            {
                _rig.Board.SpawnLocal(3, new Vector2(-2.6f + 1.3f * (i % 5), 0.6f + 1.2f * (i / 5)));
            }

            var positions = new List<Vector2>();
            foreach (var item in _rig.Board.ActiveItems)
            {
                positions.Add(item.Body.position);
            }

            Assert.AreEqual(PowerUpResult.NoRoom, Loop.UsePowerUp(Shuffle));

            for (var i = 0; i < positions.Count; i++)
            {
                Assert.AreEqual(positions[i], _rig.Board.ActiveItems[i].Body.position);
            }

            Assert.AreEqual(1, Services.PowerUps.Available(Shuffle));
            Assert.IsTrue(_rig.View.Toasts.Exists(t => t.Contains("room")));
        }

        private List<int> LevelsOnBoard()
        {
            var levels = new List<int>();
            foreach (var item in _rig.Board.ActiveItems)
            {
                levels.Add(item.Level);
            }

            return levels;
        }

        // ---------- Revive ----------

        /// <summary>A merge for some score, then an item above the danger line, then game over.</summary>
        private long ReachGameOverWithScore()
        {
            _rig.SpawnOnFloor(0, -0.25f);
            _rig.SpawnOnFloor(0, 0.25f);
            _rig.Simulate(5);
            _rig.Board.SpawnLocal(2, new Vector2(0f, _rig.Config.DangerLineHeight - 0.2f));
            var score = Loop.Session.Score;
            Assert.Greater(score, 0);
            Assert.IsTrue(Loop.EndGame());
            return score;
        }

        [Test]
        public void Revive_HappensOnlyAfterTheProviderConfirmsTheReward()
        {
            var score = ReachGameOverWithScore();
            Assert.AreEqual(ReviveOfferState.Available, _rig.View.ReviveOffer);

            _rig.View.ClickContinue();

            Assert.AreEqual(ReviveOfferState.Loading, _rig.View.ReviveOffer);
            Assert.IsTrue(Loop.IsRevivePending);
            Assert.IsTrue(Loop.Session.IsOver, "Asking for an ad grants nothing.");
            Assert.IsFalse(Loop.TryRevive(), "No second request while one is in flight.");
            Assert.AreEqual(1, _rig.Ads.ShowCalls);

            _rig.Ads.Finish(RewardedAdResult.Completed);

            Assert.IsFalse(Loop.Session.IsOver);
            Assert.AreEqual(GameState.Playing, Services.GameState.Current);
            Assert.AreEqual(score, Loop.Session.Score, "Score is kept.");
            Assert.AreEqual(1, Services.Progress.GamesPlayed, "Still one game.");
            Assert.AreEqual(1, Services.Progress.TotalMerges, "Merge stats are kept and nothing new is recorded.");
            Assert.AreEqual(ReviveOfferState.Hidden, _rig.View.ReviveOffer);
            Assert.IsTrue(_rig.Drop.InputEnabled);
            Assert.IsTrue(Loop.TryDrop(0f));
        }

        [TestCase(RewardedAdResult.Skipped, ReviveOfferState.Cancelled)]
        [TestCase(RewardedAdResult.Failed, ReviveOfferState.Failed)]
        [TestCase(RewardedAdResult.NoFill, ReviveOfferState.Failed)]
        [TestCase(RewardedAdResult.LoadFailed, ReviveOfferState.Failed)]
        [TestCase(RewardedAdResult.NetworkUnavailable, ReviveOfferState.Offline)]
        [TestCase(RewardedAdResult.NotAvailable, ReviveOfferState.NotReady)]
        public void Revive_CancelledOrFailedAd_GrantsNothing_AndCanBeRetried(RewardedAdResult result, ReviveOfferState expected)
        {
            ReachGameOverWithScore();
            var items = _rig.Board.ActiveCount;

            Loop.TryRevive();
            _rig.Ads.Finish(result);

            Assert.IsTrue(Loop.Session.IsOver);
            Assert.AreEqual(GameState.GameOver, Services.GameState.Current);
            Assert.AreEqual(expected, _rig.View.ReviveOffer);
            Assert.AreEqual(items, _rig.Board.ActiveCount, "The board is untouched.");
            Assert.AreEqual(0, Loop.Session.RevivesUsed);
            Assert.IsTrue(Loop.TryRevive(), "The player may try again.");
        }

        [Test]
        public void Revive_DuplicateProviderCallbacks_ReviveOnce()
        {
            ReachGameOverWithScore();
            Loop.TryRevive();

            _rig.Ads.Finish(RewardedAdResult.Completed, times: 3);

            Assert.AreEqual(1, Loop.Session.RevivesUsed);
            Assert.AreEqual(2, Services.Ads.IgnoredCallbacks);
            Assert.IsFalse(Loop.Session.IsOver);
        }

        [Test]
        public void Revive_AllowedOncePerGame_AndTheGameIsCountedOnce()
        {
            var score = ReachGameOverWithScore();
            Loop.TryRevive();
            _rig.Ads.Finish(RewardedAdResult.Completed);
            _rig.SpawnOnFloor(1, -1f);
            _rig.SpawnOnFloor(1, -0.4f);
            _rig.Simulate(5);
            Assert.Greater(Loop.Session.Score, score, "Play continues and scores normally.");

            Assert.IsTrue(Loop.EndGame());

            Assert.AreEqual(ReviveOfferState.Hidden, _rig.View.ReviveOffer, "No second revive.");
            Assert.IsFalse(Loop.TryRevive());
            Assert.AreEqual(1, Services.Progress.GamesPlayed);
            Assert.AreEqual(Loop.Session.Score, Services.Progress.HighScore, "The revived run's final score is the best.");

            Loop.Restart();
            Loop.EndGame();
            Assert.AreEqual(ReviveOfferState.Available, _rig.View.ReviveOffer, "A new game gets its own revive.");
            Assert.AreEqual(2, Services.Progress.GamesPlayed);
        }

        [Test]
        public void Revive_ClearsTheDangerZone_AndDoesNotEndAgainImmediately()
        {
            ReachGameOverWithScore();
            var limit = _rig.Config.DangerLineHeight - _rig.Config.ReviveClearance;

            Loop.TryRevive();
            _rig.Ads.Finish(RewardedAdResult.Completed);

            foreach (var item in _rig.Board.ActiveItems)
            {
                Assert.LessOrEqual(LocalOf(item).y + item.Radius, limit, "Nothing is left near the danger line.");
            }

            Assert.AreEqual(1, _rig.Board.ActiveCount, "Only the merged item on the floor remains.");
            _rig.Simulate(250); // 5 s: well past the grace period and the game-over delay
            Assert.IsFalse(Loop.Session.IsOver);
            Assert.AreEqual(1, _rig.View.GameOverShown);
        }

        [Test]
        public void Restart_WorksWhenAdsAreUnavailableOrUnsupported()
        {
            _rig.Ads.Ready = false;
            ReachGameOverWithScore();
            Assert.AreEqual(ReviveOfferState.NotReady, _rig.View.ReviveOffer);
            Assert.IsFalse(Loop.TryRevive());
            Assert.AreEqual(0, _rig.Ads.ShowCalls);

            _rig.View.ClickRestart();
            Assert.IsFalse(Loop.Session.IsOver);
            Assert.AreEqual(GameState.Playing, Services.GameState.Current);
            Assert.AreEqual(ReviveOfferState.Hidden, _rig.View.ReviveOffer);

            _rig.Ads.IsSupported = false;
            Loop.EndGame();
            Assert.AreEqual(ReviveOfferState.Hidden, _rig.View.ReviveOffer, "No provider in this build: no offer at all.");
            Assert.IsTrue(Loop.Restart());
        }

        [Test]
        public void AppInterruptedDuringAd_RevivesOnlyWhenTheRewardArrives()
        {
            ReachGameOverWithScore();
            Loop.TryRevive();

            // The ad covers the app: Unity pauses and loses focus, the bootstrapper flushes saves.
            Assert.IsTrue(Services.FlushAll());
            Assert.IsFalse(Loop.Pause(), "Game over can't be paused.");
            Assert.IsTrue(Loop.IsRevivePending);
            Assert.IsTrue(Loop.Session.IsOver);

            _rig.Ads.Finish(RewardedAdResult.Completed);
            Assert.IsFalse(Loop.Session.IsOver);
        }

        [Test]
        public void RestartOrLeavingDuringAd_LateRewardIsIgnored()
        {
            ReachGameOverWithScore();
            Loop.TryRevive();
            Loop.Restart();
            Assert.IsFalse(Services.Ads.IsBusy, "Restart stops waiting for the ad.");

            _rig.Ads.Finish(RewardedAdResult.Completed);

            Assert.AreEqual(0, Loop.Session.RevivesUsed);
            Assert.AreEqual(0, Loop.Session.Score, "The new game is untouched.");
            Assert.AreEqual(1, Services.Ads.IgnoredCallbacks);

            Loop.EndGame();
            Loop.TryRevive();
            Loop.ReturnToMainMenu();
            _rig.Ads.Finish(RewardedAdResult.Completed);
            Assert.IsTrue(Loop.Session.IsOver);
            Assert.AreEqual(0, Loop.Session.RevivesUsed);
            Assert.IsFalse(Services.Ads.IsBusy);
        }
    }
}
