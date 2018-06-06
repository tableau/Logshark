using System;
using System.Diagnostics;

namespace Logshark.Core.Helpers.Timers
{
    /// <summary>
    /// Helper class to maintain timing information around actions in Logshark.
    /// </summary>
    public class LogsharkTimer : IDisposable
    {
        protected readonly string eventName;
        protected readonly string eventDetail;
        protected readonly Action<EventTimingData> registrationCallback;

        protected readonly DateTime creationTime;
        protected readonly Stopwatch stopwatch;

        private bool disposed;

        public TimeSpan Elapsed { get { return stopwatch.Elapsed; } }

        public DateTime StartTime { get { return creationTime; } }

        public LogsharkTimer(string eventName, Action<EventTimingData> registrationCallback = null)
            : this(eventName, null, registrationCallback)
        {
        }

        public LogsharkTimer(string eventName, string eventDetail, Action<EventTimingData> registrationCallback = null)
        {
            this.eventName = eventName;
            this.eventDetail = eventDetail;
            this.registrationCallback = registrationCallback;

            creationTime = DateTime.UtcNow;
            stopwatch = Stopwatch.StartNew();
        }

        public EventTimingData Stop()
        {
            stopwatch.Stop();

            var eventTimingData = new EventTimingData(eventName, eventDetail, creationTime, stopwatch.Elapsed);

            if (registrationCallback != null)
            {
                registrationCallback.Invoke(eventTimingData);
            }

            return eventTimingData;
        }

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (stopwatch.IsRunning)
                    {
                        Stop();
                    }
                }

                disposed = true;
            }
        }

        #endregion IDisposable Implementation
    }
}