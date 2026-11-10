using System;
using LogShark.Shared;

namespace LogShark.Progress
{
    /// <summary>
    /// Immutable snapshot of current progress state
    /// </summary>
    public class ProgressSnapshot
    {
        public LogType? CurrentLogType { get; }
        public int CurrentLogTypeFilesCompleted { get; }
        public int CurrentLogTypeTotalFiles { get; }
        public long CurrentLogTypeBytesProcessed { get; }
        public long CurrentLogTypeTotalBytes { get; }
        public double CurrentLogTypeSpeedMbPerSec { get; }
        public TimeSpan? CurrentLogTypeEta { get; }

        public int TotalLogTypesCompleted { get; }
        public int TotalLogTypesWithFiles { get; }
        public TimeSpan? OverallEta { get; }
        public TimeSpan ElapsedTime { get; }
        public DateTime? EstimatedCompletionTime { get; }

        public int TotalFilesCompleted { get; }
        public int TotalFilesTotal { get; }
        public double OverallPercentage { get; }

        public ProgressSnapshot(
            LogType? currentLogType,
            int currentLogTypeFilesCompleted,
            int currentLogTypeTotalFiles,
            long currentLogTypeBytesProcessed,
            long currentLogTypeTotalBytes,
            double currentLogTypeSpeedMbPerSec,
            TimeSpan? currentLogTypeEta,
            int totalLogTypesCompleted,
            int totalLogTypesWithFiles,
            TimeSpan? overallEta,
            TimeSpan elapsedTime,
            DateTime? estimatedCompletionTime,
            int totalFilesCompleted,
            int totalFilesTotal,
            double overallPercentage)
        {
            CurrentLogType = currentLogType;
            CurrentLogTypeFilesCompleted = currentLogTypeFilesCompleted;
            CurrentLogTypeTotalFiles = currentLogTypeTotalFiles;
            CurrentLogTypeBytesProcessed = currentLogTypeBytesProcessed;
            CurrentLogTypeTotalBytes = currentLogTypeTotalBytes;
            CurrentLogTypeSpeedMbPerSec = currentLogTypeSpeedMbPerSec;
            CurrentLogTypeEta = currentLogTypeEta;
            TotalLogTypesCompleted = totalLogTypesCompleted;
            TotalLogTypesWithFiles = totalLogTypesWithFiles;
            OverallEta = overallEta;
            ElapsedTime = elapsedTime;
            EstimatedCompletionTime = estimatedCompletionTime;
            TotalFilesCompleted = totalFilesCompleted;
            TotalFilesTotal = totalFilesTotal;
            OverallPercentage = overallPercentage;
        }
    }
}
