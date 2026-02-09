using System;
using LogShark.Writers.Containers;

namespace LogShark.Containers
{
    public class RunSummary
    {
        public WorkbookGeneratorResults WorkbookGeneratorResults { get; }
        public TimeSpan Elapsed { get; }
        public ExitReason ExitReason { get; }
        public bool IsSuccess => ExitReason == ExitReason.CompletedSuccessfully;
        public bool? IsTransient => IsErrorTypeTransient();
        public ProcessLogSetResult ProcessLogSetResult { get; }
        public ProcessingNotificationsCollector ProcessingNotificationsCollector { get; }
        public PublisherResults PublisherResults { get; }
        public string ReasonForFailure { get; }
        public string RunId { get; }

        public static RunSummary SuccessfulRunSummary(
            string runId,
            TimeSpan elapsed,
            ProcessingNotificationsCollector processingNotificationsCollector,
            ProcessLogSetResult processLogSetResult,
            WorkbookGeneratorResults workbookGeneratorResults,
            PublisherResults publisherResults)
        {
            return new RunSummary(
                runId,
                elapsed,
                processingNotificationsCollector,
                processLogSetResult,
                workbookGeneratorResults,
                publisherResults,
                null,
                ExitReason.CompletedSuccessfully);
        }
        
        public static RunSummary FailedRunSummary(
            string runId,
            string errorMessage,
            ExitReason exitReason = ExitReason.UnclassifiedError,
            ProcessingNotificationsCollector processingNotificationsCollector = null,
            TimeSpan? elapsed = null,
            ProcessLogSetResult processLogSetResult = null,
            WorkbookGeneratorResults workbookGeneratorResults = null,
            PublisherResults publisherResults = null)
        {
            return new RunSummary(
                runId,
                elapsed ?? TimeSpan.Zero,
                processingNotificationsCollector,
                processLogSetResult,
                workbookGeneratorResults,
                publisherResults,
                errorMessage,
                exitReason);
        }

        private RunSummary(
            string runId,
            TimeSpan elapsed,
            ProcessingNotificationsCollector processingNotificationsCollector,
            ProcessLogSetResult processLogSetResult,
            WorkbookGeneratorResults workbookGeneratorResults,
            PublisherResults publisherResults,
            string reasonForFailure,
            ExitReason exitReason)
        {
            RunId = runId;
            Elapsed = elapsed;
            ExitReason = exitReason;
            ProcessingNotificationsCollector = processingNotificationsCollector;
            WorkbookGeneratorResults = workbookGeneratorResults;
            ProcessLogSetResult = processLogSetResult;
            PublisherResults = publisherResults;
            ReasonForFailure = reasonForFailure;
        }

        private bool? IsErrorTypeTransient()
        {
            switch (ExitReason)
            {
                case ExitReason.BadLogSet:
                case ExitReason.IncorrectConfiguration:
                case ExitReason.LogLineTooLong:
                case ExitReason.LogSetDoesNotContainRelevantLogs:
                case ExitReason.OutOfMemory:
                    return false;
                case ExitReason.CompletedSuccessfully:
                case ExitReason.UnclassifiedError:
                case ExitReason.TaskCancelled:
                default:
                    return null;
            }
        }
    }

    public enum ExitReason
    {
        BadLogSet,
        CompletedSuccessfully,
        IncorrectConfiguration,
        LogLineTooLong,
        LogSetDoesNotContainRelevantLogs,
        OutOfMemory,
        TaskCancelled,
        InserterDisposed,
        HyperTimeout,
        UnclassifiedError,
        MultipleExitReasonsOnDifferentThreads
    }
}