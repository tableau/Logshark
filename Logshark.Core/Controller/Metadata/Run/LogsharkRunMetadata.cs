using Logshark.PluginModel.Model;
using Logshark.RequestModel;
using Logshark.RequestModel.RunContext;
using Logshark.RequestModel.Timers;
using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Logshark.Core.Controller.Metadata.Run
{
    /// <summary>
    /// Encapsulates metadata about a Logshark run.
    /// </summary>
    public sealed class LogsharkRunMetadata
    {
        public bool ContainsSuccessfulPluginExecution { get; set; }

        [Index]
        public string CurrentProcessingPhase { get; set; }

        public string CustomId { get; set; }

        [Ignore]
        public IEnumerable<LogsharkCustomMetadata> CustomMetadataRecords { get; set; }

        public string DatabaseName { get; set; }

        public double FullRunElapsedSeconds { get; set; }

        [Index]
        public DateTime FullRunStartTime { get; set; }

        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        [Index]
        public bool IsRunComplete { get; set; }

        [Index]
        public bool? IsRunSuccessful { get; set; }

        [Index]
        public bool? IsValidLogset { get; set; }

        // Timing Info
        public DateTime? LastMetadataUpdateTime { get; set; }

        public double? LogParsingElapsedSeconds { get; set; }

        public DateTime? LogParsingStartTime { get; set; }

        public double? LogsetExtractionElapsedSeconds { get; set; }

        public DateTime? LogsetExtractionStartTime { get; set; }

        public string LogsetHash { get; set; }

        public string LogsetType { get; set; }

        public double? PluginExecutionElapsedSeconds { get; set; }

        [Ignore]
        public IEnumerable<LogsharkPluginExecutionMetadata> PluginExecutionMetadataRecords { get; set; }

        public DateTime? PluginExecutionStartTime { get; set; }

        public string PluginsExecuted { get; set; }

        public string PluginsFailed { get; set; }

        [Ignore]
        public IEnumerable<LogsharkPublishedWorkbookMetadata> PublishedWorkbookMetadataRecords { get; set; }

        public string RunByMachine { get; set; }

        [Index]
        public string RunByUser { get; set; }

        public string RunFailureExceptionType { get; set; }

        public string RunFailurePhase { get; set; }

        public string RunFailureReason { get; set; }

        [Index(Unique = true)]
        public string RunId { get; set; }

        [Index]
        public string Source { get; set; }

        public string Target { get; set; }

        public long? TargetCompressedSize { get; set; }

        public long? TargetProcessedSize { get; set; }

        public long? TargetSize { get; set; }

        public bool UtilizedExistingLogsetHash { get; set; }

        public string VersionLogshark { get; set; }

        public LogsharkRunMetadata()
        {
        }

        public LogsharkRunMetadata(LogsharkRequest request)
        {
            // Request & target data.
            CustomId = request.CustomId;
            DatabaseName = request.PostgresDatabaseName;
            RunId = request.RunId;
            RunByUser = Environment.UserName;
            RunByMachine = Environment.MachineName;
            Source = request.Source;
            Target = request.Target.OriginalTarget;
            TargetSize = request.Target.UncompressedSize;
            TargetCompressedSize = request.Target.CompressedSize;
            TargetProcessedSize = request.Target.ProcessedSize;
            VersionLogshark = typeof(LogsharkController).Assembly.GetName().Version.ToString();

            // Timing data.
            FullRunStartTime = request.RequestCreationDate;
            FullRunElapsedSeconds = (DateTime.UtcNow - request.RequestCreationDate).TotalSeconds;
            LastMetadataUpdateTime = DateTime.UtcNow;
            LogsetExtractionStartTime = request.RunContext.GetStartTime("Unpack Logset");
            LogsetExtractionElapsedSeconds = request.RunContext.GetElapsedTime("Unpack Logset");
            LogParsingStartTime = request.RunContext.GetStartTime("Parsed Files");
            LogParsingElapsedSeconds = request.RunContext.GetElapsedTime("Parsed Files");
            PluginExecutionStartTime = request.RunContext.GetStartTime("Executed Plugins");
            PluginExecutionElapsedSeconds = request.RunContext.GetElapsedTime("Executed Plugins");

            // Context data.
            if (request.RunContext.MetadataRecordId.HasValue)
            {
                Id = request.RunContext.MetadataRecordId.Value;
            }
            ContainsSuccessfulPluginExecution = request.RunContext.PluginResponses.Any(pluginResponse => pluginResponse.SuccessfulExecution);
            CurrentProcessingPhase = request.RunContext.CurrentPhase.ToString();
            CustomMetadataRecords = GetCustomMetadataRecords(request);
            IsRunSuccessful = request.RunContext.IsRunSuccessful;
            if (request.RunContext.CurrentPhase == ProcessingPhase.Complete)
            {
                IsRunComplete = true;
            }
            IsValidLogset = request.RunContext.IsValidLogset;
            LogsetHash = request.RunContext.LogsetHash;
            LogsetType = request.RunContext.LogsetType;
            PluginExecutionMetadataRecords = GetPluginExecutionMetadataRecords(request);
            PluginsExecuted = GetExecutedPluginsString(request);
            PluginsFailed = String.Join(",", request.RunContext.PluginResponses.Where(pluginResponse => !pluginResponse.SuccessfulExecution));
            PublishedWorkbookMetadataRecords = request.RunContext.PublishedWorkbooks.Select(publishedWorkbook => new LogsharkPublishedWorkbookMetadata(request, this, publishedWorkbook));
            RunFailureExceptionType = request.RunContext.RunFailureExceptionType;
            if (request.RunContext.RunFailurePhase.HasValue)
            {
                RunFailurePhase = request.RunContext.RunFailurePhase.ToString();
            }
            RunFailureReason = request.RunContext.RunFailureReason;
            UtilizedExistingLogsetHash = request.RunContext.UtilizedExistingProcessedLogset;
        }

        private IEnumerable<LogsharkCustomMetadata> GetCustomMetadataRecords(LogsharkRequest request)
        {
            return request.Metadata.Select(customMetadataItem => new LogsharkCustomMetadata(customMetadataItem, this)).ToList();
        }

        private ISet<string> GetExecutedPlugins(LogsharkRequest request)
        {
            if (request.RunContext.PluginTypesToExecute.Count == 0)
            {
                return null;
            }

            ISet<string> executedPlugins = new SortedSet<string>();
            foreach (Type pluginType in request.RunContext.PluginTypesToExecute)
            {
                executedPlugins.Add(pluginType.Name);
            }

            return executedPlugins;
        }

        private string GetExecutedPluginsString(LogsharkRequest request)
        {
            ICollection<string> executedPlugins = GetExecutedPlugins(request);
            if (executedPlugins == null || executedPlugins.Count == 0)
            {
                return null;
            }

            return String.Join(",", executedPlugins);
        }

        private IEnumerable<LogsharkPluginExecutionMetadata> GetPluginExecutionMetadataRecords(LogsharkRequest request)
        {
            ICollection<LogsharkPluginExecutionMetadata> pluginExecutionMetadataRecords = new List<LogsharkPluginExecutionMetadata>();

            foreach (IPluginResponse pluginResponse in request.RunContext.PluginResponses)
            {
                var pluginExecutionMetadataRecord = new LogsharkPluginExecutionMetadata(pluginResponse, this, GetPluginExecutionTimestamp(request, pluginResponse.PluginName), GetPluginVersion(request, pluginResponse.PluginName));
                pluginExecutionMetadataRecords.Add(pluginExecutionMetadataRecord);
            }

            return pluginExecutionMetadataRecords;
        }

        private DateTime GetPluginExecutionTimestamp(LogsharkRequest request, string pluginName)
        {
            IDictionary<string, TimingData> pluginExecutionTimings = request.RunContext.TimingData.Where(item => item.Event == "Executed Plugin").ToDictionary(item => item.Detail, item => item);

            return pluginExecutionTimings[pluginName].StartTime;
        }

        private string GetPluginVersion(LogsharkRequest request, string pluginName)
        {
            foreach (Type plugin in request.RunContext.PluginTypesToExecute)
            {
                if (plugin.Name.Equals(pluginName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return plugin.Assembly.GetName().Version.ToString();
                }
            }

            return null;
        }
    }
}