using System;

namespace DesiMerge.Core
{
    /// <summary>Source of the current time. Injected so date-based logic (daily challenges, streaks) is testable.</summary>
    public interface IClock
    {
        DateTime UtcNow { get; }
    }

    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    /// <summary>A clock that only moves when told to. For tests and for reproducing date issues.</summary>
    public sealed class ManualClock : IClock
    {
        public ManualClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; set; }

        public void AdvanceDays(double days) => UtcNow = UtcNow.AddDays(days);
    }

    /// <summary>
    /// Calendar day numbers used for daily challenges and streaks: whole days since 1970-01-01 in <b>UTC</b>.
    /// The day therefore changes at 00:00 UTC (05:00 in Pakistan) for every player, regardless of time-zone or
    /// daylight-saving changes on the device. This is not tamper-proof: a device clock moved forward is believed.
    /// </summary>
    public static class DayKey
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static int FromUtc(DateTime utc)
        {
            if (utc.Kind == DateTimeKind.Local)
            {
                utc = utc.ToUniversalTime();
            }

            return (int)Math.Floor((utc - Epoch).TotalDays);
        }

        public static int Today(IClock clock) => FromUtc(clock.UtcNow);

        /// <summary>How far the device day may be behind the latest day seen before that latest day is treated as a clock mistake.</summary>
        public const int ClockToleranceDays = 7;

        /// <summary>
        /// The day daily systems use: never earlier than the latest day seen (so moving the clock back a little can't
        /// repeat a day), unless the device is more than <see cref="ClockToleranceDays"/> behind it. Then the latest day
        /// was almost certainly a wrong future date and the device day is used, so the game can't stay locked for years.
        /// </summary>
        public static int Effective(int today, int latestDay)
        {
            if (latestDay < 0 || today >= latestDay)
            {
                return today;
            }

            return latestDay - today > ClockToleranceDays ? today : latestDay;
        }

        /// <summary>Week number for a day: weeks start on Monday 00:00 UTC (day 0, 1970-01-01, was a Thursday).</summary>
        public static int WeekOf(int day) => (int)Math.Floor((day + 3) / 7.0);
    }
}
