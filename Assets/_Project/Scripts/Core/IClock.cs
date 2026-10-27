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
    }
}
