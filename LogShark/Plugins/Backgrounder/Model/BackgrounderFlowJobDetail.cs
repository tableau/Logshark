using System;

namespace LogShark.Plugins.Backgrounder.Model
{
   public class BackgrounderFlowJobDetail
    {
 public string BackgrounderJobId { get; set; }
        public string jobName { get; set; }
        public string status { get; set; }
        public string flowId { get; set; }
        public DateTime? queuedTime { get; set; }
        public long? queueTimeSeconds { get; set; }
        public DateTime? startedTime { get; set; }
        public DateTime? endTime { get; set; }
        public long? totalTimeSeconds { get; set; }
        public long? runTimeSeconds { get; set; }
        public string runType { get; set; }

        public string flowName { get; set; }
        public string flowLuid { get; set; }

        public string outputStepIds { get; set; }

        public long? rowsGenerated { get; set; }

        public string flowRunMode { get; set; }

        public string flow_run_error_type { get; set; }

        public string tableau_error_code { get; set; }

        public string tableau_status_code { get; set; }

        public string tableau_service_name { get; set; }

        public string errors { get; set; }

        public string rootCauseErrorMsg { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
