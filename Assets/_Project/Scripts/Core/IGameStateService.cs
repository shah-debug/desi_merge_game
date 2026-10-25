using System;

namespace DesiMerge.Core
{
    public interface IGameStateService
    {
        GameState Current { get; }

        /// <summary>Raised after a successful transition with (previous, next).</summary>
        event Action<GameState, GameState> StateChanged;

        bool CanTransitionTo(GameState next);

        /// <summary>Attempts a transition. Returns false (and does not raise events) if the transition is not allowed.</summary>
        bool TryTransitionTo(GameState next);
    }
}
