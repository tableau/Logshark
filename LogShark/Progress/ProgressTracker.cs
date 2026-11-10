using System;
using System.Collections.Generic;
using System.Linq;
using LogShark.Shared;

namespace LogShark.Progress
{
    /// <summary>
    /// Tracks processing progress across all log types and calculates ETAs
    /// Thread-safe: Designed to be called from within semaphore-protected code
    /// </summary>
    public class ProgressTracker
    {
        private class LogTypeStats
        {
            public int TotalFiles { get; set; }
            public long TotalBytes { get; set; }
            public int FilesCompleted { get; set; }
            public long BytesProcessed { get; set; }
            public readonly List<FileProcessingRecord> RecentFiles = new List<FileProcessingRecord>();
        }

        private class FileProcessingRecord
        {
            public long Bytes { get; set; }
            public TimeSpan Elapsed { get; set; }
            public double MbPerSec => Elapsed.TotalSeconds > 0 ? (Bytes / 1024.0 / 1024.0) / Elapsed.TotalSeconds : 0;
        }

        private readonly Dictionary<LogType, LogTypeStats> _logTypeStats = new Dictionary<LogType, LogTypeStats>();
        private readonly ConsoleProgressRenderer _renderer;
        private readonly object _lock = new object();
        private LogType? _currentLogType;
        private int _totalFilesCompleted;
        private int _totalFilesTotal;
        private DateTime _startTime;

        public ProgressTracker(ConsoleProgressRenderer renderer)
        {
            _renderer = renderer;
        }

        /// <summary>
        /// Initialize tracker with expected file counts per log type
        /// </summary>
        public void Initialize(Dictionary<LogType, int> fileCountsByLogType)
        {
            _startTime = DateTime.Now;

            foreach (var (logType, fileCount) in fileCountsByLogType)
            {
                if (fileCount > 0)
                {
                    _logTypeStats[logType] = new LogTypeStats
                    {
                        TotalFiles = fileCount,
                        TotalBytes = 0 // Will be estimated as files are processed
                    };
                }
            }

            _totalFilesTotal = _logTypeStats.Values.Sum(s => s.TotalFiles);
        }

        /// <summary>
        /// Record completion of a single file
        /// </summary>
        public void OnFileCompleted(LogType logType, long fileSizeBytes, TimeSpan processingTime)
        {
            lock (_lock)
            {
                if (!_logTypeStats.ContainsKey(logType))
                {
                    // This shouldn't happen, but handle gracefully
                    _logTypeStats[logType] = new LogTypeStats { TotalFiles = 1 };
                }

                var stats = _logTypeStats[logType];
                stats.FilesCompleted++;
                stats.BytesProcessed += fileSizeBytes;

                // Keep last 3 files for speed averaging
                stats.RecentFiles.Add(new FileProcessingRecord { Bytes = fileSizeBytes, Elapsed = processingTime });
                if (stats.RecentFiles.Count > 3)
                {
                    stats.RecentFiles.RemoveAt(0);
                }

                // Estimate total bytes for this log type based on average file size
                if (stats.TotalFiles > 0 && stats.FilesCompleted > 0)
                {
                    var avgFileSize = stats.BytesProcessed / stats.FilesCompleted;
                    stats.TotalBytes = avgFileSize * stats.TotalFiles;
                }

                _currentLogType = logType;
                _totalFilesCompleted++;

                // Render progress immediately
                _renderer.Update(GetSnapshot());
            }
        }

        /// <summary>
        /// Get current progress snapshot for display
        /// </summary>
        public ProgressSnapshot GetSnapshot()
        {
            lock (_lock)
            {
                if (_currentLogType == null || !_logTypeStats.ContainsKey(_currentLogType.Value))
                {
                    return CreateEmptySnapshot();
                }

            var currentStats = _logTypeStats[_currentLogType.Value];

            // Calculate current log type speed (average of recent files)
            var currentSpeed = currentStats.RecentFiles.Any()
                ? currentStats.RecentFiles.Average(f => f.MbPerSec)
                : 0;

            // Calculate current log type ETA
            TimeSpan? currentLogTypeEta = null;
            if (currentSpeed > 0 && currentStats.TotalBytes > 0)
            {
                var remainingBytes = currentStats.TotalBytes - currentStats.BytesProcessed;
                var remainingMb = remainingBytes / 1024.0 / 1024.0;
                var secondsRemaining = remainingMb / currentSpeed;
                currentLogTypeEta = TimeSpan.FromSeconds(secondsRemaining);
            }

            // Count completed log types
            var completedLogTypes = _logTypeStats.Values.Count(s => s.FilesCompleted >= s.TotalFiles && s.TotalFiles > 0);

            // Calculate overall ETA (rough estimate)
            TimeSpan? overallEta = null;
            DateTime? estimatedCompletionTime = null;

            if (_totalFilesTotal > 0 && _totalFilesCompleted > 0)
            {
                // Get average speed across all completed files
                var allRecentFiles = _logTypeStats.Values
                    .SelectMany(s => s.RecentFiles)
                    .ToList();

                if (allRecentFiles.Any())
                {
                    var avgSpeed = allRecentFiles.Average(f => f.MbPerSec);
                    if (avgSpeed > 0)
                    {
                        // Estimate remaining bytes based on average file size so far
                        var totalBytesProcessed = _logTypeStats.Values.Sum(s => s.BytesProcessed);
                        var avgFileSize = totalBytesProcessed / _totalFilesCompleted;
                        var remainingFiles = _totalFilesTotal - _totalFilesCompleted;
                        var estimatedRemainingBytes = avgFileSize * remainingFiles;
                        var estimatedRemainingMb = estimatedRemainingBytes / 1024.0 / 1024.0;
                        var secondsRemaining = estimatedRemainingMb / avgSpeed;
                        overallEta = TimeSpan.FromSeconds(secondsRemaining);
                        estimatedCompletionTime = DateTime.Now.Add(overallEta.Value);
                    }
                }
            }

            // Calculate elapsed time and overall percentage
            var elapsedTime = DateTime.Now - _startTime;
            var overallPercentage = _totalFilesTotal > 0
                ? (_totalFilesCompleted * 100.0) / _totalFilesTotal
                : 0;

                return new ProgressSnapshot(
                    currentLogType: _currentLogType,
                    currentLogTypeFilesCompleted: currentStats.FilesCompleted,
                    currentLogTypeTotalFiles: currentStats.TotalFiles,
                    currentLogTypeBytesProcessed: currentStats.BytesProcessed,
                    currentLogTypeTotalBytes: currentStats.TotalBytes,
                    currentLogTypeSpeedMbPerSec: currentSpeed,
                    currentLogTypeEta: currentLogTypeEta,
                    totalLogTypesCompleted: completedLogTypes,
                    totalLogTypesWithFiles: _logTypeStats.Count,
                    overallEta: overallEta,
                    elapsedTime: elapsedTime,
                    estimatedCompletionTime: estimatedCompletionTime,
                    totalFilesCompleted: _totalFilesCompleted,
                    totalFilesTotal: _totalFilesTotal,
                    overallPercentage: overallPercentage
                );
            }
        }

        private ProgressSnapshot CreateEmptySnapshot()
        {
            var elapsedTime = _startTime != default ? DateTime.Now - _startTime : TimeSpan.Zero;

            return new ProgressSnapshot(
                currentLogType: null,
                currentLogTypeFilesCompleted: 0,
                currentLogTypeTotalFiles: 0,
                currentLogTypeBytesProcessed: 0,
                currentLogTypeTotalBytes: 0,
                currentLogTypeSpeedMbPerSec: 0,
                currentLogTypeEta: null,
                totalLogTypesCompleted: 0,
                totalLogTypesWithFiles: 0,
                overallEta: null,
                elapsedTime: elapsedTime,
                estimatedCompletionTime: null,
                totalFilesCompleted: 0,
                totalFilesTotal: 0,
                overallPercentage: 0
            );
        }

        /// <summary>
        /// Mark processing as complete
        /// </summary>
        public void Complete()
        {
            _renderer.Complete();
        }
    }
}
