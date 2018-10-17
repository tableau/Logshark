using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
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

        public string QpBatchSummaryEventGuid { get; set; }

        public List<VizqlQpBatchSummaryJob> QpBatchSummaryJobs { get; private set; }

        public VizqlQpBatchSummary() { }

        public VizqlQpBatchSummary(BsonDocument document)
        {
            SetEventMetadata(document);

            BsonDocument values = BsonDocumentHelper.GetValuesStruct(document);
            Elapsed = values.GetDouble("elapsed");
            ElapsedComputeKeys = values.GetNullableDouble("elapsed-compute-keys");
            ElapsedSum = values.GetDouble("elapsed-sum");
            JobCount = values.GetInt("job-count");

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