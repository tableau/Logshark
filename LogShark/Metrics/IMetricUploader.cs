using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LogShark.Metrics
{
    public interface IMetricUploader
    {
        Task Upload(object metricsBody, string eventType);
    }
}
