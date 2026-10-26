using DesiMerge.Core;
using NUnit.Framework;
using UnityEngine;

namespace DesiMerge.Tests.PlayMode
{
    /// <summary>Pause, resume, restart and leaving to the menu, driven through the same view events as the HUD.</summary>
    public class PauseAndFlowTests
    {
        private GameplayTestRig _rig;

        [SetUp]
        public void SetUp()
        {
            _rig = new GameplayTestRig();
            _rig.CreateLoop();
        }

        [TearDown]
        public void TearDown() => _rig.Dispose();

        [Test]
        public void Begin_PlaysGameplayMusicAndShowsInstructions()
        {
            Assert.AreEqual(DesiMerge.Audio.MusicTrack.Gameplay, _rig.Audio.CurrentTrack);
            Assert.IsTrue(_rig.View.InstructionsVisible);
            Assert.AreEqual(1f, _rig.TimeScale);
        }

        [Test]
        public void Pause_StopsTimeInputAndMusic()
        {
            _rig.View.ClickPause();

            Assert.AreEqual(GameState.Paused, _rig.Services.GameState.Current);
            Assert.AreEqual(0f, _rig.TimeScale);
            Assert.IsFalse(_rig.Drop.InputEnabled);
            Assert.IsTrue(_rig.Audio.MusicPaused);
            Assert.AreEqual(1, _rig.View.PauseShown);
            Assert.IsFalse(_rig.Loop.TryDrop(0f), "No drops while paused.");
        }

        [Test]
        public void Resume_RestoresTimeInputAndMusic()
        {
            _rig.View.ClickPause();

            _rig.View.ClickResume();

            Assert.AreEqual(GameState.Playing, _rig.Services.GameState.Current);
            Assert.AreEqual(1f, _rig.TimeScale);
            Assert.IsTrue(_rig.Drop.InputEnabled);
            Assert.IsFalse(_rig.Audio.MusicPaused);
            Assert.IsTrue(_rig.Loop.TryDrop(0f));
        }

        [Test]
        public void RepeatedPauseAndResumeRequests_AreIgnored()
        {
            Assert.IsTrue(_rig.Loop.Pause());
            Assert.IsFalse(_rig.Loop.Pause());
            Assert.AreEqual(1, _rig.View.PauseShown);

            Assert.IsTrue(_rig.Loop.Resume());
            Assert.IsFalse(_rig.Loop.Resume());
        }

        [Test]
        public void Pause_IsRejectedAfterGameOver()
        {
            _rig.Loop.EndGame();

            Assert.IsFalse(_rig.Loop.Pause());
            Assert.AreEqual(GameState.GameOver, _rig.Services.GameState.Current);
            Assert.AreEqual(0, _rig.View.PauseShown);
        }

        [Test]
        public void GameOver_ShowsActualFinalScoreAndStopsMusic()
        {
            _rig.SpawnOnFloor(0, -0.25f);
            _rig.SpawnOnFloor(0, 0.25f);
            _rig.Simulate();

            _rig.Loop.EndGame();

            Assert.AreEqual(_rig.Loop.Session.Score, _rig.View.LastFinalScore);
            Assert.Greater(_rig.View.LastFinalScore, 0);
            Assert.AreEqual(DesiMerge.Audio.MusicTrack.None, _rig.Audio.CurrentTrack);
        }

        [Test]
        public void Restart_FromPause_ResumesTimeAndHidesPanel()
        {
            _rig.Loop.Pause();

            _rig.View.ClickRestart();

            Assert.AreEqual(GameState.Playing, _rig.Services.GameState.Current);
            Assert.AreEqual(1f, _rig.TimeScale);
            Assert.Greater(_rig.View.PauseHidden, 0);
            Assert.AreEqual(DesiMerge.Audio.MusicTrack.Gameplay, _rig.Audio.CurrentTrack);
            Assert.IsTrue(_rig.Loop.TryDrop(0f));
        }

        [Test]
        public void MainMenu_LeavesOnceRestoresTimeAndSaves()
        {
            _rig.Loop.Pause();
            _rig.Services.Settings.MusicVolume = 0.2f;

            _rig.View.ClickMainMenu();
            _rig.View.ClickMainMenu();

            Assert.AreEqual(1, _rig.Navigator.MainMenuLoads, "Double tap must not load twice.");
            Assert.AreEqual(GameState.MainMenu, _rig.Services.GameState.Current);
            Assert.AreEqual(1f, _rig.TimeScale);
            Assert.IsFalse(_rig.Drop.InputEnabled);
            Assert.IsTrue(_rig.Store.TryRead(DesiMerge.Persistence.SaveKey.Settings, out _), "Pending settings are saved before leaving.");
            Assert.IsFalse(_rig.Loop.TryDrop(0f));
            Assert.IsFalse(_rig.Loop.Restart(), "No restart once leaving.");
        }

        [Test]
        public void Dispose_WhilePaused_RestoresTimeScale()
        {
            _rig.Loop.Pause();

            _rig.Loop.Dispose();

            Assert.AreEqual(1f, _rig.TimeScale);
            _rig.View.ClickResume();
            Assert.AreEqual(GameState.Paused, _rig.Services.GameState.Current, "Disposed loop no longer reacts to the view.");
        }

        [Test]
        public void Merge_ShowsFeedbackOnceWithAwardedPoints()
        {
            _rig.SpawnOnFloor(0, -0.25f);
            _rig.SpawnOnFloor(0, 0.25f);

            _rig.Simulate(10);

            Assert.AreEqual(1, _rig.View.MergeFeedbackCount);
            Assert.AreEqual(_rig.Progression.Get(1).ScoreValue, _rig.View.LastFeedbackPoints);
        }

        [Test]
        public void FirstDrop_HidesInstructionsUntilRestart()
        {
            _rig.Loop.TryDrop(0f);
            Assert.IsFalse(_rig.View.InstructionsVisible);

            _rig.Loop.Restart();
            Assert.IsTrue(_rig.View.InstructionsVisible);
        }

        [Test]
        public void PressOnHudControl_DoesNotDrop()
        {
            _rig.View.BlockAboveY = 500f;

            _rig.Drop.HandlePressStarted(new Vector2(100f, 900f));
            _rig.Drop.HandlePressReleased();

            Assert.AreEqual(0, _rig.Board.ActiveCount, "A tap on the HUD (e.g. Pause) must not drop an item.");
        }

        [Test]
        public void PressInPlayArea_Drops()
        {
            _rig.View.BlockAboveY = 500f;

            _rig.Drop.HandlePressStarted(new Vector2(100f, 200f));
            _rig.Drop.HandlePressReleased();

            Assert.AreEqual(1, _rig.Board.ActiveCount);
        }

        [Test]
        public void PressStartedWhilePaused_DoesNotDropAfterResume()
        {
            _rig.Loop.Pause();
            _rig.Drop.HandlePressStarted(new Vector2(100f, 200f));
            _rig.Loop.Resume();

            _rig.Drop.HandlePressReleased();

            Assert.AreEqual(0, _rig.Board.ActiveCount, "The Resume tap must not also drop an item.");
        }
    }
}
