using UnityEngine;

namespace DesiMerge.Configuration
{
    /// <summary>
    /// One mergeable object level (e.g. Patti, Nimbu). Its level and "next" item are defined by its position
    /// in <see cref="MergeProgression"/>, so the merge chain lives in exactly one place.
    /// </summary>
    [CreateAssetMenu(fileName = "MergeItem", menuName = "Desi Merge/Merge Item Definition")]
    public sealed class MergeItemDefinition : ScriptableObject
    {
        [Tooltip("Stable identifier used in saves and analytics. Never change it once shipped.")]
        [SerializeField] private string id = string.Empty;

        [Tooltip("Provisional player-facing name.")]
        [SerializeField] private string displayName = string.Empty;

        [Tooltip("Placeholder art. Drawn at diameter = 2 x radius world units (sprite should be 1 unit wide).")]
        [SerializeField] private Sprite sprite;

        [SerializeField] private Color tint = Color.white;

        [Tooltip("Multiply the sprite by the tint (single-colour placeholder art). Off for full-colour artwork; the tint is then only the accent colour for effects.")]
        [SerializeField] private bool tintSprite = true;

        [Tooltip("Collision and visual radius in world units.")]
        [SerializeField, Min(0.05f)] private float radius = 0.5f;

        [Tooltip("Rigidbody2D mass. Larger items should usually be heavier.")]
        [SerializeField, Min(0.01f)] private float mass = 1f;

        [Tooltip("Points awarded when this item is created by a merge.")]
        [SerializeField, Min(0)] private int scoreValue;

        public string Id => id;
        public string DisplayName => displayName;
        public Sprite Sprite => sprite;
        public Color Tint => tint;
        public Color SpriteColor => tintSprite ? tint : Color.white;
        public float Radius => radius;
        public float Mass => mass;
        public int ScoreValue => scoreValue;

        /// <summary>Used by editor tooling and tests to author definitions in code.</summary>
        internal void Set(string itemId, string name, Sprite itemSprite, Color itemTint, float itemRadius, float itemMass, int score)
        {
            id = itemId;
            displayName = name;
            sprite = itemSprite;
            tint = itemTint;
            radius = itemRadius;
            mass = itemMass;
            scoreValue = score;
        }
    }
}
