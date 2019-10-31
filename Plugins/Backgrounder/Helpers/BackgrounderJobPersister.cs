using Logshark.PluginModel.Model;
using Logshark.Plugins.Backgrounder.Model;
using System;
using System.Collections.Generic;

namespace Logshark.Plugins.Backgrounder.Helpers
{
    internal sealed class BackgrounderJobPersister : IPersister<BackgrounderJob>
    {
        private readonly IPersister<BackgrounderJob> _jobPersister;
        private readonly IPersister<BackgrounderJobError> _errorPersister;
        private readonly IPersister<BackgrounderExtractJobDetail> _extractJobDetailsPersister;
        private readonly IPersister<BackgrounderSubscriptionJobDetail> _subscriptionJobDetailsPersister;

        public long ItemsPersisted => _jobPersister.ItemsPersisted;

        public BackgrounderJobPersister(IExtractPersisterFactory extractFactory)
        {
            _jobPersister = extractFactory.CreateExtract<BackgrounderJob>("BackgrounderJobs.hyper");
            _errorPersister = extractFactory.CreateExtract<BackgrounderJobError>("BackgrounderJobErrors.hyper");
            _extractJobDetailsPersister = extractFactory.CreateExtract<BackgrounderExtractJobDetail>("BackgrounderExtractJobDetails.hyper");
            _subscriptionJobDetailsPersister = extractFactory.CreateExtract<BackgrounderSubscriptionJobDetail>("BackgrounderSubscriptionJobDetails.hyper");
        }

        public void Dispose()
        {
            _jobPersister?.Dispose();
            _errorPersister?.Dispose();
            _extractJobDetailsPersister?.Dispose();
            _subscriptionJobDetailsPersister?.Dispose();
        }

        public void Enqueue(IEnumerable<BackgrounderJob> jobs)
        {
            foreach (var job in jobs)
            {
                Enqueue(job);
            }
        }

        public void Enqueue(BackgrounderJob job)
        {
            _jobPersister.Enqueue(job);

            if (job.Errors != null && job.Errors.Count > 0)
            {
                _errorPersister.Enqueue(job.Errors);
            }

            if (job.BackgrounderJobDetail != null)
            {
                var detail = job.BackgrounderJobDetail as BackgrounderExtractJobDetail;
                if (detail != null)
                {
                    _extractJobDetailsPersister.Enqueue(detail);
                }
                else
                {
                    var jobDetail = job.BackgrounderJobDetail as BackgrounderSubscriptionJobDetail;
                    if (jobDetail != null)
                    {
                        _subscriptionJobDetailsPersister.Enqueue(jobDetail);
                    }
                }
            }
        }
    }
}