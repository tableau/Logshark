using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Logshark.Core.Helpers.Timers
{
    public class EventTimingDataCollection : IEnumerable<KeyValuePair<string, ConcurrentBag<EventTimingData>>>
    {
        protected ConcurrentDictionary<string, ConcurrentBag<EventTimingData>> timingData;

        public IEnumerable<string> Keys { get { return timingData.Keys; } }

        public EventTimingDataCollection()
        {
            timingData = new ConcurrentDictionary<string, ConcurrentBag<EventTimingData>>();
        }

        public void Add(EventTimingData datum)
        {
            timingData.AddOrUpdate(datum.Event,
                                   new ConcurrentBag<EventTimingData> { datum },
                                   (existingKey, existingValue) => { existingValue.Add(datum); return existingValue; });
        }

        public void Add(IEnumerable<EventTimingData> data)
        {
            foreach (var datum in data)
            {
                Add(datum);
            }
        }

        public void Clear()
        {
            timingData.Clear();
        }

        public IEnumerable<EventTimingData> GetEventTimingData(string eventKey)
        {
            ConcurrentBag<EventTimingData> value;
            timingData.TryGetValue(eventKey, out value);

            if (value == null)
            {
                return new List<EventTimingData>();
            }

            return value;
        }

        #region IEnumerable Implementation

        public IEnumerator<KeyValuePair<string, ConcurrentBag<EventTimingData>>> GetEnumerator()
        {
            return timingData.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion IEnumerable Implementation
    }
}