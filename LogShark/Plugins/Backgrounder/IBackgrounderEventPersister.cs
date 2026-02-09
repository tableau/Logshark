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
        IEnumerable<WriterLineCounts> DrainEvents();
    }
}