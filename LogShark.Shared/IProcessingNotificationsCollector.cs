using LogShark.Shared.LogReading.Containers;

namespace LogShark.Shared
{
    public interface IProcessingNotificationsCollector
    {
        void ReportError(string message, string filePath, int lineNumber, string reportedBy);
        void ReportError(string message, LogLine logLine, string reportedBy);
        void ReportError(string message, string reportedBy);
        void ReportWarning(string message, string filePath, int lineNumber, string reportedBy);
        void ReportWarning(string message, LogLine logLine, string reportedBy);
        void ReportWarning(string message, string reportedBy);
    }
}