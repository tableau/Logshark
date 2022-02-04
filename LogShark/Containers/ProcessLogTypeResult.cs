using System;
using LogShark.Exceptions;

namespace LogShark.Containers
{
    public class ProcessLogTypeResult
    {
        public TimeSpan Elapsed { get; private set; }
        public string ErrorMessage { get; private set; }
        public ExitReason ExitReason { get; private set; }
        public int FilesProcessed { get; private set; }
        public long FilesSizeBytes { get; private set; }
        public long LinesProcessed { get; private set; }
        public bool IsSuccessful => ExitReason == ExitReason.CompletedSuccessfully;

        public ProcessLogTypeResult()
        {
            Elapsed = TimeSpan.Zero;
            ErrorMessage = null;
            ExitReason = ExitReason.CompletedSuccessfully;
            FilesProcessed = 0;
            FilesSizeBytes = 0;
            LinesProcessed = 0;
        }

        public void AddProcessFileResults(ProcessFileResult processFileResult)
        {
            AddProcessingInfo(processFileResult.Elapsed, processFileResult.FileSizeBytes, processFileResult.LinesProcessed, 1, processFileResult.ErrorMessage, processFileResult.ExitReason);
        }

        public override string ToString()
        {
            var fileSizeMb = FilesSizeBytes / 1024 / 1024;
            var mbPerSecond = Elapsed.TotalSeconds > 0
                ? fileSizeMb / Elapsed.TotalSeconds
                : fileSizeMb;

            return $"Processed {FilesProcessed} files in {Elapsed}. Elapsed: {Elapsed}. Total size: {fileSizeMb} MB. Processing at {mbPerSecond:0.00} MB/sec";
        }

        private void AddProcessingInfo(TimeSpan elapsed, long filesSizeBytes, long linesProcessed, int filesProcessed, string errorMessage, ExitReason exitReason)
        {
            Elapsed = new TimeSpan(Math.Max(Elapsed.Ticks, elapsed.Ticks)); // Take the larger of the two timespans since log parts are processed in parallel
            FilesSizeBytes += filesSizeBytes;
            LinesProcessed += linesProcessed;
            FilesProcessed += filesProcessed;

            if (errorMessage != null && exitReason != ExitReason.TaskCancelled)
            {
                ErrorMessage = ErrorMessage == null
                    ? errorMessage
                    : ErrorMessage + $"\n\nAdditional error message from another thread: {errorMessage}"; 
            }

            if (exitReason != ExitReason.CompletedSuccessfully && exitReason != ExitReason.TaskCancelled)
            {
                ExitReason = ExitReason == ExitReason.CompletedSuccessfully || ExitReason == exitReason
                    ? exitReason : ExitReason.MultipleExitReasonsOnDifferentThreads;
            }
        }
    }
}