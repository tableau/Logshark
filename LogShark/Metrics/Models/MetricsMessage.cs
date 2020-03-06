namespace LogShark.Metrics.Models
{
    public class MetricsMessage
    {
        public string Application { get; set; }
        public string CorrelationId { get; set; }
        public string Environment { get; set; }
        public string EventType { get; set; }
        public object Body { get; set; }
    }
}