namespace LogShark.Metrics
{
    public class MetricsConfig
    {
        public IMetricUploader MetricUploader { get; }
        public TelemetryLevel TelemetryLevel { get; }

        public MetricsConfig(IMetricUploader metricUploader, LogSharkConfiguration config) : this(metricUploader, config.TelemetryLevel) { }

        public MetricsConfig(IMetricUploader metricUploader, TelemetryLevel telemetryLevel)
        {
            MetricUploader = metricUploader;
            TelemetryLevel = telemetryLevel;
        }
    }
}