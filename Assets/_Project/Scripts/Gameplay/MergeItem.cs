using DesiMerge.Configuration;
using UnityEngine;

namespace DesiMerge.Gameplay
{
    /// <summary>Receives contact reports from <see cref="MergeItem"/>s. Implemented by the board.</summary>
    public interface IMergeContactSink
    {
        void ReportContact(MergeItem self, MergeItem other);
    }

    /// <summary>
    /// A pooled physics body on the board. Holds no game rules: it only applies its definition's look and physics
    /// and reports contacts with other items to the board.
    /// The prefab's sprite and collider are authored at 1 unit diameter; the transform is scaled to the item's size.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    [DisallowMultipleComponent]
    public sealed class MergeItem : MonoBehaviour, IMergeable
    {
        private static readonly Color DarkLabel = new Color(0.1f, 0.08f, 0.06f);

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private TextMesh label;

        private IMergeContactSink _sink;

        public int Level { get; private set; }
        public MergeItemDefinition Definition { get; private set; }
        public bool IsConsumed { get; private set; }
        public float Radius { get; private set; }
        public float SpawnTime { get; private set; }
        public Rigidbody2D Body => body;

        /// <summary>Danger-line timer used by <see cref="GameOverRule"/>.</summary>
        internal float AboveLineSince = GameOverRule.NotAbove;

        internal void Activate(IMergeContactSink sink, MergeItemDefinition definition, int level, Vector2 worldPosition, Vector2 velocity, float spawnTime, GameplayConfig config)
        {
            // Prefabs serialize these; items built in code (tests) resolve them here.
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            _sink = sink;
            Definition = definition;
            Level = level;
            Radius = definition.Radius;
            SpawnTime = spawnTime;
            IsConsumed = false;
            AboveLineSince = GameOverRule.NotAbove;

            transform.SetPositionAndRotation(worldPosition, Quaternion.identity);
            transform.localScale = Vector3.one * (definition.Radius * 2f);

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = definition.Sprite;
                spriteRenderer.color = definition.Tint;
            }

            if (label != null)
            {
                label.text = definition.DisplayName.Replace(' ', '\n');
                var tint = definition.Tint;
                var luminance = 0.299f * tint.r + 0.587f * tint.g + 0.114f * tint.b;
                label.color = luminance > 0.55f ? DarkLabel : Color.white;
            }

            body.mass = definition.Mass;
            body.gravityScale = config.GravityScale;
            body.linearDamping = config.LinearDamping;
            body.angularDamping = config.AngularDamping;
            body.sharedMaterial = config.ItemMaterial;
            body.simulated = true;

            gameObject.SetActive(true);
            body.position = worldPosition;
            body.rotation = 0f;
            body.linearVelocity = velocity;
            body.angularVelocity = 0f;
        }

        internal void Deactivate()
        {
            IsConsumed = true;
            _sink = null;
            gameObject.SetActive(false);
        }

        public void MarkConsumed()
        {
            IsConsumed = true;
        }

        internal void SetFrozen(bool frozen)
        {
            body.simulated = !frozen;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (IsConsumed || _sink == null)
            {
                return;
            }

            if (collision.collider.TryGetComponent(out MergeItem other) && !other.IsConsumed)
            {
                _sink.ReportContact(this, other);
            }
        }
    }
}
