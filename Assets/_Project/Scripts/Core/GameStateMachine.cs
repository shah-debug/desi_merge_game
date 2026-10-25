using System;
using System.Collections.Generic;

namespace DesiMerge.Core
{
    /// <summary>
    /// Explicit, table-driven state machine for the top-level game flow.
    /// Pure C# so it can be unit tested without a scene.
    /// </summary>
    public sealed class GameStateMachine : IGameStateService
    {
        private static readonly Dictionary<GameState, GameState[]> AllowedTransitions = new Dictionary<GameState, GameState[]>
        {
            { GameState.Boot, new[] { GameState.MainMenu } },
            { GameState.MainMenu, new[] { GameState.Playing } },
            { GameState.Playing, new[] { GameState.Paused, GameState.GameOver, GameState.MainMenu } },
            { GameState.Paused, new[] { GameState.Playing, GameState.MainMenu } },
            { GameState.GameOver, new[] { GameState.Playing, GameState.MainMenu } },
        };

        public GameStateMachine(GameState initial = GameState.Boot)
        {
            Current = initial;
        }

        public GameState Current { get; private set; }

        public event Action<GameState, GameState> StateChanged;

        public bool CanTransitionTo(GameState next)
        {
            return AllowedTransitions.TryGetValue(Current, out var targets) && Array.IndexOf(targets, next) >= 0;
        }

        public bool TryTransitionTo(GameState next)
        {
            if (!CanTransitionTo(next))
            {
                return false;
            }

            var previous = Current;
            Current = next;
            StateChanged?.Invoke(previous, next);
            return true;
        }
    }
}
