using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static readonly DataSetInfo FlowJobDetailsDsi = new DataSetInfo("Backgrounder", "BackgrounderFlowJobDetails");
        private static readonly DataSetInfo DataInserterDsi = new DataSetInfo("Backgrounder", "BackgrounderExtractDataInserter");

        private static readonly HashSet<string> ExtractRefreshJobTypes = new HashSet<string> { "refresh_extracts", "increment_extracts" };
        private static readonly HashSet<string> SubscriptionJobTypes = new HashSet<string> { "single_subscription_notify" };
        private static readonly HashSet<string> FlowJobTypes = new HashSet<string> { "run_flow" };
        private readonly IWriter<BackgrounderJob> _jobWriter;
        private readonly IWriter<BackgrounderJobError> _jobErrorWriter;
        private readonly IWriter<BackgrounderExtractJobDetail> _extractJobDetailWriter;
        private readonly IWriter<BackgrounderSubscriptionJobDetail> _subscriptionJobDetailWriter;
        private readonly IWriter<BackgrounderFlowJobDetail> _flowJobDetailWriter;
        private readonly IWriter<BackgrounderExtractDataInserter> _dataInserterWriter;

        // request_id -> job_id, harvested from Java lines. Used to link native inserter events to a job.
        private readonly Dictionary<string, string> _requestIdToJobId = new Dictionary<string, string>();
        private readonly object _requestMapLock = new object();

        // Native inserter events awaiting their complementary half, keyed by req + table-name.
        private readonly Dictionary<string, BackgrounderExtractDataInserter> _pendingInserters = new Dictionary<string, BackgrounderExtractDataInserter>();
        private readonly List<BackgrounderExtractDataInserter> _completedInserters = new List<BackgrounderExtractDataInserter>();
        private readonly object _inserterLock = new object();

        // Resolved at drain time: job_id -> inserter rows. Only extract-refresh jobs get written.
        private Dictionary<string, List<BackgrounderExtractDataInserter>> _inserterRowsByJobId;

        public BackgrounderEventPersister(IPluginWriterProvider writerProvider)
        {
            _jobWriter = writerProvider.GetWriter<BackgrounderJob>(JobsDsi);
            _jobErrorWriter = writerProvider.GetWriter<BackgrounderJobError>(JobErrorsDsi);
            _extractJobDetailWriter = writerProvider.GetWriter<BackgrounderExtractJobDetail>(ExtractJobDetailsDsi);
            _subscriptionJobDetailWriter = writerProvider.GetWriter<BackgrounderSubscriptionJobDetail>(SubscriptionJobDetailsDsi);
            _flowJobDetailWriter = writerProvider.GetWriter<BackgrounderFlowJobDetail>(FlowJobDetailsDsi);
            _dataInserterWriter = writerProvider.GetWriter<BackgrounderExtractDataInserter>(DataInserterDsi);
        }

        public void RegisterRequestToJobId(string requestId, string jobId)
        {
            if (string.IsNullOrEmpty(requestId) || requestId == "-" || string.IsNullOrEmpty(jobId))
            {
                return;
            }

            lock (_requestMapLock)
            {
                _requestIdToJobId[requestId] = jobId;
            }
        }

        public void AddDataInserterEvent(BackgrounderExtractDataInserter dataInserter)
        {
            // req + table-name is unique per table-insert within a job (req is unique per job run).
            if (dataInserter?.RequestId == null || dataInserter.TableName == null)
            {
                return;
            }

            var key = dataInserter.RequestId + "|" + dataInserter.TableName;
            lock (_inserterLock)
            {
                if (_pendingInserters.TryGetValue(key, out var existing))
                {
                    existing.FillMissingFrom(dataInserter);
                    _completedInserters.Add(existing);
                    _pendingInserters.Remove(key);
                }
                else
                {
                    _pendingInserters[key] = dataInserter;
                }
            }
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
        public void AddFlowJobDetails(BackgrounderFlowJobDetail flowJobDetail)
        {
            lock (_events)
            {
                var dateTimeKey = flowJobDetail.Timestamp;
                if (_events.ContainsKey(flowJobDetail.BackgrounderJobId))
                {
                    var existingDateTimeEvent = _events[flowJobDetail.BackgrounderJobId];
                    while (existingDateTimeEvent.ContainsKey(dateTimeKey))
                    {
                        dateTimeKey = dateTimeKey.AddTicks(1);
                    }
                    existingDateTimeEvent.Add(dateTimeKey, new BackgrounderEvent { FlowJobDetails = flowJobDetail });

                }
                else
                {
                    var dateTimeEvents = new SortedList<DateTime, BackgrounderEvent>();
                    dateTimeEvents.Add(flowJobDetail.Timestamp, new BackgrounderEvent { FlowJobDetails = flowJobDetail });

                    _events.Add(flowJobDetail.BackgrounderJobId, dateTimeEvents);
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
            _flowJobDetailWriter?.Dispose();
            _dataInserterWriter?.Dispose();
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

                // Stamp the worker from the matched job so every detail datasource carries it (see WorkerId doc).
                extractDetails.WorkerId = jobEvent.WorkerId;

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

                // Native inserter rows are only emitted for extract-refresh jobs (per design).
                if (_inserterRowsByJobId != null && _inserterRowsByJobId.TryGetValue(jobEvent.JobId, out var inserterRows))
                {
                    foreach (var inserterRow in inserterRows)
                    {
                        _dataInserterWriter.AddLine(inserterRow);
                    }
                }
            }
                
            if (SubscriptionJobTypes.Contains(jobEvent.JobType) && @event.SubscriptionJobDetails != null)
            {
                @event.SubscriptionJobDetails.WorkerId = jobEvent.WorkerId;
                _subscriptionJobDetailWriter.AddLine(@event.SubscriptionJobDetails);
            }
            if(FlowJobTypes.Contains(jobEvent.JobType) )
            {
                //Haven't figured this out but sometimes this event can be null
                if (@event.FlowJobDetails != null)
                {
                    @event.FlowJobDetails.WorkerId = jobEvent.WorkerId;
                    _flowJobDetailWriter.AddLine(@event.FlowJobDetails);
                }
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
            _inserterRowsByJobId = ResolveInserterRowsByJobId();

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
                        if(e.FlowJobDetails != null && newEvent.StartEvent != null)
                        {
                            newEvent.SetFlowJobDetail(e.FlowJobDetails);
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
                    _subscriptionJobDetailWriter.Close(),
                    _flowJobDetailWriter.Close(),
                    _dataInserterWriter.Close()
                };
            }
        }

        // Flushes any unpaired native inserter events and groups all inserter rows by the job_id their
        // req resolves to. Rows whose req matches no job (orphans) are dropped: without a job we cannot
        // confirm they belong to an extract refresh.
        private Dictionary<string, List<BackgrounderExtractDataInserter>> ResolveInserterRowsByJobId()
        {
            var result = new Dictionary<string, List<BackgrounderExtractDataInserter>>();

            lock (_inserterLock)
            {
                foreach (var pending in _pendingInserters.Values)
                {
                    _completedInserters.Add(pending);
                }
                _pendingInserters.Clear();

                lock (_requestMapLock)
                {
                    foreach (var row in _completedInserters)
                    {
                        if (row.RequestId == null || !_requestIdToJobId.TryGetValue(row.RequestId, out var jobId))
                        {
                            continue;
                        }

                        row.BackgrounderJobId = jobId;
                        if (!result.TryGetValue(jobId, out var rows))
                        {
                            rows = new List<BackgrounderExtractDataInserter>();
                            result[jobId] = rows;
                        }
                        rows.Add(row);
                    }
                }
            }

            return result;
        }

        private class BackgrounderEvent
        {
            public BackgrounderEvent(int requeueExten = 0) => RequeueExten = requeueExten;
            public BackgrounderJob StartEvent { get; set; }
            public BackgrounderJob EndEvent { get; set; }
            public BackgrounderExtractJobDetail ExtractJobDetails { get; set; }
            public BackgrounderSubscriptionJobDetail SubscriptionJobDetails { get; set; }
            public BackgrounderFlowJobDetail FlowJobDetails { get; set; }
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
            public void SetFlowJobDetail(BackgrounderFlowJobDetail flowJobDetail)
            {
                FlowJobDetails = flowJobDetail;
                if (RequeueExten != 0)
                    FlowJobDetails.BackgrounderJobId = flowJobDetail.BackgrounderJobId + "-requeue-" + RequeueExten;
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