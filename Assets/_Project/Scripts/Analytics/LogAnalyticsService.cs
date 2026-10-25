using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DesiMerge.Analytics
{
    /// <summary>Development-only analytics that writes events to the Unity console.</summary>
    public sealed class LogAnalyticsService : IAnalyticsService
    {
        public void TrackEvent(string eventName, IReadOnlyDictionary<string, object> parameters = null)
        {
            var builder = new StringBuilder("[Analytics] ").Append(eventName);
            if (parameters != null)
            {
                foreach (var pair in parameters)
                {
                    builder.Append(' ').Append(pair.Key).Append('=').Append(pair.Value);
                }
            }

            Debug.Log(builder.ToString());
        }
    }
}
