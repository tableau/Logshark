using System;
using System.Text.RegularExpressions;
using LogShark.Containers;
using LogShark.Extensions;
using LogShark.Shared;
using LogShark.Shared.Extensions;
using LogShark.Shared.LogReading.Containers;

namespace LogShark.Plugins.TabadminController
{
    public class TabadminAgentEventParser
    {
        private readonly IProcessingNotificationsCollector _processingNotificationsCollector;

        public TabadminAgentEventParser(IProcessingNotificationsCollector processingNotificationsCollector)
        {
            _processingNotificationsCollector = processingNotificationsCollector;
        }

        public TabadminControllerEvent ParseEvent(LogLine logLine, JavaLineMatchResult javaLineMatchResult)
        {
            if (string.IsNullOrWhiteSpace(javaLineMatchResult.Message))
            {
                _processingNotificationsCollector.ReportError($"Line does not appear to have a message", logLine, nameof(TabadminControllerEventParser));
                return null;
            }
            
            if (javaLineMatchResult.IsWarningPriorityOrHigher())
            {
                return new TabadminControllerEvent("Error - Tabadmin Agent", logLine, javaLineMatchResult);
            }

            if (javaLineMatchResult.Class.StartsWith("com.tableausoftware.tabadmin.agent.status.ServiceStatusRequestRunner"))
            {
                return ParseServiceStatus(logLine, javaLineMatchResult);
            }

            return null; // Line did not match any known events
        }

        private static readonly Regex MatchServiceStatusLine = new Regex(@"^Posting status update for (?<process>[a-z]+_\d+): (?<status>[A-Z_]+)((\.?)|(, detail message: (?<detail>.+)))$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private TabadminControllerEvent ParseServiceStatus(LogLine logLine, JavaLineMatchResult javaLineMatchResult)
        {
            var match = MatchServiceStatusLine.Match(javaLineMatchResult.Message);
            if (!match.Success)
            {
                return null;
            }

            var process = match.GetNullableString("process");
            var status = match.GetNullableString("status");
            var detail = match.GetNullableString("detail");

            if (string.IsNullOrWhiteSpace(process))
            {
                _processingNotificationsCollector.ReportError("Line looks a service status event, but process string cannot be parsed", logLine, nameof(TabadminControllerEventParser));
                return null;
            }

            if (string.IsNullOrWhiteSpace(status))
            {
                _processingNotificationsCollector.ReportError("Line looks a service status event, but status string cannot be parsed", logLine, nameof(TabadminControllerEventParser));
                return null;
            }

            return new TabadminControllerEvent("Process Status Update", logLine, javaLineMatchResult)
            {
                StatusProcess = process,
                StatusMessage = status,
                DetailMessage = detail,
            };
        }
    }
}