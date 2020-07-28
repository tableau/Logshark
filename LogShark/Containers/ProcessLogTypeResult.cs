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

        public void AddProcessingInfo(TimeSpan elapsed, long filesSizeBytes, ProcessFileResult processFileResult, int filesProcessed = 1)
        {
            AddProcessingInfo(elapsed, filesSizeBytes, processFileResult.LinesProcessed, filesProcessed, processFileResult.ErrorMessage, processFileResult.ExitReason);
        }
        
        public void AddNumbersFrom(ProcessLogTypeResult otherResult)
        {
            AddProcessingInfo(otherResult.Elapsed, otherResult.FilesSizeBytes, otherResult.LinesProcessed, otherResult.FilesProcessed, otherResult.ErrorMessage, otherResult.ExitReason);
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
            Elapsed += elapsed;
            FilesSizeBytes += filesSizeBytes;
            LinesProcessed += linesProcessed;
            FilesProcessed += filesProcessed;

            if (errorMessage != null)
            {
                ErrorMessage = ErrorMessage == null
                    ? errorMessage
                    : throw new LogSharkProgramLogicException($"{nameof(ProcessLogTypeResult)} already contains error and code is trying to push in another one. Why did not we stop at the first error? Previous error: `{ErrorMessage}`. New error: `{errorMessage}`");
            }

            if (exitReason != ExitReason.CompletedSuccessfully)
            {
                ExitReason = ExitReason == ExitReason.CompletedSuccessfully
                    ? exitReason
                    : throw new LogSharkConfigurationException($"{nameof(ProcessLogTypeResult)} already contains non-successful exit reason and code is trying to push in another one. Why did not we stop at the first error? Previous exit reason: `{ExitReason}`. New exit reason: `{exitReason}`");
            }
        }
    }
}