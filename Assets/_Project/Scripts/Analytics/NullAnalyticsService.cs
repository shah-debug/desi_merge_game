using System.Collections.Generic;

namespace DesiMerge.Analytics
{
    public sealed class NullAnalyticsService : IAnalyticsService
    {
        public void TrackEvent(string eventName, IReadOnlyDictionary<string, object> parameters = null)
        {
        }
    }
}
