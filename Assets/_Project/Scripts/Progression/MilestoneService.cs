using System;
using System.Collections.Generic;
using DesiMerge.Persistence;
using UnityEngine;

namespace DesiMerge.Progression
{
    [Serializable]
    public sealed class MilestoneSaveData
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public List<string> reachedIds = new List<string>();
        public List<string> claimedIds = new List<string>();
    }

    public interface IMilestoneService
    {
        /// <summary>Valid milestones in ascending score order.</summary>
        IReadOnlyList<MilestoneDefinition> Definitions { get; }

        /// <summary>The highest reached milestone (the player's rank), or null before the first one.</summary>
        MilestoneDefinition Current { get; }

        /// <summary>The next milestone to reach, or null when all are reached.</summary>
        MilestoneDefinition Next { get; }

        /// <summary>0..1 progress of the best score from the current milestone to the next.</summary>
        float ProgressToNext { get; }

        int ClaimableCount { get; }

        event Action Changed;

        /// <summary>Raised once per milestone, when a final score first reaches it during play.</summary>
        event Action<MilestoneDefinition> Reached;

        bool IsReached(string id);

        bool IsClaimed(string id);

        ClaimResult TryClaim(string id);

        bool Flush();
    }

    /// <summary>
    /// Milestones follow the saved best score, which is never lowered. Milestones already earned by an existing best
    /// score are marked reached at startup (claimable, without a notification). Each reward can be claimed once.
    /// </summary>
    public sealed class MilestoneService : IMilestoneService, IDisposable
    {
        private readonly JsonRepository<MilestoneSaveData> _repository;
        private readonly MilestoneSaveData _data;
        private readonly List<MilestoneDefinition> _definitions = new List<MilestoneDefinition>();
        private readonly HashSet<string> _reached;
        private readonly HashSet<string> _claimed;
        private readonly IPlayerProgressService _progress;
        private readonly IRewardGranter _rewards;
        private bool _dirty;

        public MilestoneService(ISaveStore store, MilestoneCatalog catalog, IPlayerProgressService progress, IRewardGranter rewards)
        {
            _repository = new JsonRepository<MilestoneSaveData>(store, SaveKey.Milestones);
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _rewards = rewards;
            _data = _repository.Load();
            _data.schemaVersion = MilestoneSaveData.CurrentSchemaVersion;
            _reached = new HashSet<string>(_data.reachedIds ?? new List<string>());
            _claimed = new HashSet<string>(_data.claimedIds ?? new List<string>());
            _reached.RemoveWhere(string.IsNullOrWhiteSpace);
            _claimed.RemoveWhere(string.IsNullOrWhiteSpace);

            if (catalog != null)
            {
                var ids = new HashSet<string>();
                foreach (var milestone in catalog.All)
                {
                    if (MilestoneCatalog.IsValid(milestone) && ids.Add(milestone.id))
                    {
                        _definitions.Add(milestone);
                    }
                }
            }

            _definitions.Sort((a, b) => a.score.CompareTo(b.score));
            Evaluate(_progress.HighScore, notify: false);
            _progress.FinalScoreRecorded += OnFinalScoreRecorded;
        }

        public IReadOnlyList<MilestoneDefinition> Definitions => _definitions;

        public MilestoneDefinition Current
        {
            get
            {
                MilestoneDefinition current = null;
                foreach (var definition in _definitions)
                {
                    if (_reached.Contains(definition.id))
                    {
                        current = definition;
                    }
                }

                return current;
            }
        }

        public MilestoneDefinition Next
        {
            get
            {
                foreach (var definition in _definitions)
                {
                    if (!_reached.Contains(definition.id))
                    {
                        return definition;
                    }
                }

                return null;
            }
        }

        public float ProgressToNext
        {
            get
            {
                var next = Next;
                if (next == null)
                {
                    return 1f;
                }

                var from = Current?.score ?? 0;
                var span = next.score - from;
                return span <= 0 ? 1f : Mathf.Clamp01((float)(_progress.HighScore - from) / span);
            }
        }

        public int ClaimableCount
        {
            get
            {
                var count = 0;
                foreach (var definition in _definitions)
                {
                    count += CanClaim(definition) ? 1 : 0;
                }

                return count;
            }
        }

        public event Action Changed;
        public event Action<MilestoneDefinition> Reached;

        internal static string GrantPrefixFor(MilestoneDefinition milestone) => "milestone:" + milestone.id;

        public bool IsReached(string id) => id != null && _reached.Contains(id);

        public bool IsClaimed(string id) => id != null && _claimed.Contains(id);

        public ClaimResult TryClaim(string id)
        {
            var definition = _definitions.Find(candidate => candidate.id == id);
            if (definition == null || !CanClaim(definition))
            {
                return ClaimResult.NotAvailable;
            }

            var result = RewardClaims.GrantAll(_rewards, GrantPrefixFor(definition), new[] { definition.reward });
            if (result != ClaimResult.Claimed)
            {
                return result;
            }

            _claimed.Add(id);
            _dirty = true;
            Flush();
            Changed?.Invoke();
            return ClaimResult.Claimed;
        }

        public bool Flush()
        {
            if (!_dirty)
            {
                return true;
            }

            _data.reachedIds = new List<string>(_reached);
            _data.claimedIds = new List<string>(_claimed);
            if (!_repository.Save(_data))
            {
                return false;
            }

            _dirty = false;
            return true;
        }

        public void Dispose() => _progress.FinalScoreRecorded -= OnFinalScoreRecorded;

        private bool CanClaim(MilestoneDefinition definition) =>
            definition.reward.IsSomething && _reached.Contains(definition.id) && !_claimed.Contains(definition.id);

        private void OnFinalScoreRecorded(long score) => Evaluate(Math.Max(score, _progress.HighScore), notify: true);

        private void Evaluate(long bestScore, bool notify)
        {
            List<MilestoneDefinition> reached = null;
            foreach (var definition in _definitions)
            {
                if (bestScore >= definition.score && _reached.Add(definition.id))
                {
                    (reached ??= new List<MilestoneDefinition>()).Add(definition);
                }
            }

            if (reached == null)
            {
                return;
            }

            _dirty = true;
            Changed?.Invoke();
            if (!notify)
            {
                return;
            }

            foreach (var definition in reached)
            {
                Reached?.Invoke(definition);
            }
        }
    }
}
