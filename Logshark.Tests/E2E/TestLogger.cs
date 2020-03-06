using FluentAssertions;
using Microsoft.Extensions.Logging;
using System;

namespace LogShark.Tests.E2E
{
    class TestLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            string message = string.Empty;

            if (formatter != null)
            {
                message = formatter(state, exception);
            }

            logLevel.Should().NotBe(LogLevel.Critical, message ?? "");
            logLevel.Should().NotBe(LogLevel.Error, message ?? "");
            logLevel.Should().NotBe(LogLevel.Warning, message ?? "");
        }
    }
}
