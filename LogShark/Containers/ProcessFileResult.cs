using System;
using LogShark.Shared;

namespace LogShark.Containers
{
    public class ProcessFileResult
    {
        public TimeSpan Elapsed { get; }
        public string ErrorMessage { get; }
        public ExitReason ExitReason { get; }
        public long FileSizeBytes { get; }
        public long LinesProcessed { get; }
        public LogType LogType { get; }
        public bool IsSuccessful => ExitReason == ExitReason.CompletedSuccessfully;

        public ProcessFileResult(LogType logType, ProcessStreamResult processStreamResult, long fileSizeBytes, TimeSpan elapsed)
        {
            Elapsed = elapsed;
            ErrorMessage = processStreamResult.ErrorMessage;
            ExitReason = processStreamResult.ExitReason;
            FileSizeBytes = fileSizeBytes;
            LinesProcessed = processStreamResult.LinesProcessed;
            LogType = logType;
        }
        
        public ProcessFileResult(LogType logType, string errorMessage, ExitReason exitReason, long linesProcessed = 0)
        {
            Elapsed = TimeSpan.Zero;
            ErrorMessage = errorMessage;
            ExitReason = exitReason;
            FileSizeBytes = 0;
            LinesProcessed = linesProcessed;
            LogType = logType;
        }
    }
}