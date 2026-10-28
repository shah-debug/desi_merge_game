using System;
using System.Collections.Generic;
using DesiMerge.Configuration;
using UnityEngine;
using UnityEngine.Pool;

namespace DesiMerge.Gameplay
{
    /// <summary>
    /// The play container: builds its walls from <see cref="GameplayConfig"/>, owns the pooled items,
    /// resolves merges once per physics step and watches the danger line.
    /// It reports what happened through events and knows nothing about score, UI or services.
    /// Board-local space: x = 0 is the container centre, y = 0 the floor.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MergeBoard : MonoBehaviour, IMergeContactSink, IMergeHandler<MergeItem>
    {
        private const float WallThickness = 0.5f;

        [SerializeField] private MergeItem itemPrefab;
        [SerializeField] private Transform itemRoot;

        [Header("Placeholder visuals (optional)")]
        [SerializeField] private Sprite blockSprite;
        [SerializeField] private Color wallColor = new Color(0.55f, 0.3f, 0.15f);
        [SerializeField] private Color backgroundColor = new Color(1f, 0.95f, 0.85f, 0.35f);
        [SerializeField] private Color dangerLineColor = new Color(0.9f, 0.15f, 0.15f, 0.8f);

        private readonly List<MergeItem> _active = new List<MergeItem>();
        /// <summary>Items merged away during the current step; pooled only after the step (see RetireMerged).</summary>
        private readonly List<MergeItem> _retired = new List<MergeItem>();
        private GameplayConfig _config;
        private MergeResolver<MergeItem> _resolver;
        private ObjectPool<MergeItem> _pool;
        private Func<float> _clock;
        private float _stepTime;
        private bool _frozen;
        private bool _dangerRaised;

        /// <summary>A merge created an item of the given level at the given world position.</summary>
        public event Action<int, Vector2> ItemMerged;

        /// <summary>Two final-level items were cleared at the given world position.</summary>
        public event Action<Vector2> FinalLevelCleared;

        /// <summary>An item stayed above the danger line too long. Raised once until <see cref="Clear"/>.</summary>
        public event Action DangerTimeExceeded;

        public bool IsInitialized => _config != null;
        public int ActiveCount => _active.Count;
        public IReadOnlyList<MergeItem> ActiveItems => _active;

        public void Initialize(GameplayConfig config, Func<float> clock)
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException("Board is already initialized.");
            }

            if (itemPrefab == null)
            {
                throw new InvalidOperationException("Board has no item prefab assigned.");
            }

            _config = config;
            _clock = clock;
            _resolver = new MergeResolver<MergeItem>(config.Progression, config.FinalLevelRule);
            _pool = new ObjectPool<MergeItem>(CreateItem, actionOnRelease: item => item.Deactivate(), actionOnDestroy: DestroyItem, defaultCapacity: 32);
            BuildContainer();
        }

        public Vector2 LocalToWorld(Vector2 local) => transform.TransformPoint(local);

        /// <summary>Spawns an item at a board-local position (used for drops).</summary>
        public MergeItem SpawnLocal(int level, Vector2 localPosition)
        {
            return SpawnWorld(level, LocalToWorld(localPosition), Vector2.zero, _clock());
        }

        public void ReportContact(MergeItem self, MergeItem other)
        {
            if (!_frozen)
            {
                _resolver.Report(self, other);
            }
        }

        /// <summary>Stops physics and merging (game over). Items stay visible.</summary>
        public void SetFrozen(bool frozen)
        {
            _frozen = frozen;
            foreach (var item in _active)
            {
                item.SetFrozen(frozen);
            }

            if (frozen)
            {
                _resolver.Clear();
            }
        }

        public Vector2 WorldToLocal(Vector2 world) => transform.InverseTransformPoint(world);

        /// <summary>The item under a board-local point (closest centre within its radius + tolerance), or null.</summary>
        public MergeItem FindItemAt(Vector2 localPoint, float tolerance)
        {
            MergeItem best = null;
            var bestDistance = float.MaxValue;
            foreach (var item in _active)
            {
                if (item.IsConsumed)
                {
                    continue;
                }

                var distance = Vector2.Distance(WorldToLocal(item.Body.position), localPoint);
                if (distance <= item.Radius + tolerance && distance < bestDistance)
                {
                    best = item;
                    bestDistance = distance;
                }
            }

            return best;
        }

        /// <summary>Removes one item without a merge (Hammer). Returns false if it isn't an active item on this board.</summary>
        public bool TryRemove(MergeItem item)
        {
            if (item == null || item.IsConsumed || !_active.Contains(item))
            {
                return false;
            }

            _resolver.Forget(item);
            Release(item);
            return true;
        }

        /// <summary>
        /// Moves every active item to a planned position (Shuffle). The whole plan is validated first: a plan that
        /// doesn't fit or overlaps is refused and nothing moves. Pending contacts are discarded (they are stale).
        /// </summary>
        public bool TryShuffle(System.Random random)
        {
            var radii = new List<float>(_active.Count);
            foreach (var item in _active)
            {
                radii.Add(item.Radius);
            }

            var maxTop = _config.DangerLineHeight - _config.ShuffleTopMargin;
            var positions = new List<Vector2>(radii.Count);
            if (_active.Count == 0 ||
                !ShufflePlanner.TryPlan(radii, _config.HalfWidth, maxTop, _config.ShuffleGap, random, positions) ||
                ShufflePlanner.HasOverlap(positions, radii) ||
                !ShufflePlanner.AllInside(positions, radii, _config.HalfWidth, maxTop))
            {
                return false;
            }

            _resolver.Clear();
            for (var i = 0; i < _active.Count; i++)
            {
                _active[i].Teleport(LocalToWorld(positions[i]));
            }

            return true;
        }

        /// <summary>
        /// Makes the board safe to continue after a revive: removes every item whose top is above
        /// (danger line − clearance), restarts the grace period of the rest and re-arms the danger check.
        /// Returns how many items were removed. Removed items give no score and are not merges.
        /// </summary>
        public int ClearForRevive(float clearance, float now)
        {
            var limit = _config.DangerLineHeight - clearance;
            var removed = 0;
            for (var i = _active.Count - 1; i >= 0; i--)
            {
                var item = _active[i];
                if (WorldToLocal(item.Body.position).y + item.Radius > limit)
                {
                    Release(item);
                    removed++;
                }
            }

            foreach (var item in _active)
            {
                item.RestartGrace(now);
            }

            _resolver.Clear();
            _dangerRaised = false;
            return removed;
        }

        /// <summary>Returns every item to the pool and resets merge and danger state.</summary>
        public void Clear()
        {
            if (!IsInitialized)
            {
                return;
            }

            for (var i = _active.Count - 1; i >= 0; i--)
            {
                _pool.Release(_active[i]);
            }

            _active.Clear();
            ReturnRetiredToPool();
            _resolver?.Clear();
            _frozen = false;
            _dangerRaised = false;
        }

        private void FixedUpdate()
        {
            if (IsInitialized)
            {
                Step(Time.fixedTime);
            }
        }

        /// <summary>One gameplay step: resolve contacts from the last physics step, then check the danger line.</summary>
        internal void Step(float now)
        {
            if (_frozen)
            {
                return;
            }

            _stepTime = now;
            _resolver.ResolvePending(this);
            ReturnRetiredToPool();
            CheckDangerLine(now);
        }

        void IMergeHandler<MergeItem>.OnUpgrade(MergeItem first, MergeItem second, int resultLevel)
        {
            var position = (first.Body.position + second.Body.position) * 0.5f;
            var velocity = (first.Body.linearVelocity + second.Body.linearVelocity) * 0.5f;
            RetireMerged(first);
            RetireMerged(second);
            SpawnWorld(resultLevel, position, velocity, _stepTime).PlaySpawnPop();
            ItemMerged?.Invoke(resultLevel, position);
        }

        void IMergeHandler<MergeItem>.OnFinalCleared(MergeItem first, MergeItem second)
        {
            var position = (first.Body.position + second.Body.position) * 0.5f;
            RetireMerged(first);
            RetireMerged(second);
            FinalLevelCleared?.Invoke(position);
        }

        /// <summary>
        /// Removes a merged item from play but keeps it out of the pool until the step is resolved. Otherwise the pool
        /// could hand it straight back as the merge result, un-consumed, and a stale contact reported in the same step
        /// would merge two items that never touched.
        /// </summary>
        private void RetireMerged(MergeItem item)
        {
            _active.Remove(item);
            item.Deactivate();
            _retired.Add(item);
        }

        private void ReturnRetiredToPool()
        {
            foreach (var item in _retired)
            {
                _pool.Release(item);
            }

            _retired.Clear();
        }

        private MergeItem SpawnWorld(int level, Vector2 worldPosition, Vector2 velocity, float spawnTime)
        {
            var item = _pool.Get();
            item.Activate(this, _config.Progression.Get(level), level, worldPosition, velocity, spawnTime, _config);
            _active.Add(item);
            return item;
        }

        private void Release(MergeItem item)
        {
            _active.Remove(item);
            _pool.Release(item);
        }

        private void CheckDangerLine(float now)
        {
            if (_dangerRaised)
            {
                return;
            }

            var dangerLine = transform.position.y + _config.DangerLineHeight;
            foreach (var item in _active)
            {
                var top = item.Body.position.y + item.Radius;
                if (GameOverRule.Evaluate(now, top, dangerLine, item.SpawnTime, _config.SpawnGraceSeconds, _config.GameOverDelaySeconds, ref item.AboveLineSince))
                {
                    _dangerRaised = true;
                    DangerTimeExceeded?.Invoke();
                    return;
                }
            }
        }

        private MergeItem CreateItem()
        {
            var item = Instantiate(itemPrefab, itemRoot != null ? itemRoot : transform);
            item.gameObject.SetActive(false);
            return item;
        }

        private static void DestroyItem(MergeItem item)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }

        private void BuildContainer()
        {
            var width = _config.ContainerWidth;
            var halfWidth = _config.HalfWidth;
            var wallHeight = _config.DropHeight + 2f;

            var walls = new GameObject("Walls").transform;
            walls.SetParent(transform, false);

            CreateBlock(walls, "Floor", new Vector2(0f, -WallThickness * 0.5f), new Vector2(width + WallThickness * 2f, WallThickness), wallColor, 5, true);
            CreateBlock(walls, "LeftWall", new Vector2(-halfWidth - WallThickness * 0.5f, wallHeight * 0.5f), new Vector2(WallThickness, wallHeight), wallColor, 5, true);
            CreateBlock(walls, "RightWall", new Vector2(halfWidth + WallThickness * 0.5f, wallHeight * 0.5f), new Vector2(WallThickness, wallHeight), wallColor, 5, true);
            CreateBlock(walls, "Background", new Vector2(0f, _config.ContainerHeight * 0.5f), new Vector2(width, _config.ContainerHeight), backgroundColor, -10, false);
            CreateBlock(walls, "DangerLine", new Vector2(0f, _config.DangerLineHeight), new Vector2(width, 0.06f), dangerLineColor, 4, false);
        }

        private void CreateBlock(Transform parent, string blockName, Vector2 localCenter, Vector2 size, Color color, int sortingOrder, bool solid)
        {
            var block = new GameObject(blockName);
            block.transform.SetParent(parent, false);
            block.transform.localPosition = localCenter;

            if (blockSprite != null)
            {
                var blockRenderer = block.AddComponent<SpriteRenderer>();
                blockRenderer.sprite = blockSprite;
                blockRenderer.color = color;
                blockRenderer.sortingOrder = sortingOrder;
                blockRenderer.drawMode = SpriteDrawMode.Sliced;
                blockRenderer.size = size;
            }

            if (solid)
            {
                var blockCollider = block.AddComponent<BoxCollider2D>();
                blockCollider.size = size;
                blockCollider.sharedMaterial = _config.ItemMaterial;
            }
        }

        /// <summary>Test hook for building boards in code.</summary>
        internal void SetPrefabForTests(MergeItem prefab)
        {
            itemPrefab = prefab;
        }
    }
}
