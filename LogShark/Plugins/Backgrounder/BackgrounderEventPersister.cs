using System;
using System.Collections.Generic;
using System.Linq;
using LogShark.Containers;
using LogShark.Plugins.Backgrounder.Model;
using LogShark.Writers;
using LogShark.Writers.Containers;

namespace LogShark.Plugins.Backgrounder
{
    public class BackgrounderEventPersister : IBackgrounderEventPersister
    {
        private readonly Dictionary<long, BackgrounderEvent> _events = new Dictionary<long, BackgrounderEvent>();
        private readonly LatestStartEvents _latestStartEvents = new LatestStartEvents();
        
        private static readonly DataSetInfo JobsDsi = new DataSetInfo("Backgrounder", "BackgrounderJobs");
        private static readonly DataSetInfo JobErrorsDsi = new DataSetInfo("Backgrounder", "BackgrounderJobErrors");
        private static readonly DataSetInfo ExtractJobDetailsDsi = new DataSetInfo("Backgrounder", "BackgrounderExtractJobDetails");
        private static readonly DataSetInfo SubscriptionJobDetailsDsi = new DataSetInfo("Backgrounder", "BackgrounderSubscriptionJobDetails");
        
        private static readonly HashSet<string> ExtractRefreshJobTypes = new HashSet<string> { "refresh_extracts", "increment_extracts" };
        private static readonly HashSet<string> SubscriptionJobTypes = new HashSet<string> { "single_subscription_notify" };
        
        private readonly IWriter<BackgrounderJob> _jobWriter;
        private readonly IWriter<BackgrounderJobError> _jobErrorWriter;
        private readonly IWriter<BackgrounderExtractJobDetail> _extractJobDetailWriter;
        private readonly IWriter<BackgrounderSubscriptionJobDetail> _subscriptionJobDetailWriter;

        public BackgrounderEventPersister(IWriterFactory writerFactory)
        {
            _jobWriter = writerFactory.GetWriter<BackgrounderJob>(JobsDsi);
            _jobErrorWriter = writerFactory.GetWriter<BackgrounderJobError>(JobErrorsDsi);
            _extractJobDetailWriter = writerFactory.GetWriter<BackgrounderExtractJobDetail>(ExtractJobDetailsDsi);
            _subscriptionJobDetailWriter = writerFactory.GetWriter<BackgrounderSubscriptionJobDetail>(SubscriptionJobDetailsDsi);
        }

        public void AddStartEvent(BackgrounderJob startEvent)
        {
            if (_events.ContainsKey(startEvent.JobId))
            {
                var existingEvent = _events[startEvent.JobId];
                existingEvent.StartEvent = startEvent;
            }
            else
            {
                _events.Add(startEvent.JobId, new BackgrounderEvent{ StartEvent = startEvent });
            }
            
            _latestStartEvents.AddWatermark(startEvent);
        }

        public void AddEndEvent(BackgrounderJob endEvent)
        {
            if (_events.ContainsKey(endEvent.JobId))
            {
                var existingEvent = _events[endEvent.JobId];
                existingEvent.EndEvent = endEvent;
                PersistEventIfItIsComplete(existingEvent);
            }
            else
            {
                _events.Add(endEvent.JobId, new BackgrounderEvent{ EndEvent = endEvent });
            }
        }
        
        public void AddExtractJobDetails(BackgrounderExtractJobDetail extractJobDetail)
        {
            if (_events.ContainsKey(extractJobDetail.BackgrounderJobId))
            {
                _events[extractJobDetail.BackgrounderJobId].ExtractJobDetails = extractJobDetail;
            }
            else
            {
                _events.Add(extractJobDetail.BackgrounderJobId, new BackgrounderEvent{ ExtractJobDetails = extractJobDetail });
            }
        }
        
        public void AddSubscriptionJobDetails(BackgrounderSubscriptionJobDetail subscriptionJobDetail)
        {
            if (_events.ContainsKey(subscriptionJobDetail.BackgrounderJobId))
            {
                var existingEvent = _events[subscriptionJobDetail.BackgrounderJobId];
                existingEvent.AddSubscriptionDetail(subscriptionJobDetail);
                
            }
            else
            {
                _events.Add(subscriptionJobDetail.BackgrounderJobId, new BackgrounderEvent{ SubscriptionJobDetails = subscriptionJobDetail });
            }
        }

        public void AddErrorEvent(BackgrounderJobError jobError)
        {
            _jobErrorWriter.AddLine(jobError);
        }

        public IEnumerable<WriterLineCounts> DrainEvents()
        {
            foreach (var (_, @event) in _events)
            {
                if (@event.StartEvent != null)
                {
                    PersistEvent(@event, false);
                }
            }

            return new List<WriterLineCounts>
            {
                _jobWriter.Close(),
                _jobErrorWriter.Close(),
                _extractJobDetailWriter.Close(),
                _subscriptionJobDetailWriter.Close()
            };
        }
        
        public void Dispose()
        {
            _jobWriter?.Dispose();
            _jobErrorWriter?.Dispose();
            _extractJobDetailWriter?.Dispose();
            _subscriptionJobDetailWriter?.Dispose();
        }

        private void PersistEventIfItIsComplete(BackgrounderEvent @event)
        {
            if (@event.CanBeMergedNow()) 
            {
                PersistEvent(@event);
            }
        }

        private void PersistEvent(BackgrounderEvent @event, bool deleteFromDictionary = true)
        {
            var jobEvent = GetFinalJobRecord(@event);

            _jobWriter.AddLine(jobEvent);

            if (ExtractRefreshJobTypes.Contains(jobEvent.JobType)) 
            {
                var extractDetails = @event.ExtractJobDetails ?? new BackgrounderExtractJobDetail()
                {
                    BackgrounderJobId = @event.StartEvent.JobId,
                };
                    
                var argChunks = jobEvent.Args?
                    .Replace("[", "")
                    .Replace("]", "")
                    .Split(',')
                    .ToArray();

                if (argChunks?.Length > 0)
                {
                    extractDetails.ResourceType = argChunks[0].Trim();
                }

                if (argChunks?.Length >= 3)
                {
                    extractDetails.ResourceName = argChunks[2].Trim();
                }

                _extractJobDetailWriter.AddLine(extractDetails);
            }
                
            if (SubscriptionJobTypes.Contains(jobEvent.JobType) && @event.SubscriptionJobDetails != null)
            {
                _subscriptionJobDetailWriter.AddLine(@event.SubscriptionJobDetails);
            }

            if (deleteFromDictionary)
            {
                _events.Remove(jobEvent.JobId);
            }
        }
 
        private BackgrounderJob GetFinalJobRecord(BackgrounderEvent @event)
        {
            var finalEvent = @event.StartEvent;
                
            if (@event.IsComplete())
            {
                finalEvent.AddInfoFromEndEvent(@event.EndEvent);
                return finalEvent;
            }

            if (_latestStartEvents.StartedBeforeLatestStartEvent(finalEvent))
            {
                finalEvent.MarkAsTimedOut();
                return finalEvent;
            }

            finalEvent.MarkAsUnknown();
            return finalEvent;
        }
        
        private class BackgrounderEvent
        {
            public BackgrounderJob StartEvent { get; set; }
            public BackgrounderJob EndEvent { get; set; }
            public BackgrounderExtractJobDetail ExtractJobDetails { get; set; }
            public BackgrounderSubscriptionJobDetail SubscriptionJobDetails { get; set; }

            public void AddSubscriptionDetail(BackgrounderSubscriptionJobDetail newDetail)
            {
                if (SubscriptionJobDetails == null)
                {
                    SubscriptionJobDetails = newDetail;
                }
                else
                {
                    SubscriptionJobDetails.MergeInfo(newDetail);
                }
            }

            public bool CanBeMergedNow()
            {
                return IsComplete() && 
                       StartEvent.StartFile == EndEvent.EndFile; // If both lines are NOT in the same file, we don't have guarantee that we processed all lines after "start" event yet 
            }

            public bool IsComplete()
            {
                return (StartEvent != null && EndEvent != null);
            }
        }

        private class LatestStartEvents
        {
            private readonly IDictionary<string, DateTime> _latestStartEvents = new Dictionary<string, DateTime>();

            public void AddWatermark(BackgrounderJob startEvent)
            {
                var key = GetKeyForWorkerAndBackgrounder(startEvent.WorkerId, startEvent.BackgrounderId);

                if (_latestStartEvents.ContainsKey(key))
                {
                    if (startEvent.StartTime > _latestStartEvents[key])
                    {
                        _latestStartEvents[key] = startEvent.StartTime;
                    }
                }
                else
                {
                    _latestStartEvents.Add(key, startEvent.StartTime);
                }
            }

            public bool StartedBeforeLatestStartEvent(BackgrounderJob startEvent)
            {
                var key = GetKeyForWorkerAndBackgrounder(startEvent.WorkerId, startEvent.BackgrounderId);
                return _latestStartEvents.ContainsKey(key) && _latestStartEvents[key] > startEvent.StartTime;
            }
            
            private static string GetKeyForWorkerAndBackgrounder(string workerId, int? backgrounderId)
            {
                var backgrounderIdStr = backgrounderId == null ? "(null)" : backgrounderId.ToString(); 
                return $"{workerId}___{backgrounderIdStr}";
            }
        }
    }
}