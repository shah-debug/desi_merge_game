using System.Collections.Generic;
using DesiMerge.Configuration;

namespace DesiMerge.Gameplay
{
    /// <summary>
    /// Collects contact reports during a physics step and resolves them afterwards, in report order.
    /// Both bodies of a contact usually report it, and one item can touch several equals at once;
    /// marking items consumed before handing them to the handler guarantees each item merges at most once.
    /// Items created by the handler are not resolved until the next step, so chains never recurse.
    /// </summary>
    public sealed class MergeResolver<T> where T : class, IMergeable
    {
        private readonly MergeProgression _progression;
        private readonly FinalLevelMergeRule _finalRule;
        private List<(T First, T Second)> _pending = new List<(T, T)>();
        private List<(T First, T Second)> _resolving = new List<(T, T)>();

        public MergeResolver(MergeProgression progression, FinalLevelMergeRule finalRule)
        {
            _progression = progression;
            _finalRule = finalRule;
        }

        public int PendingCount => _pending.Count;

        /// <summary>Queues a contact. Ignores self-contacts, different levels and consumed items up front.</summary>
        public void Report(T first, T second)
        {
            if (first == null || second == null || ReferenceEquals(first, second))
            {
                return;
            }

            if (first.IsConsumed || second.IsConsumed || first.Level != second.Level)
            {
                return;
            }

            _pending.Add((first, second));
        }

        /// <summary>Resolves everything reported so far. Returns the number of merges performed.</summary>
        public int ResolvePending(IMergeHandler<T> handler)
        {
            // Swap buffers so reports made while resolving are kept for the next step.
            (_pending, _resolving) = (_resolving, _pending);

            var merges = 0;
            foreach (var (first, second) in _resolving)
            {
                if (first.IsConsumed || second.IsConsumed)
                {
                    continue;
                }

                var decision = MergeRules.Decide(_progression, first.Level, second.Level, _finalRule);
                if (decision.Kind == MergeKind.None)
                {
                    continue;
                }

                first.MarkConsumed();
                second.MarkConsumed();
                merges++;

                if (decision.Kind == MergeKind.Upgrade)
                {
                    handler.OnUpgrade(first, second, decision.ResultLevel);
                }
                else
                {
                    handler.OnFinalCleared(first, second);
                }
            }

            _resolving.Clear();
            return merges;
        }

        /// <summary>Drops pending contacts involving an item removed outside a merge (Hammer), so a reused pooled object can't inherit them.</summary>
        public void Forget(T item)
        {
            _pending.RemoveAll(pair => ReferenceEquals(pair.First, item) || ReferenceEquals(pair.Second, item));
        }

        public void Clear()
        {
            _pending.Clear();
            _resolving.Clear();
        }
    }
}
