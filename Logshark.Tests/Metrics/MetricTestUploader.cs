using LogShark.Metrics;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LogShark.Tests.Metrics
{
    public class MetricTestUploader : InvariantCultureTestsBase, IMetricUploader
    {
        public List<object> UploadedPayloads { get; private set; } = new List<object>();

        public int UploadCallCount => UploadedPayloads.Count;

        public Task Upload(object metricsBody, string eventType)
        {
            UploadedPayloads.Add(metricsBody);
            return Task.CompletedTask;
        }
    }
}
