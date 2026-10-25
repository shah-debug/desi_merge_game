using DesiMerge.Core;
using NUnit.Framework;

namespace DesiMerge.Tests.EditMode
{
    public class GameStateMachineTests
    {
        [Test]
        public void StartsInBoot()
        {
            Assert.AreEqual(GameState.Boot, new GameStateMachine().Current);
        }

        [TestCase(GameState.Boot, GameState.MainMenu)]
        [TestCase(GameState.MainMenu, GameState.Playing)]
        [TestCase(GameState.Playing, GameState.Paused)]
        [TestCase(GameState.Paused, GameState.Playing)]
        [TestCase(GameState.Playing, GameState.GameOver)]
        [TestCase(GameState.GameOver, GameState.Playing)]
        [TestCase(GameState.GameOver, GameState.MainMenu)]
        public void AllowsValidTransition(GameState from, GameState to)
        {
            var machine = new GameStateMachine(from);

            Assert.IsTrue(machine.TryTransitionTo(to));
            Assert.AreEqual(to, machine.Current);
        }

        [TestCase(GameState.Boot, GameState.Playing)]
        [TestCase(GameState.MainMenu, GameState.GameOver)]
        [TestCase(GameState.Paused, GameState.GameOver)]
        [TestCase(GameState.Playing, GameState.Playing)]
        [TestCase(GameState.GameOver, GameState.Boot)]
        public void RejectsInvalidTransitionWithoutChangingState(GameState from, GameState to)
        {
            var machine = new GameStateMachine(from);
            var raised = false;
            machine.StateChanged += (_, __) => raised = true;

            Assert.IsFalse(machine.TryTransitionTo(to));
            Assert.AreEqual(from, machine.Current);
            Assert.IsFalse(raised);
        }

        [Test]
        public void RaisesStateChangedWithPreviousAndNext()
        {
            var machine = new GameStateMachine();
            GameState? previous = null;
            GameState? next = null;
            machine.StateChanged += (p, n) =>
            {
                previous = p;
                next = n;
            };

            machine.TryTransitionTo(GameState.MainMenu);

            Assert.AreEqual(GameState.Boot, previous);
            Assert.AreEqual(GameState.MainMenu, next);
        }
    }
}
