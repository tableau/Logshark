namespace LogShark.Metrics
{
    public class MetricsUploaderConfiguration
    {
        public string Application { get; }
        public string EndpointUrl { get; }
        public string Environment { get; }
        public int UploadTimeout { get; }

        public MetricsUploaderConfiguration(string application, string endpointUrl, string environment, int uploadTimeout)
        {
            Application = application;
            EndpointUrl = endpointUrl;
            Environment = environment;
            UploadTimeout = uploadTimeout;
        }

        public MetricsUploaderConfiguration(LogSharkConfiguration logSharkConfiguration) : this(
            logSharkConfiguration.TelemetryApplication,
            logSharkConfiguration.TelemetryEndpoint,
            logSharkConfiguration.TelemetryEnvironment,
            logSharkConfiguration.TelemetryTimeout)
        {
        }
    }
}