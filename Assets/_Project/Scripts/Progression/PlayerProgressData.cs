using System;

namespace DesiMerge.Progression
{
    /// <summary>Serialized player progress. Bump <see cref="CurrentSchemaVersion"/> when the shape changes and migrate on load.</summary>
    [Serializable]
    public sealed class PlayerProgressData
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public long highScore;
        public long coins;
        public int gamesPlayed;
    }
}
