namespace LogShark.Plugins.ServerTelemetry
{
    public class ServerTelemetryMetric
    {
        public string MetricName { get; }
        public string RequestId { get; }
        public string SessionId { get; }
        
        public int Count { get; }
        public double MaxSeconds { get; }
        public double MinSeconds { get; }
        public double TotalTimeSeconds { get; }

        public ServerTelemetryMetric(
            string metricName, 
            string requestId, 
            string sessionId, 
            int count, 
            double maxSeconds, 
            double minSeconds,
            double totalTimeSeconds
            )
        {
            MetricName = metricName;
            RequestId = requestId;
            SessionId = sessionId;
            Count = count;
            MaxSeconds = maxSeconds;
            MinSeconds = minSeconds;
            TotalTimeSeconds = totalTimeSeconds;
        }
    }
}