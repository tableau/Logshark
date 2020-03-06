using System;
using LogShark.Writers.Containers;

namespace LogShark.Containers
{
    public class RunSummary
    {
        public WorkbookGeneratorResults WorkbookGeneratorResults { get; }
        public TimeSpan Elapsed { get; }
        public bool IsSuccess => string.IsNullOrWhiteSpace(ReasonForFailure);
        public bool? IsTransient { get; }
        public LogReadingResults LogReadingResults { get; }
        public ProcessingNotificationsCollector ProcessingNotificationsCollector { get; }
        public PublisherResults PublisherResults { get; }
        public string ReasonForFailure { get; }
        public string RunId { get; }

        public RunSummary(string runId,
            TimeSpan elapsed,
            ProcessingNotificationsCollector processingNotificationsCollector,
            LogReadingResults logReadingResults,
            WorkbookGeneratorResults workbookGeneratorResults,
            PublisherResults publisherResults,
            string reasonForFailure = null,
            bool? isTransient = null)
        {
            RunId = runId;
            Elapsed = elapsed;
            ProcessingNotificationsCollector = processingNotificationsCollector;
            WorkbookGeneratorResults = workbookGeneratorResults;
            LogReadingResults = logReadingResults;
            PublisherResults = publisherResults;
            ReasonForFailure = reasonForFailure;
            IsTransient = isTransient;
        }
    }
}