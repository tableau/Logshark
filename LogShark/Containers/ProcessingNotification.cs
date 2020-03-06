namespace LogShark.Containers
{
    public class ProcessingNotification
    {
        public string Message { get; }
        public string FilePath { get; }
        public int LineNumber { get; }
        public string ReportedBy { get; }

        public ProcessingNotification(string message, string filePath, int lineNumber, string reportedBy)
        {
            FilePath = filePath;
            LineNumber = lineNumber;
            Message = message;
            ReportedBy = reportedBy;
        }
    }
}