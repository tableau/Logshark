using Microsoft.Extensions.Logging;

namespace LogShark.Tests.E2E
{
    class TestLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger();
        }

        public void Dispose()
        {
        }
    }
}
