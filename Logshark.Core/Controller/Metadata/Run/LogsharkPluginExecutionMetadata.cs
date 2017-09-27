using Logshark.PluginModel.Model;
using ServiceStack.DataAnnotations;
using System;

namespace Logshark.Core.Controller.Metadata.Run
{
    public class LogsharkPluginExecutionMetadata
    {
        private readonly LogsharkRunMetadata runMetadata;

        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; set; }

        [Index]
        [References(typeof(LogsharkRunMetadata))]
        public int LogsharkRunMetadataId
        {
            get { return runMetadata.Id; }
        }

        [Index]
        public string PluginName { get; set; }

        public string PluginVersion { get; set; }

        [Index]
        public bool IsSuccessful { get; set; }

        public string FailureReason { get; set; }

        public DateTime Timestamp { get; set; }

        public double ElapsedSeconds { get; set; }

        public LogsharkPluginExecutionMetadata()
        {
        }

        public LogsharkPluginExecutionMetadata(IPluginResponse pluginResponse, LogsharkRunMetadata runMetadata, DateTime timestamp, string pluginVersion)
        {
            this.runMetadata = runMetadata;
            PluginVersion = pluginVersion;
            PluginName = pluginResponse.PluginName;
            IsSuccessful = pluginResponse.SuccessfulExecution;
            FailureReason = pluginResponse.FailureReason;
            Timestamp = timestamp;
            ElapsedSeconds = pluginResponse.PluginRunTime.TotalSeconds;
        }
    }
}