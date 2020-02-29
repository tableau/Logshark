using System;
using System.Collections.Generic;
using System.Text;

namespace LogShark.Metrics.Models
{
    public class StartMetrics
    {
        public ContextModel Context { get; set; }
        public SystemModel System { get; set; }

        public class ContextModel
        {
            public string RequestedPlugins { get; set; }
            public string RequestedWriter { get; set; }
            public string UserProvidedRunId { get; set; }
        }

        public class SystemModel
        {
            public bool? DebuggerIsAttached { get; set; }
            public string DomainName { get; set; }
            public string LogSharkVersion { get; set; }
            public string MachineName { get; set; }
            public string OSArchitecture { get; set; }
            public string OSDescription { get; set; }
            public string OSVersion { get; set; }
            public int? ProcessorCount { get; set; }
            public TelemetryLevel TelemetryLevel { get; set; }
            public string Username { get; set; }
        }
    }
}
