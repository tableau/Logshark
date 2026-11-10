using System;
using System.Collections.Generic;
using LogShark.Plugins.Backgrounder.Model;
using LogShark.Writers.Containers;

namespace LogShark.Plugins.Backgrounder
{
    public interface IBackgrounderEventPersister : IDisposable
    {
        void AddStartEvent(BackgrounderJob startEvent);
        void AddEndEvent(BackgrounderJob endEvent);
        void AddExtractJobDetails(BackgrounderExtractJobDetail extractJobDetail);
        void AddSubscriptionJobDetails(BackgrounderSubscriptionJobDetail subscriptionJobDetail);

        void AddFlowJobDetails(BackgrounderFlowJobDetail flowJobDetail);
        void AddErrorEvent(BackgrounderJobError jobError);

        // Records the request_id -> job_id mapping harvested from Java backgrounder lines, used to
        // link nativeapi (BackgrounderCpp) inserter events (which only carry req) back to a job.
        void RegisterRequestToJobId(string requestId, string jobId);

        // Accepts one half of a native data-inserter event; the persister merges the two halves
        // (inserter-end + data-inserter-summary) that share the same req + table-name.
        void AddDataInserterEvent(BackgrounderExtractDataInserter dataInserter);

        IEnumerable<WriterLineCounts> DrainEvents();
    }
}