using Logshark.Controller;
using System;
using System.Diagnostics;

namespace Logshark.Helpers
{
    /// <summary>
    /// Helper class to maintain timing information around actions in Logshark.
    /// </summary>
    public class LogsharkTimer
    {
        protected readonly LogsharkRunContext logsharkRunState;
        protected readonly string eventName;
        protected readonly string eventDetail;
        protected readonly DateTime creationTime;
        protected readonly Stopwatch stopwatch;

        public TimeSpan Elapsed
        {
            get
            {
                return stopwatch.Elapsed;
            }
        }

        public LogsharkTimer(LogsharkRunContext logsharkRunState, string eventName, string eventDetail)
        {
            this.logsharkRunState = logsharkRunState;
            this.eventName = eventName;
            this.eventDetail = eventDetail;
            creationTime = DateTime.UtcNow;
            stopwatch = Stopwatch.StartNew();
        }

        public void Stop()
        {
            stopwatch.Stop();
            TimingData timingData = new TimingData(eventName, eventDetail, creationTime, stopwatch.Elapsed);
            logsharkRunState.AddTimingData(timingData);
        }
    }
}