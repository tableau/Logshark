using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LogShark.Logging
{
    /// <summary>
    /// Writes LogShark's own diagnostic log to disk, named after the logset being processed, rolling over
    /// to a new numbered file once the size limit configured in "Logging:FileSizeLimitBytes" is reached.
    /// Replaces Serilog.Extensions.Logging.File, whose only rollover trigger is a calendar-day boundary -
    /// its size limit just caps and silently drops further lines within a day.
    /// </summary>
    public class RollingFileLoggerProvider : ILoggerProvider
    {
        private readonly LogLevel _defaultLogLevel;
        private readonly Dictionary<string, LogLevel> _categoryLogLevelOverrides;
        private readonly RollingFileWriter _writer;

        public RollingFileLoggerProvider(IConfiguration loggingConfig, string logSetLocation)
        {
            var logLevelSection = loggingConfig.GetSection("LogLevel");
            _defaultLogLevel = logLevelSection.GetValue("Default", LogLevel.Information);
            _categoryLogLevelOverrides = new Dictionary<string, LogLevel>();
            foreach (var child in logLevelSection.GetChildren())
            {
                if (child.Key != "Default" && Enum.TryParse<LogLevel>(child.Value, true, out var level))
                {
                    _categoryLogLevelOverrides[child.Key] = level;
                }
            }

            var directory = loggingConfig.GetValue("Directory", "Logs");
            var fileNamePrefix = loggingConfig.GetValue("FileNamePrefix", "LogShark");
            var fileSizeLimitBytes = loggingConfig.GetValue("FileSizeLimitBytes", 10 * 1024 * 1024);
            var retainedFileCountLimit = loggingConfig.GetValue("RetainedFileCountLimit", 5);

            var logSetName = GetLogSetName(logSetLocation);
            var baseFileName = $"{fileNamePrefix}-{DateTime.Now:yyyyMMddHHmmss}-{logSetName}";

            _writer = new RollingFileWriter(directory, baseFileName, fileSizeLimitBytes, retainedFileCountLimit);
        }

        // Extracts the log-set name for the file name. Splits on both separators explicitly rather than
        // using Path.GetFileNameWithoutExtension on the whole path, because a log-set location can be a
        // Windows path, a Linux path, or an S3 key - and Path only treats the running OS's separator as one,
        // so a Windows path processed on a Linux host (hosted containers) would keep its backslashes.
        private static string GetLogSetName(string logSetLocation)
        {
            if (string.IsNullOrWhiteSpace(logSetLocation))
            {
                return "unspecified";
            }

            var lastSeparator = logSetLocation.LastIndexOfAny(new[] { '/', '\\' });
            var fileName = lastSeparator >= 0 ? logSetLocation.Substring(lastSeparator + 1) : logSetLocation;
            return Path.GetFileNameWithoutExtension(fileName);
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new RollingFileLogger(categoryName, GetMinLevelForCategory(categoryName), _writer);
        }

        // Filters by hand instead of relying solely on the shared ILoggerFactory's config-driven filtering:
        // when --progress is on, Program.ConfigureLogging sets the whole factory's minimum level to Trace
        // and skips builder.AddConfiguration(loggingConfig) so console progress output isn't filtered - but
        // the file log should still honor Logging:LogLevel regardless of console/progress mode.
        private LogLevel GetMinLevelForCategory(string categoryName)
        {
            var bestMatchLength = -1;
            var level = _defaultLogLevel;

            foreach (var (category, categoryLevel) in _categoryLogLevelOverrides)
            {
                if (category.Length > bestMatchLength && categoryName.StartsWith(category, StringComparison.OrdinalIgnoreCase))
                {
                    bestMatchLength = category.Length;
                    level = categoryLevel;
                }
            }

            return level;
        }

        public void Dispose()
        {
            _writer.Dispose();
        }

        private class RollingFileLogger : ILogger
        {
            private readonly string _categoryName;
            private readonly LogLevel _minLevel;
            private readonly RollingFileWriter _writer;

            public RollingFileLogger(string categoryName, LogLevel minLevel, RollingFileWriter writer)
            {
                _categoryName = categoryName;
                _minLevel = minLevel;
                _writer = writer;
            }

            public IDisposable BeginScope<TState>(TState state) => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && logLevel >= _minLevel;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (!IsEnabled(logLevel))
                {
                    return;
                }

                var message = formatter(state, exception);
                if (string.IsNullOrEmpty(message) && exception == null)
                {
                    return;
                }

                _writer.WriteLine(FormatLine(logLevel, message, exception));
            }

            private string FormatLine(LogLevel logLevel, string message, Exception exception)
            {
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{GetLogLevelText(logLevel)}] {_categoryName}: {message}";
                return exception == null ? line : $"{line}{Environment.NewLine}{exception}";
            }

            private static string GetLogLevelText(LogLevel logLevel) => logLevel switch
            {
                LogLevel.Trace => "TRCE",
                LogLevel.Debug => "DBUG",
                LogLevel.Information => "INFO",
                LogLevel.Warning => "WARN",
                LogLevel.Error => "FAIL",
                LogLevel.Critical => "CRIT",
                _ => "????"
            };
        }

        /// <summary>
        /// Owns the current file handle for one run's log. Synchronous, lock-serialized writes with a flush
        /// after every line trade a little throughput for guaranteeing no buffered lines are lost if the
        /// process is killed mid-run.
        /// </summary>
        private sealed class RollingFileWriter : IDisposable
        {
            private const string Extension = ".log";

            private readonly object _gate = new object();
            private readonly string _directory;
            private readonly string _baseFileName;
            private readonly long _fileSizeLimitBytes;
            private readonly int _retainedFileCountLimit;

            private StreamWriter _currentWriter;
            private int _rollIndex;

            public RollingFileWriter(string directory, string baseFileName, long fileSizeLimitBytes, int retainedFileCountLimit)
            {
                _directory = directory;
                _baseFileName = baseFileName;
                _fileSizeLimitBytes = fileSizeLimitBytes;
                _retainedFileCountLimit = retainedFileCountLimit;

                Directory.CreateDirectory(_directory);
                OpenFile(rollIndex: 0);
            }

            public void WriteLine(string line)
            {
                lock (_gate)
                {
                    try
                    {
                        _currentWriter.WriteLine(line);
                        _currentWriter.Flush();
                    }
                    catch (IOException ex)
                    {
                        // Deliberately writes to stderr instead of routing through ILogger: this class IS
                        // one of the registered log providers, so logging its own write failure back through
                        // the shared ILoggerFactory would dispatch right back into this same failing writer.
                        Console.Error.WriteLine($"LogShark: failed to write to log file: {ex.Message}");
                        return;
                    }

                    if (_currentWriter.BaseStream.Length > _fileSizeLimitBytes)
                    {
                        Roll();
                    }
                }
            }

            // Opens the next file before touching current state, so a failed roll (disk full, permissions,
            // locked by another process) leaves the writer able to keep appending to the current file
            // instead of being left pointing at an already-disposed writer.
            private void Roll()
            {
                StreamWriter newWriter;
                var nextRollIndex = _rollIndex + 1;
                try
                {
                    newWriter = CreateWriter(nextRollIndex);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    Console.Error.WriteLine($"LogShark: failed to roll to a new log file, continuing to write to the current file: {ex.Message}");
                    return;
                }

                _currentWriter.Dispose();
                _currentWriter = newWriter;
                _rollIndex = nextRollIndex;

                RolledLogFileRetentionPruner.PruneOldRolledFiles(_directory, _baseFileName, Extension, _retainedFileCountLimit);
            }

            private void OpenFile(int rollIndex)
            {
                _currentWriter = CreateWriter(rollIndex);
            }

            private StreamWriter CreateWriter(int rollIndex)
            {
                var path = GetPath(rollIndex);
                return new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read));
            }

            private string GetPath(int rollIndex)
            {
                var fileName = rollIndex == 0 ? $"{_baseFileName}{Extension}" : $"{_baseFileName}_{rollIndex:000}{Extension}";
                return Path.Combine(_directory, fileName);
            }

            public void Dispose()
            {
                lock (_gate)
                {
                    _currentWriter?.Flush();
                    _currentWriter?.Dispose();
                }
            }
        }
    }
}
