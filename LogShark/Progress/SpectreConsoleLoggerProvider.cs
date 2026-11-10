using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace LogShark.Progress
{
    /// <summary>
    /// Logger provider that writes through Spectre.Console instead of Console.WriteLine
    /// This ensures logs appear above the LiveDisplay without pushing it around
    /// </summary>
    public class SpectreConsoleLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<string> _logBuffer = new ConcurrentQueue<string>();
        private readonly int _maxBufferSize;
        private readonly LogLevel _minLevel;
        private bool _liveDisplayActive = true;

        public SpectreConsoleLoggerProvider(LogLevel minLevel = LogLevel.Trace, int maxBufferSize = 500)
        {
            _minLevel = minLevel;
            _maxBufferSize = maxBufferSize;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new SpectreConsoleLogger(categoryName, _minLevel, _logBuffer, _maxBufferSize, () => _liveDisplayActive);
        }

        public IEnumerable<string> GetRecentLogs(int count)
        {
            return _logBuffer.TakeLast(count);
        }

        /// <summary>
        /// Call this when LiveDisplay ends to flush buffered logs and switch to direct output
        /// </summary>
        public void OnLiveDisplayEnded()
        {
            _liveDisplayActive = false;

            // Flush all buffered logs to console
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold yellow]════════════════════════════════════════════════════════════════════════[/]");
            AnsiConsole.MarkupLine("[bold yellow]Progress Complete - Full Log Output:[/]");
            AnsiConsole.MarkupLine("[bold yellow]════════════════════════════════════════════════════════════════════════[/]");

            foreach (var log in _logBuffer)
            {
                AnsiConsole.MarkupLine(log);
            }

            AnsiConsole.WriteLine();
        }

        public void Dispose()
        {
        }

        private class SpectreConsoleLogger : ILogger
        {
            private readonly string _categoryName;
            private readonly LogLevel _minLevel;
            private readonly ConcurrentQueue<string> _logBuffer;
            private readonly int _maxBufferSize;
            private readonly Func<bool> _isLiveDisplayActive;

            public SpectreConsoleLogger(string categoryName, LogLevel minLevel, ConcurrentQueue<string> logBuffer, int maxBufferSize, Func<bool> isLiveDisplayActive)
            {
                _categoryName = categoryName;
                _minLevel = minLevel;
                _logBuffer = logBuffer;
                _maxBufferSize = maxBufferSize;
                _isLiveDisplayActive = isLiveDisplayActive;
            }

            public IDisposable BeginScope<TState>(TState state) => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (!IsEnabled(logLevel))
                {
                    return;
                }

                var message = formatter(state, exception);
                if (string.IsNullOrEmpty(message))
                {
                    return;
                }

                var logLevelColor = GetLogLevelColor(logLevel);
                var logLevelText = GetLogLevelText(logLevel);
                var timestamp = DateTime.Now.ToString("HH:mm:ss");

                var formattedMessage = $"[dim]{timestamp}[/] [{logLevelColor}]{logLevelText}[/]: {Markup.Escape(message)}";

                // Add to buffer for LiveDisplay
                _logBuffer.Enqueue(formattedMessage);
                while (_logBuffer.Count > _maxBufferSize)
                {
                    _logBuffer.TryDequeue(out _);
                }

                // If LiveDisplay has ended, also output directly to console
                if (!_isLiveDisplayActive())
                {
                    AnsiConsole.MarkupLine(formattedMessage);
                }
            }

            private string GetLogLevelColor(LogLevel logLevel)
            {
                return logLevel switch
                {
                    LogLevel.Trace => "grey",
                    LogLevel.Debug => "grey",
                    LogLevel.Information => "green",
                    LogLevel.Warning => "yellow",
                    LogLevel.Error => "red",
                    LogLevel.Critical => "red bold",
                    _ => "white"
                };
            }

            private string GetLogLevelText(LogLevel logLevel)
            {
                return logLevel switch
                {
                    LogLevel.Trace => "trce",
                    LogLevel.Debug => "dbug",
                    LogLevel.Information => "info",
                    LogLevel.Warning => "warn",
                    LogLevel.Error => "fail",
                    LogLevel.Critical => "crit",
                    _ => "????"
                };
            }
        }
    }
}
