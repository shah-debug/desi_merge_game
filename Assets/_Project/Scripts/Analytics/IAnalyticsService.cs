using System.Collections.Generic;

namespace DesiMerge.Analytics
{
    /// <summary>
    /// Records gameplay events. Must never block or throw into gameplay code.
    /// No implementation may send data over the network in the offline-first phases.
    /// </summary>
    public interface IAnalyticsService
    {
        void TrackEvent(string eventName, IReadOnlyDictionary<string, object> parameters = null);
    }
}
