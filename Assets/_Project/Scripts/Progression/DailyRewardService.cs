using System;
using System.Collections.Generic;
using DesiMerge.Core;
using DesiMerge.Persistence;
using UnityEngine;

namespace DesiMerge.Progression
{
    [Serializable]
    public sealed class DailyRewardSaveData
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public int lastClaimDay = -1;
        public int lastClaimIndex = -1;
        public int streak;
        public int bestStreak;
        public int totalClaims;
        public int latestDay = -1;
    }

    public enum DailyRewardDayState
    {
        Claimed = 0,
        Available = 1,
        Upcoming = 2,
    }

    public interface IDailyRewardService
    {
        IReadOnlyList<DailyRewardDay> Days { get; }

        /// <summary>Calendar position of today's reward (or of the one claimed today).</summary>
        int TodayIndex { get; }

        bool CanClaim { get; }
        bool ClaimedToday { get; }

        /// <summary>Consecutive days claimed, as shown today (0 once a day has been missed).</summary>
        int Streak { get; }

        int BestStreak { get; }

        /// <summary>The device clock is earlier than a day already seen. Claims wait until that day is reached again.</summary>
        bool ClockMovedBack { get; }

        event Action Changed;

        DailyRewardDayState StateOf(int index);

        /// <summary>Re-reads the date. Call when a screen opens; there is no timer.</summary>
        void Refresh();

        ClaimResult TryClaim();

        bool Flush();
    }

    /// <summary>
    /// A 7-day calendar (any length works). One claim per UTC day (<see cref="DayKey"/>). Claiming on consecutive days
    /// moves along the calendar and wraps after the last day. Missing a day restarts the calendar at day 1 and the
    /// streak at 1. The effective day is max(device day, latest day seen), so moving the clock back never re-opens a
    /// claimed day (a clock more than <see cref="DayKey.ClockToleranceDays"/> days behind is treated as a corrected mistake:
    /// the calendar restarts at day 1). Moving it forward is believed (no server time).
    /// </summary>
    public sealed class DailyRewardService : IDailyRewardService
    {
        private readonly JsonRepository<DailyRewardSaveData> _repository;
        private readonly DailyRewardSaveData _data;
        private readonly List<DailyRewardDay> _days = new List<DailyRewardDay>();
        private readonly IClock _clock;
        private readonly IRewardGranter _rewards;
        private int _effectiveDay;
        private bool _dirty;

        public DailyRewardService(ISaveStore store, DailyRewardCatalog catalog, IClock clock, IRewardGranter rewards)
        {
            _repository = new JsonRepository<DailyRewardSaveData>(store, SaveKey.DailyRewards);
            _clock = clock ?? new SystemClock();
            _rewards = rewards;
            _data = _repository.Load();

            if (catalog != null)
            {
                foreach (var day in catalog.Days)
                {
                    if (DailyRewardCatalog.IsValid(day))
                    {
                        _days.Add(day);
                    }
                    else
                    {
                        Debug.LogWarning("[DailyRewards] Skipping a calendar day without a valid reward.");
                    }
                }
            }

            Sanitize();
            Refresh();
        }

        public IReadOnlyList<DailyRewardDay> Days => _days;
        public int TodayIndex { get; private set; }
        public bool ClaimedToday { get; private set; }
        public bool CanClaim => _days.Count > 0 && !ClaimedToday;
        public int Streak { get; private set; }
        public int BestStreak => _data.bestStreak;
        public bool ClockMovedBack { get; private set; }

        public event Action Changed;

        internal static string GrantPrefixFor(int day) => "dailyreward:" + day;

        public DailyRewardDayState StateOf(int index)
        {
            if (index < TodayIndex || (index == TodayIndex && ClaimedToday))
            {
                return DailyRewardDayState.Claimed;
            }

            return index == TodayIndex ? DailyRewardDayState.Available : DailyRewardDayState.Upcoming;
        }

        public void Refresh()
        {
            var today = DayKey.Today(_clock);
            _effectiveDay = DayKey.Effective(today, _data.latestDay);
            ClockMovedBack = today < _effectiveDay;
            if (_data.lastClaimDay > _effectiveDay)
            {
                _data.lastClaimDay = -1;
                _data.lastClaimIndex = -1;
                _data.streak = 0;
                _dirty = true;
            }

            if (_effectiveDay != _data.latestDay)
            {
                _data.latestDay = _effectiveDay;
                _dirty = true;
            }

            var count = Math.Max(1, _days.Count);
            if (_data.lastClaimDay < 0)
            {
                Set(0, false, 0);
            }
            else if (_effectiveDay == _data.lastClaimDay)
            {
                Set(_data.lastClaimIndex, true, _data.streak);
            }
            else if (_effectiveDay == _data.lastClaimDay + 1)
            {
                Set((_data.lastClaimIndex + 1) % count, false, _data.streak);
            }
            else
            {
                Set(0, false, 0);
            }
        }

        public ClaimResult TryClaim()
        {
            Refresh();
            if (!CanClaim)
            {
                return ClaimResult.NotAvailable;
            }

            var result = RewardClaims.GrantAll(_rewards, GrantPrefixFor(_effectiveDay), _days[TodayIndex].rewards);
            if (result != ClaimResult.Claimed)
            {
                return result;
            }

            var consecutive = _data.lastClaimDay >= 0 && _effectiveDay == _data.lastClaimDay + 1;
            _data.streak = consecutive ? _data.streak + 1 : 1;
            _data.bestStreak = Math.Max(_data.bestStreak, _data.streak);
            _data.lastClaimDay = _effectiveDay;
            _data.lastClaimIndex = TodayIndex;
            _data.totalClaims++;
            _dirty = true;
            Flush();
            Refresh();
            Changed?.Invoke();
            return ClaimResult.Claimed;
        }

        public bool Flush()
        {
            if (!_dirty)
            {
                return true;
            }

            if (!_repository.Save(_data))
            {
                return false;
            }

            _dirty = false;
            return true;
        }

        private void Set(int index, bool claimedToday, int streak)
        {
            TodayIndex = _days.Count == 0 ? 0 : Mathf.Clamp(index, 0, _days.Count - 1);
            ClaimedToday = claimedToday;
            Streak = streak;
        }

        private void Sanitize()
        {
            _data.schemaVersion = DailyRewardSaveData.CurrentSchemaVersion;
            _data.streak = Math.Max(0, _data.streak);
            _data.bestStreak = Math.Max(_data.bestStreak, _data.streak);
            _data.totalClaims = Math.Max(0, _data.totalClaims);
            if (_data.lastClaimDay < 0)
            {
                _data.lastClaimDay = -1;
                _data.lastClaimIndex = -1;
            }
            else
            {
                _data.lastClaimIndex = _days.Count == 0 ? 0 : Mathf.Clamp(_data.lastClaimIndex, 0, _days.Count - 1);
                _data.latestDay = Math.Max(_data.latestDay, _data.lastClaimDay);
            }
        }
    }
}
