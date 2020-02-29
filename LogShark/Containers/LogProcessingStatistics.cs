using System;

namespace LogShark.Containers
{
    public class LogProcessingStatistics
    {
        public TimeSpan Elapsed { get; private set; }
        public int FilesProcessed { get; private set; }
        public long FilesSizeBytes { get; private set; }
        public long LinesProcessed { get; private set; }
        
        public LogProcessingStatistics()
        {
            FilesProcessed = 0;
            FilesSizeBytes = 0;
            LinesProcessed = 0;
            Elapsed = TimeSpan.Zero;
        }

        public void AddProcessingInfo(TimeSpan elapsed, long filesSizeBytes, long linesProcessed, int filesProcessed = 1)
        {
            Elapsed += elapsed;
            FilesSizeBytes += filesSizeBytes;
            LinesProcessed += linesProcessed;
            FilesProcessed += filesProcessed;
        }
        
        public void AddNumbersFrom(LogProcessingStatistics otherResult)
        {
            AddProcessingInfo(otherResult.Elapsed, otherResult.FilesSizeBytes, otherResult.LinesProcessed, otherResult.FilesProcessed);
        }

        public override string ToString()
        {
            var fileSizeMb = FilesSizeBytes / 1024 / 1024;
            var mbPerSecond = Elapsed.TotalSeconds > 0
                ? fileSizeMb / Elapsed.TotalSeconds
                : fileSizeMb;
            
            return $"Processed {FilesProcessed} files in {Elapsed}. Elapsed: {Elapsed}. Total size: {fileSizeMb} MB. Processing at {mbPerSecond:0.00} MB/sec";
        }
    }
}