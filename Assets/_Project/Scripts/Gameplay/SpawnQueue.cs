using System;

namespace DesiMerge.Gameplay
{
    /// <summary>
    /// The item about to be dropped (<see cref="Current"/>) and the one after it (<see cref="Next"/>, shown in the HUD).
    /// Levels are drawn uniformly from [0, spawnableLevelCount). A fixed seed makes the sequence reproducible.
    /// </summary>
    public sealed class SpawnQueue
    {
        private readonly int _spawnableLevelCount;
        private Random _random;

        public SpawnQueue(int spawnableLevelCount, int seed)
        {
            if (spawnableLevelCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(spawnableLevelCount));
            }

            _spawnableLevelCount = spawnableLevelCount;
            Reset(seed);
        }

        public int Current { get; private set; }
        public int Next { get; private set; }

        /// <summary>Puts back the current and next items of a saved game (clamped to the spawnable range).</summary>
        public void Restore(int current, int next)
        {
            Current = Math.Max(0, Math.Min(current, _spawnableLevelCount - 1));
            Next = Math.Max(0, Math.Min(next, _spawnableLevelCount - 1));
        }

        /// <summary>Returns the current level and advances the queue.</summary>
        public int Take()
        {
            var taken = Current;
            Current = Next;
            Next = Roll();
            return taken;
        }

        /// <param name="seed">0 picks a time-based seed.</param>
        public void Reset(int seed)
        {
            _random = seed == 0 ? new Random() : new Random(seed);
            Current = Roll();
            Next = Roll();
        }

        private int Roll() => _random.Next(_spawnableLevelCount);
    }
}
