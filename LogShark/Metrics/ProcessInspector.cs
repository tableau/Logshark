using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;

namespace LogShark.Metrics
{
    public class ProcessInspector : Inspector
    {
        public ProcessInspector(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ProcessInspector>();
        }

        public long? GetPeakWorkingSet()
        {
            return GetMetric(() => Process.GetCurrentProcess().PeakWorkingSet64);
        }

        public string GetLogSharkVersion()
        {
            return GetMetric(() => Assembly.GetExecutingAssembly().GetName().Version.ToString());
        }
    }
}
