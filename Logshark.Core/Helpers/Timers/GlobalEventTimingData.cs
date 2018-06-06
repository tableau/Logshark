using System;
using System.Collections.Generic;
using System.Linq;

namespace Logshark.Core.Helpers.Timers
{
    internal static class GlobalEventTimingData
    {
        private static readonly Lazy<EventTimingDataCollection> lazy = new Lazy<EventTimingDataCollection>(() => new EventTimingDataCollection());

        public static EventTimingDataCollection Instance { get { return lazy.Value; } }

        public static void Add(EventTimingData datum)
        {
            Instance.Add(datum);
        }

        public static void Clear()
        {
            Instance.Clear();
        }

        public static double? GetElapsedTime(string eventKey, string eventDetail = null)
        {
            var eventTimingData = Search(eventKey, eventDetail).FirstOrDefault();
            if (eventTimingData == null)
            {
                return null;
            }

            return eventTimingData.ElapsedSeconds;
        }

        public static DateTime? GetStartTime(string eventKey, string eventDetail = null)
        {
            var eventTimingData = Search(eventKey, eventDetail).FirstOrDefault();
            if (eventTimingData == null)
            {
                return null;
            }

            return eventTimingData.StartTime;
        }

        public static IEnumerable<EventTimingData> Search(string eventKey, string eventDetail = null)
        {
            if (eventDetail == null)
            {
                return Instance.GetEventTimingData(eventKey);
            }
            else
            {
                return Instance.GetEventTimingData(eventKey).Where(item => item.Detail == eventDetail);
            }
        }
    }
}