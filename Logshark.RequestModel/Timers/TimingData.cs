using System;

namespace Logshark.RequestModel.Timers
{
    /// <summary>
    /// Encapsulates timing data about an event.
    /// </summary>
    public class TimingData
    {
        public string Event { get; protected set; }
        public string Detail { get; protected set; }
        public DateTime StartTime { get; protected set; }
        public double ElapsedSeconds { get; protected set; }
        protected TimeSpan Elapsed { get; set; }

        public TimingData(string eventName, string detail, DateTime startTime, TimeSpan timeTaken)
        {
            Event = eventName;
            Detail = detail;
            StartTime = startTime;
            Elapsed = timeTaken;
            ElapsedSeconds = Math.Round(Elapsed.TotalSeconds, 3);
        }

        public override string ToString()
        {
            if (String.IsNullOrWhiteSpace(Detail))
            {
                return String.Format("{0}: {1}", Event, ElapsedSeconds.ToString("0.00"));
            }
            else
            {
                return String.Format("{0} - {1}: {2}", Event, Detail, ElapsedSeconds.ToString("0.00"));
            }
        }
    }
}