using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;

namespace Logshark.Plugins.Vizql.Models.Events.Query
{
    public class VizqlQpBatchSummary : VizqlEvent
    {
        public int JobCount { get; set; }
        public double Elapsed { get; set; }
        public double? ElapsedComputeKeys { get; set; }
        public double ElapsedSum { get; set; }

        [Index(Unique = true)]
        public string QpBatchSummaryEventGuid { get; set; }

        [Ignore]
        public List<VizqlQpBatchSummaryJob> QpBatchSummaryJobs { get; private set; }

        public VizqlQpBatchSummary()
        {
        }

        public VizqlQpBatchSummary(BsonDocument document)
        {
            ValidateArguments("qp-batch-summary", document);
            SetEventMetadata(document);

            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);
            Elapsed = BsonDocumentHelper.GetDouble("elapsed", values);
            ElapsedComputeKeys = BsonDocumentHelper.GetNullableDouble("elapsed-compute-keys", values);
            ElapsedSum = BsonDocumentHelper.GetDouble("elapsed-sum", values);
            JobCount = BsonDocumentHelper.GetInt("job-count", values);

            QpBatchSummaryEventGuid = GetQpBatchSummaryGuid();

            QpBatchSummaryJobs = new List<VizqlQpBatchSummaryJob>();
            BsonArray jobs = values.GetValue("jobs").AsBsonArray;
            foreach (BsonDocument job in jobs)
            {
                QpBatchSummaryJobs.Add(new VizqlQpBatchSummaryJob(QpBatchSummaryEventGuid, VizqlSessionId, job));
            }
        }

        private string GetQpBatchSummaryGuid()
        {
            return Guid.NewGuid().ToString("N");
        }

        public override double? GetElapsedTimeInSeconds()
        {
            return Elapsed;
        }
    }
}