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
        private readonly Dictionary<string, SortedList<DateTime, BackgrounderEvent>>  _events = new Dictionary<string, SortedList<DateTime, BackgrounderEvent>> ();
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
            lock (_events)
            {
                var dateTimeKey = startEvent.StartTime;
                if (_events.ContainsKey(startEvent.JobId))
                {
                    var existingDateTimeEvents = _events[startEvent.JobId];
                    while (existingDateTimeEvents.ContainsKey(dateTimeKey))
                    {
                        dateTimeKey = dateTimeKey.AddTicks(1);
                    }

                    existingDateTimeEvents.Add(dateTimeKey, new BackgrounderEvent { StartEvent = startEvent });
                }
                else
                {
                    var dateTimeEvents = new SortedList<DateTime, BackgrounderEvent>();
                    dateTimeEvents.Add(startEvent.StartTime, new BackgrounderEvent { StartEvent = startEvent });
                    _events.Add(startEvent.JobId, dateTimeEvents);
                }
            }

            lock (_latestStartEvents)
            {
                _latestStartEvents.AddWatermark(startEvent);
            }
        }

        public void AddEndEvent(BackgrounderJob endEvent)
        {
            lock (_events)
            {
                var dateTimeKey = (DateTime)endEvent.EndTime;
                if (_events.ContainsKey(endEvent.JobId))
                {
                    var existingDateTimeEvent = _events[endEvent.JobId];
                    while (existingDateTimeEvent.ContainsKey(dateTimeKey))
                    {
                        dateTimeKey = dateTimeKey.AddTicks(1);
                    }
                    existingDateTimeEvent.Add(dateTimeKey, new BackgrounderEvent { EndEvent = endEvent });
                }
                else
                {
                    var dateTimeEvents = new SortedList<DateTime, BackgrounderEvent>();
                    dateTimeEvents.Add(dateTimeKey, new BackgrounderEvent { EndEvent = endEvent });

                    _events.Add(endEvent.JobId, dateTimeEvents);
                }
            }
        }
        
        public void AddExtractJobDetails(BackgrounderExtractJobDetail extractJobDetail)
        {
            lock (_events)
            {
                var dateTimeKey = extractJobDetail.Timestamp;
                if (_events.ContainsKey(extractJobDetail.BackgrounderJobId))
                {
                    var existingDateTimeEvent = _events[extractJobDetail.BackgrounderJobId];
                    while (existingDateTimeEvent.ContainsKey(dateTimeKey))
                    {
                        dateTimeKey = dateTimeKey.AddTicks(1);
                    }
                    existingDateTimeEvent.Add(dateTimeKey, new BackgrounderEvent { ExtractJobDetails = extractJobDetail });
                }
                else
                {
                    var dateTimeEvents = new SortedList<DateTime, BackgrounderEvent>();
                    dateTimeEvents.Add(extractJobDetail.Timestamp, new BackgrounderEvent { ExtractJobDetails = extractJobDetail });

                    _events.Add(extractJobDetail.BackgrounderJobId, dateTimeEvents);
                }
            }
        }
        
        public void AddSubscriptionJobDetails(BackgrounderSubscriptionJobDetail subscriptionJobDetail)
        {
            lock (_events)
            {
                var dateTimeKey = subscriptionJobDetail.Timestamp;
                if (_events.ContainsKey(subscriptionJobDetail.BackgrounderJobId))
                {
                    var existingDateTimeEvent = _events[subscriptionJobDetail.BackgrounderJobId];
                    while (existingDateTimeEvent.ContainsKey(dateTimeKey))
                    {
                        dateTimeKey = dateTimeKey.AddTicks(1);
                    }
                    existingDateTimeEvent.Add(dateTimeKey, new BackgrounderEvent { SubscriptionJobDetails = subscriptionJobDetail });

                }
                else
                {
                    var dateTimeEvents = new SortedList<DateTime, BackgrounderEvent>();
                    dateTimeEvents.Add(subscriptionJobDetail.Timestamp, new BackgrounderEvent { SubscriptionJobDetails = subscriptionJobDetail });

                    _events.Add(subscriptionJobDetail.BackgrounderJobId, dateTimeEvents);
                }
            }
        }

        public void AddErrorEvent(BackgrounderJobError jobError)
        {
            _jobErrorWriter.AddLine(jobError);
        }

        public IEnumerable<WriterLineCounts> DrainEvents()
        {
            return DrainAllEvents();
        }

        public void Dispose()
        {
            _jobWriter?.Dispose();
            _jobErrorWriter?.Dispose();
            _extractJobDetailWriter?.Dispose();
            _subscriptionJobDetailWriter?.Dispose();
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
                finalEvent.MarkAsInvalidEnd();
                return finalEvent;
            }

            finalEvent.MarkAsUnknown();
            return finalEvent;
        }

        private IEnumerable<WriterLineCounts> DrainAllEvents()
        {
            lock (_events)
            {
                foreach (var (jobId, @event) in _events)
                {
                    int requeueExtension = 0;
                    BackgrounderEvent newEvent = new BackgrounderEvent();
                    foreach (var e in @event.Values)
                    {
                        if(e.StartEvent != null)
                        {
                            if(requeueExtension != 0)
                            {
                                PersistEvent(newEvent, false);
                                newEvent = new BackgrounderEvent(requeueExtension++);
                            } 
                            else
                            {
                                requeueExtension++;
                            }

                            newEvent.SetStartEvent(e.StartEvent);
                        }
                        if(e.EndEvent != null && newEvent.StartEvent != null)
                        {
                            newEvent.SetEndEvent(e.EndEvent);
                        }
                        if (e.SubscriptionJobDetails != null && newEvent.StartEvent != null)
                        {
                            newEvent.AddSubscriptionDetail(e.SubscriptionJobDetails);
                        }
                        if (e.ExtractJobDetails != null && newEvent.StartEvent != null)
                        {
                            newEvent.SetExtractJobDetail(e.ExtractJobDetails);
                        }
                    }

                    if (newEvent.StartEvent != null)
                    {
                        PersistEvent(newEvent, false);
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
        }

        private class BackgrounderEvent
        {
            public BackgrounderEvent(int requeueExten = 0) => RequeueExten = requeueExten;
            public BackgrounderJob StartEvent { get; set; }
            public BackgrounderJob EndEvent { get; set; }
            public BackgrounderExtractJobDetail ExtractJobDetails { get; set; }
            public BackgrounderSubscriptionJobDetail SubscriptionJobDetails { get; set; }
            public int RequeueExten { get; set; }

            public void AddSubscriptionDetail(BackgrounderSubscriptionJobDetail newDetail)
            {
                if (SubscriptionJobDetails == null)
                {
                    SubscriptionJobDetails = newDetail;
                    if (RequeueExten != 0)
                        SubscriptionJobDetails.BackgrounderJobId = newDetail.BackgrounderJobId + "-requeue-" + RequeueExten;
                }
                else
                {
                    SubscriptionJobDetails.MergeInfo(newDetail);
                }
            }

            public bool IsComplete()
            {
                return (StartEvent != null && EndEvent != null);
            }

            public void SetStartEvent(BackgrounderJob job)
            {
                StartEvent = job;
                if(RequeueExten != 0)
                    StartEvent.JobId = StartEvent.JobId + "-requeue-" + RequeueExten;
            }

            public void SetEndEvent(BackgrounderJob job)
            {
                EndEvent = job;
                if (RequeueExten != 0)
                    EndEvent.JobId = EndEvent.JobId + "-requeue-" + RequeueExten;
            }

            public void SetExtractJobDetail(BackgrounderExtractJobDetail extractDetail)
            {
                ExtractJobDetails = extractDetail;
                if (RequeueExten != 0)
                    ExtractJobDetails.BackgrounderJobId = extractDetail.BackgrounderJobId + "-requeue-" + RequeueExten;
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
            
            private static string GetKeyForWorkerAndBackgrounder(string workerId, string backgrounderId)
            {
                var backgrounderIdStr = backgrounderId == null ? "(null)" : backgrounderId.ToString(); 
                return $"{workerId}___{backgrounderIdStr}";
            }
        }
    }
}