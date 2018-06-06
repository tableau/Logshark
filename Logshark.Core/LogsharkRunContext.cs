using Logshark.Core.Controller.Initialization;
using Logshark.Core.Controller.Parsing;
using Logshark.Core.Controller.Plugin;
using Logshark.Core.Controller.Workbook;
using Logshark.Core.Exceptions;
using Logshark.RequestModel;
using System;
using System.Linq;
using System.Text;

namespace Logshark.Core
{
    public enum ProcessingPhase { Pending, Initializing, Parsing, ExecutingPlugins, Complete }

    /// <summary>
    /// Encapsulates the state of processing a logset from end-to-end.
    /// </summary>
    public class LogsharkRunContext
    {
        public string Id { get; protected set; }

        // The originating request associated with the current run.
        public LogsharkRequest Request { get; protected set; }

        // The current phase that the run is in.
        public ProcessingPhase CurrentPhase { get; set; }

        public RunInitializationResult InitializationResult { get; set; }

        public LogsetParsingResult ParsingResult { get; set; }

        public PluginExecutionResult PluginExecutionResult { get; set; }

        // Indicates whether a logset is valid and able to be parsed by Logshark.
        public bool? IsValidLogset { get; set; }

        // Indicates whether the Logshark run was successful or not.
        public bool? IsRunSuccessful { get; protected set; }

        // If the run failed, this indicates the exception type thrown when it failed.
        public string RunFailureExceptionType { get; protected set; }

        // If the run failed, this indicates the phase it was in when it failed.
        public ProcessingPhase? RunFailurePhase { get; protected set; }

        // If the run failed, this indicates the reason why.
        public string RunFailureReason { get; protected set; }

        public LogsharkRunContext(LogsharkRequest request)
        {
            Id = request.RunId;
            Request = request;
            CurrentPhase = ProcessingPhase.Pending;
        }

        public void SetRunSuccessful()
        {
            IsRunSuccessful = true;
        }

        public void SetRunFailed(Exception ex)
        {
            if (ex is InvalidLogsetException)
            {
                IsValidLogset = false;
            }

            IsRunSuccessful = false;
            RunFailureExceptionType = ex.GetType().FullName;
            RunFailurePhase = CurrentPhase;
            RunFailureReason = ex.Message;
        }

        public virtual string BuildRunSummary()
        {
            var summary = new StringBuilder();

            // Display logset hash, if relevant.
            if (InitializationResult != null && !String.IsNullOrWhiteSpace(InitializationResult.LogsetHash))
            {
                summary.AppendFormat("Logset hash for this run was '{0}'.", InitializationResult.LogsetHash);
            }

            // Display Postgres output location, if relevant.
            if (PluginExecutionResult != null)
            {
                int pluginSuccesses = PluginExecutionResult.PluginResponses.Count(pluginResponse => pluginResponse.SuccessfulExecution);
                if (pluginSuccesses > 0)
                {
                    summary.AppendLine();
                    summary.AppendFormat("Plugin backing data was written to Postgres database '{0}\\{1}'.", Request.Configuration.PostgresConnectionInfo, Request.PostgresDatabaseName);
                }

                // A plugin may run successfully, yet not output a workbook.  We only want to display the workbook output location if at least one workbook was output.
                int workbooksOutput = PluginExecutionResult.PluginResponses.Sum(pluginResponse => pluginResponse.WorkbooksOutput.Count);
                if (workbooksOutput > 0)
                {
                    summary.AppendLine();
                    summary.AppendFormat("Plugin workbook output was saved to '{0}'.", PluginExecutor.GetOutputLocation(Request.RunId));
                }

                // Display information about any published workbooks, if relevant.
                if (Request.PublishWorkbooks && pluginSuccesses > 0)
                {
                    var publishedWorkbookResults = PluginExecutionResult.PluginResponses
                                                                        .Where(response => response.WorkbooksPublished != null)
                                                                        .SelectMany(response => response.WorkbooksPublished)
                                                                        .ToList();
                    summary.AppendLine();
                    summary.AppendFormat(WorkbookPublisher.BuildPublishingSummary(publishedWorkbookResults));
                }
            }

            return summary.ToString();
        }
    }
}