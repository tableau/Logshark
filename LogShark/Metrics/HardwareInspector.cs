using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace LogShark.Metrics
{
    public class HardwareInspector : Inspector
    {
        public HardwareInspector(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HardwareInspector>();
        }

        public string GetOSArchitecture()
        {
            return GetMetric(() => RuntimeInformation.OSArchitecture.ToString());
        }

        public string GetOSDescription()
        {
            return GetMetric(() => RuntimeInformation.OSDescription);
        }

        public string GetOSVersion()
        {
            return GetMetric(() => Environment.Version.ToString());            
        }

        public int? GetProcessorCount()
        {
            return GetMetric(() => Environment.ProcessorCount);
        }
    }
}
