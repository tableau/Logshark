using Logshark.PluginModel.Model;
using System;
using System.Collections.Generic;
using Tableau.RestApi.Model;

namespace Logshark.PluginLib.Model.Impl
{
    public class PluginResponse : IPluginResponse
    {
        public string PluginName { get; private set; }
        public bool SuccessfulExecution { get; protected set; }
        public string FailureReason { get; protected set; }
        public bool GeneratedNoData { get; set; }
        public ICollection<string> WorkbooksOutput { get; private set; }
        public ICollection<PublishedWorkbookResult> WorkbooksPublished { get; private set; }
        public TimeSpan PluginRunTime { get; set; }

        public PluginResponse(string pluginName)
        {
            PluginName = pluginName;
            SuccessfulExecution = true;
            WorkbooksOutput = new List<string>();
            WorkbooksPublished = new List<PublishedWorkbookResult>();
        }

        public void SetExecutionOutcome(bool isSuccessful, string failureReason = null)
        {
            SuccessfulExecution = isSuccessful;
            if (!isSuccessful && !String.IsNullOrWhiteSpace(failureReason))
            {
                FailureReason = failureReason;
            }
        }

        public override string ToString()
        {
            return PluginName;
        }
    }
}