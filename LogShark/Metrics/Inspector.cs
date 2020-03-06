using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace LogShark.Metrics
{
    public abstract class Inspector
    {
        protected ILogger _logger;

        protected T GetMetric<T>(Func<T> metricAction, [CallerMemberName] string callingMethod = null)
        {
            try
            {
                return metricAction();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Exception of type '{ex.GetType().Name}' occured during '{callingMethod}' in '{this.GetType().Name}'");
                return default(T);
            }
        }
    }
}
