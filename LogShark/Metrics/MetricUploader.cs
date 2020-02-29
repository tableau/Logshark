using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Flurl.Http;
using LogShark.Metrics.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace LogShark.Metrics
{
    public class MetricUploader : IMetricUploader
    {
        private LogSharkConfiguration _config;
        private readonly string _correlationId;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly ILogger _logger;

        public MetricUploader(LogSharkConfiguration config, ILoggerFactory loggerFactory)
        {
            _config = config;
            _correlationId = Guid.NewGuid().ToString();
            _logger = loggerFactory.CreateLogger<MetricUploader>();
            _jsonSerializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
            };
        }

        public async Task Upload(object metricsBody, string eventType)
        {
            try
            {
                var payload = new MetricsMessage
                {
                    Application = _config.TelemetryApplication,
                    Body = metricsBody,
                    CorrelationId = _correlationId,
                    Environment = _config.TelemetryEnvironment,
                    EventType = eventType,
                };
                
                var serializedPayload = JsonConvert.SerializeObject(payload, _jsonSerializerSettings);

                var response = await Retry.DoWithRetries<FlurlHttpException, HttpResponseMessage>(nameof(MetricUploader), _logger, async () =>
                     await _config.TelemetryEndpoint
                        .WithTimeout(_config.TelemetryTimeout)
                        .PostAsync(new StringContent(serializedPayload, Encoding.UTF8, "application/json")));

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Successful status code not returned from telemetry upload.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception occurred during telemetry upload.");
            }
        }
    }
}
