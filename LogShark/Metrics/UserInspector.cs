using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace LogShark.Metrics
{
    public class UserInspector : Inspector
    {
        private TelemetryLevel _telemetryLevel;

        public UserInspector(ILoggerFactory loggerFactory, TelemetryLevel telemetryLevel)
        {
            _logger = loggerFactory.CreateLogger<UserInspector>();
            _telemetryLevel = telemetryLevel;
        }

        public string GetUsername()
        {
            return GetMetric(() => _telemetryLevel == TelemetryLevel.Full ? Environment.UserName : null);
        }

        public string GetDomainName()
        {
            return GetMetric(() => _telemetryLevel == TelemetryLevel.Full ? Environment.UserDomainName : null);
        }

        public string GetMachineName()
        {
            return GetMetric(() => _telemetryLevel == TelemetryLevel.Full ? Environment.MachineName : null);
        }
    }
}
