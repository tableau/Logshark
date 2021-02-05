using System.Collections.Generic;
using LogShark.Containers;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;

namespace LogShark
{
    public class ProcessingNotificationsCollector : IProcessingNotificationsCollector
    {
        public int MaxErrorsWithDetails { get; }

        public IDictionary<string, int> ErrorCountByReporter { get; }
        public IList<ProcessingNotification> ProcessingErrorsDetails { get; }
        public int TotalErrorsReported { get; private set; }

        public IDictionary<string, int> WarningCountByReporter { get; }
        public IList<ProcessingNotification> ProcessingWarningsDetails { get; }
        public int TotalWarningsReported { get; private set; }

        public ProcessingNotificationsCollector(int maxErrorsWithDetails)
        {
            ErrorCountByReporter = new Dictionary<string, int>();
            MaxErrorsWithDetails = maxErrorsWithDetails;
            ProcessingErrorsDetails = new List<ProcessingNotification>(MaxErrorsWithDetails);
            TotalErrorsReported = 0;

            WarningCountByReporter = new Dictionary<string, int>();
            ProcessingWarningsDetails = new List<ProcessingNotification>(MaxErrorsWithDetails);
            TotalWarningsReported = 0;
        }

        public void ReportError(string message, string filePath, int lineNumber, string reportedBy)
        {
            if (TotalErrorsReported < MaxErrorsWithDetails)
            {
                ProcessingErrorsDetails.Add(new ProcessingNotification(message, filePath, lineNumber, reportedBy));
            }

            ++TotalErrorsReported;

            if (ErrorCountByReporter.ContainsKey(reportedBy))
            {
                ErrorCountByReporter[reportedBy] += 1;
            }
            else
            {
                ErrorCountByReporter.Add(reportedBy, 1);
            }
        }

        public void ReportError(string message, LogLine logLine, string reportedBy)
        {
            ReportError(message, logLine.LogFileInfo.FilePath, logLine.LineNumber, reportedBy);
        }
        
        public void ReportError(string message, string reportedBy)
        {
            ReportError(message, "N/A", 0, reportedBy);
        }

        public void ReportWarning(string message, string filePath, int lineNumber, string reportedBy)
        {
            if (TotalWarningsReported < MaxErrorsWithDetails)
            {
                ProcessingWarningsDetails.Add(new ProcessingNotification(message, filePath, lineNumber, reportedBy));
            }

            ++TotalWarningsReported;

            if (WarningCountByReporter.ContainsKey(reportedBy))
            {
                WarningCountByReporter[reportedBy] += 1;
            }
            else
            {
                WarningCountByReporter.Add(reportedBy, 1);
            }
        }

        public void ReportWarning(string message, LogLine logLine, string reportedBy)
        {
            ReportWarning(message, logLine.LogFileInfo.FilePath, logLine.LineNumber, reportedBy);
        }
        
        public void ReportWarning(string message, string reportedBy)
        {
            ReportWarning(message, "N/A", 0, reportedBy);
        }
    }
}