using LogShark.Containers;

namespace LogShark
{
    public interface IProcessingNotificationsCollector
    {
        void ReportError(string message, string filePath, int lineNumber, string reportedBy);
        void ReportError(string message, LogLine logLine, string reportedBy);
        void ReportWarning(string message, string filePath, int lineNumber, string reportedBy);
        void ReportWarning(string message, LogLine logLine, string reportedBy);
    }
}