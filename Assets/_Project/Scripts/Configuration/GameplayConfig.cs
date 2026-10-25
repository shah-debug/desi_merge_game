using System.Collections.Generic;
using UnityEngine;

namespace DesiMerge.Configuration
{
    /// <summary>
    /// Tuning for the core drop-and-merge loop. Board coordinates are in world units, relative to the board's transform:
    /// x = 0 is the container centre and y = 0 is the container floor.
    /// </summary>
    [CreateAssetMenu(fileName = "GameplayConfig", menuName = "Desi Merge/Gameplay Config")]
    public sealed class GameplayConfig : ScriptableObject
    {
        [SerializeField] private MergeProgression progression;

        [Header("Board")]
        [SerializeField, Min(1f)] private float containerWidth = 6.5f;
        [SerializeField, Min(1f)] private float containerHeight = 9f;
        [Tooltip("Height above the floor where items are dropped from.")]
        [SerializeField, Min(1f)] private float dropHeight = 10.5f;

        [Header("Dropping")]
        [Tooltip("Minimum time between drops.")]
        [SerializeField, Min(0f)] private float dropCooldownSeconds = 0.5f;
        [Tooltip("Seed for the next-item sequence. 0 = random each session.")]
        [SerializeField] private int spawnSeed;

        [Header("Game over")]
        [Tooltip("Height above the floor of the danger line (normally the container top).")]
        [SerializeField, Min(0.5f)] private float dangerLineHeight = 9f;
        [Tooltip("An item whose top stays above the danger line this long ends the game.")]
        [SerializeField, Min(0.1f)] private float gameOverDelaySeconds = 2f;
        [Tooltip("Newly dropped or merged items are ignored by the game-over check for this long.")]
        [SerializeField, Min(0f)] private float spawnGraceSeconds = 1.5f;

        [Header("Final level")]
        [SerializeField] private FinalLevelMergeRule finalLevelRule = FinalLevelMergeRule.ClearBoth;
        [SerializeField, Min(0)] private int finalMergeBonus = 100;

        [Header("Physics")]
        [SerializeField] private PhysicsMaterial2D itemMaterial;
        [SerializeField, Min(0f)] private float gravityScale = 1.5f;
        [SerializeField, Min(0f)] private float linearDamping = 0.1f;
        [SerializeField, Min(0f)] private float angularDamping = 0.5f;

        public MergeProgression Progression => progression;
        public float ContainerWidth => containerWidth;
        public float ContainerHeight => containerHeight;
        public float HalfWidth => containerWidth * 0.5f;
        public float DropHeight => dropHeight;
        public float DropCooldownSeconds => dropCooldownSeconds;
        public int SpawnSeed => spawnSeed;
        public float DangerLineHeight => dangerLineHeight;
        public float GameOverDelaySeconds => gameOverDelaySeconds;
        public float SpawnGraceSeconds => spawnGraceSeconds;
        public FinalLevelMergeRule FinalLevelRule => finalLevelRule;
        public int FinalMergeBonus => finalMergeBonus;
        public PhysicsMaterial2D ItemMaterial => itemMaterial;
        public float GravityScale => gravityScale;
        public float LinearDamping => linearDamping;
        public float AngularDamping => angularDamping;

        /// <summary>Returns human-readable problems (including the progression's); empty when usable.</summary>
        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            if (progression == null)
            {
                errors.Add("Gameplay config has no merge progression assigned.");
                return errors;
            }

            errors.AddRange(progression.Validate());

            if (dangerLineHeight > containerHeight + 0.001f)
            {
                errors.Add("Danger line must not be above the container top.");
            }

            if (dropHeight <= dangerLineHeight)
            {
                errors.Add("Drop height must be above the danger line.");
            }

            for (var i = 0; i < progression.Count; i++)
            {
                var definition = progression.Get(i);
                if (definition != null && definition.Radius * 2f > containerWidth)
                {
                    errors.Add($"Level {i} ({definition.Id}) is wider than the container.");
                }
            }

            return errors;
        }

        /// <summary>Used by editor tooling and tests to author configs in code.</summary>
        internal void Set(
            MergeProgression mergeProgression,
            PhysicsMaterial2D material,
            float width = 6.5f,
            float height = 9f,
            float drop = 10.5f,
            float dangerLine = 9f,
            float cooldown = 0.5f,
            float gameOverDelay = 2f,
            float grace = 1.5f,
            FinalLevelMergeRule finalRule = FinalLevelMergeRule.ClearBoth,
            int finalBonus = 100,
            int seed = 0)
        {
            progression = mergeProgression;
            itemMaterial = material;
            containerWidth = width;
            containerHeight = height;
            dropHeight = drop;
            dangerLineHeight = dangerLine;
            dropCooldownSeconds = cooldown;
            gameOverDelaySeconds = gameOverDelay;
            spawnGraceSeconds = grace;
            finalLevelRule = finalRule;
            finalMergeBonus = finalBonus;
            spawnSeed = seed;
        }
    }
}
