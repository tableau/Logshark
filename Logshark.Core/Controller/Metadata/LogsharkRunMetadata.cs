using Logshark.ConnectionModel.TableauServer;
using Logshark.Core.Controller.Initialization;
using Logshark.Core.Controller.Plugin;
using Logshark.Core.Helpers.Timers;
using Logshark.PluginModel.Model;
using Logshark.RequestModel;
using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Logshark.Core.Controller.Metadata
{
    /// <summary>
    /// Encapsulates metadata about a Logshark run.
    /// </summary>
    internal sealed class LogsharkRunMetadata
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

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

        [Index]
        public bool IsRunComplete { get; set; }

        [Index]
        public bool? IsRunSuccessful { get; set; }

        [Index]
        public bool? IsValidLogset { get; set; }

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

        public long? TargetProcessedSize { get; set; }

        public long? TargetSize { get; set; }

        public bool UtilizedExistingLogsetHash { get; set; }

        public string VersionLogshark { get; set; }

        public LogsharkRunMetadata()
        {
        }

        public LogsharkRunMetadata(LogsharkRunContext run, int? id = null)
        {
            // Update record id if it was passed in.
            if (id.HasValue)
            {
                Id = id.Value;
            }

            // Request & target metadata.
            CustomId = run.Request.CustomId;
            DatabaseName = run.Request.PostgresDatabaseName;
            RunId = run.Request.RunId;
            RunByUser = Environment.UserName;
            RunByMachine = Environment.MachineName;
            Source = run.Request.Source;
            Target = run.Request.Target;
            TargetSize = run.Request.Target.Size;
            VersionLogshark = typeof(LogsharkRequestProcessor).Assembly.GetName().Version.ToString();

            // Timing metadata.
            FullRunStartTime = run.Request.RequestCreationDate;
            FullRunElapsedSeconds = (DateTime.UtcNow - run.Request.RequestCreationDate).TotalSeconds;
            LastMetadataUpdateTime = DateTime.UtcNow;
            LogsetExtractionStartTime = GlobalEventTimingData.GetStartTime("Unpack Archives");
            LogsetExtractionElapsedSeconds = GlobalEventTimingData.GetElapsedTime("Unpack Archives");
            LogParsingStartTime = GlobalEventTimingData.GetStartTime("Parsed Files");
            LogParsingElapsedSeconds = GlobalEventTimingData.GetElapsedTime("Parsed Files");
            PluginExecutionStartTime = GlobalEventTimingData.GetStartTime("Executed Plugins");
            PluginExecutionElapsedSeconds = GlobalEventTimingData.GetElapsedTime("Executed Plugins");

            // Context metadata.
            CurrentProcessingPhase = run.CurrentPhase.ToString();
            CustomMetadataRecords = GetCustomMetadataRecords(run.Request);
            IsValidLogset = run.IsValidLogset;

            // Initialization metadata.
            if (run.InitializationResult != null)
            {
                LogsetHash = run.InitializationResult.LogsetHash;
                LogsetType = run.InitializationResult.ArtifactProcessor.ArtifactType;
                PluginsExecuted = GetExecutedPluginsString(run.InitializationResult);
            }

            // Parsing metadata.
            if (run.ParsingResult != null)
            {
                TargetProcessedSize = run.ParsingResult.ParsedDataVolumeBytes;
                UtilizedExistingLogsetHash = run.ParsingResult.UtilizedExistingProcessedLogset;
            }

            // Plugin execution metadata.
            if (run.PluginExecutionResult != null)
            {
                ContainsSuccessfulPluginExecution = run.PluginExecutionResult.PluginResponses.Any(pluginResponse => pluginResponse.SuccessfulExecution);
                PluginExecutionMetadataRecords = GetPluginExecutionMetadataRecords(run.PluginExecutionResult);
                PluginsFailed = String.Join(",", run.PluginExecutionResult.PluginResponses.Where(pluginResponse => !pluginResponse.SuccessfulExecution));
                PublishedWorkbookMetadataRecords = GetPublishedWorkbookMetadataRecords(run.PluginExecutionResult, run.Request.Configuration.TableauConnectionInfo);
            }

            // Outcome metadata.
            IsRunSuccessful = run.IsRunSuccessful;
            if (run.CurrentPhase == ProcessingPhase.Complete)
            {
                IsRunComplete = true;
            }
            RunFailureExceptionType = run.RunFailureExceptionType;
            if (run.RunFailurePhase.HasValue)
            {
                RunFailurePhase = run.RunFailurePhase.ToString();
            }
            RunFailureReason = run.RunFailureReason;
        }

        private IEnumerable<LogsharkCustomMetadata> GetCustomMetadataRecords(LogsharkRequest request)
        {
            return request.Metadata.Select(customMetadataItem => new LogsharkCustomMetadata(customMetadataItem, this)).ToList();
        }

        private string GetExecutedPluginsString(RunInitializationResult runInitializationResult)
        {
            ICollection<string> executedPlugins = GetExecutedPlugins(runInitializationResult);

            if (!executedPlugins.Any())
            {
                return null;
            }

            return String.Join(",", executedPlugins);
        }

        private ISet<string> GetExecutedPlugins(RunInitializationResult runInitializationResult)
        {
            ISet<string> executedPlugins = new SortedSet<string>();

            foreach (Type pluginType in runInitializationResult.PluginTypesToExecute)
            {
                executedPlugins.Add(pluginType.Name);
            }

            return executedPlugins;
        }

        private IEnumerable<LogsharkPluginExecutionMetadata> GetPluginExecutionMetadataRecords(PluginExecutionResult pluginExecutionResult)
        {
            var pluginExecutionMetadataRecords = new List<LogsharkPluginExecutionMetadata>();

            foreach (IPluginResponse pluginResponse in pluginExecutionResult.PluginResponses)
            {
                var pluginExecutionMetadataRecord = new LogsharkPluginExecutionMetadata(pluginResponse, this, GetPluginExecutionTimestamp(pluginResponse.PluginName), GetPluginVersion(pluginExecutionResult, pluginResponse.PluginName));
                pluginExecutionMetadataRecords.Add(pluginExecutionMetadataRecord);
            }

            return pluginExecutionMetadataRecords;
        }

        private IEnumerable<LogsharkPublishedWorkbookMetadata> GetPublishedWorkbookMetadataRecords(PluginExecutionResult pluginExecutionResult, TableauServerConnectionInfo tableauConnectionInfo)
        {
            var publishedWorkbookMetadataRecords = new List<LogsharkPublishedWorkbookMetadata>();

            foreach (IPluginResponse pluginResponse in pluginExecutionResult.PluginResponses.Where(pluginResponse => pluginResponse.WorkbooksPublished != null))
            {
                publishedWorkbookMetadataRecords.AddRange(pluginResponse.WorkbooksPublished.Select(publishedWorkbook => new LogsharkPublishedWorkbookMetadata(publishedWorkbook, this, tableauConnectionInfo)));
            }

            return publishedWorkbookMetadataRecords;
        }

        private DateTime GetPluginExecutionTimestamp(string pluginName)
        {
            var timeStamp = GlobalEventTimingData.GetStartTime("Executed Plugin", pluginName);
            if (timeStamp == null)
            {
                return default(DateTime);
            }

            return timeStamp.Value;
        }

        private string GetPluginVersion(PluginExecutionResult pluginExecutionResult, string pluginName)
        {
            foreach (Type plugin in pluginExecutionResult.PluginsExecuted)
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