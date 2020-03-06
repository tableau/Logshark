using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;

namespace LogShark
{
    public static class Retry
    {
        public static async Task<TResult> DoWithRetries<TException, TResult>(
            string componentNameForLogging,
            ILogger logger,
            Func<Task<TResult>> whatToRun,
            int firstRetryDelaySeconds = 1,
            int secondRetryDelaySeconds = 2)
            where TException : Exception
        {
            return await Policy
                .Handle<TException>()
                .WaitAndRetryAsync(
                    new[]
                    {
                        TimeSpan.FromSeconds(firstRetryDelaySeconds),
                        TimeSpan.FromSeconds(secondRetryDelaySeconds)
                    },
                    (exception, timeSpan, retryCount, context) =>
                    {
                        logger.LogDebug("{componentName} had to retry its action. This is retry number {retryCount}. Exception was: {exceptionMessage}", componentNameForLogging ?? "null", retryCount, exception?.Message ?? "null");
                    })
                .ExecuteAsync(async () => await whatToRun());
        }
    }
}