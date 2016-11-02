using log4net;
using Logshark.PluginLib.Helpers;
using Logshark.PluginLib.Logging;
using Logshark.PluginLib.Persistence;
using Logshark.Plugins.Backgrounder.Helpers;
using Logshark.Plugins.Backgrounder.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Logshark.Plugins.Backgrounder
{
    internal class BackgrounderJobProcessor
    {
        private readonly IMongoCollection<BsonDocument> backgrounderJavaCollection;
        private readonly IPersister<BackgrounderJob> backgrounderPersister;
        private readonly Guid logsetHash;

        private static readonly Regex BackgrounderIdFromFileNameRegex = new Regex(@"^backgrounder-(?<process_id>\d+)\.", RegexOptions.Compiled);

        private static readonly ILog Log = PluginLogFactory.GetLogger(Assembly.GetExecutingAssembly(),
                                                                      MethodBase.GetCurrentMethod());

        public BackgrounderJobProcessor(IMongoDatabase mongoDatabase, IPersister<BackgrounderJob> backgrounderPersister)
        {
            backgrounderJavaCollection = mongoDatabase.GetCollection<BsonDocument>(BackgrounderConstants.BackgrounderJavaCollectionName);
            this.backgrounderPersister = backgrounderPersister;
            logsetHash = Guid.Parse(backgrounderJavaCollection.CollectionNamespace.DatabaseNamespace.DatabaseName);
        }

        #region Public Methods

        public void ProcessJobs()
        {
            foreach (int workerId in GetDistinctWorkerIndices())
            {
                foreach (int backgrounderId in GetDistinctBackgrounderIds(workerId))
                {
                    foreach (var jobType in BackgrounderConstants.BackgrounderJobTypes)
                    {
                        try
                        {
                            ProcessJobsForBackgrounderId(workerId, backgrounderId, jobType);
                        }
                        catch (Exception ex)
                        {
                            Log.ErrorFormat("Failed to process events for job type '{0}' on backgrounder '{1}:{2}': {3}", jobType, workerId, backgrounderId, ex.Message);
                        }
                    }
                }
            }
        }

        public IEnumerable<int> GetDistinctWorkerIndices()
        {
            return MongoQueryHelper.GetDistinctWorkerIndices(backgrounderJavaCollection);
        }

        public IEnumerable<int> GetDistinctBackgrounderIds(int workerId)
        {
            return MongoQueryHelper.GetDistinctBackgrounderIdsForWorker(backgrounderJavaCollection, workerId);
        }

        public static int? GetBackgrounderIdFromFilename(string fileName)
        {
            Match match = BackgrounderIdFromFileNameRegex.Match(fileName);
            if (match.Success)
            {
                int processId;
                if (Int32.TryParse(match.Groups["process_id"].Value, out processId))
                {
                    return processId;
                }
            }

            return null;
        }

        #endregion Public Methods

        #region Private Methods

        private void ProcessJobsForBackgrounderId(int workerId, int backgrounderId, string jobType)
        {
            Queue<BsonDocument> recordQueue = new Queue<BsonDocument>(MongoQueryHelper.GetJobEventsForProcessByType(workerId, backgrounderId, jobType, backgrounderJavaCollection));

            while (recordQueue.Count >= 2)
            {
                // This logic is a bit messy but unfortunately our backgrounder logs are messy.
                // We pop the next element off the record queue, make sure its a valid start event and then peek at the next element to make sure its the corresponding
                // Completion message, if it is then we go forward with processing, if it isn't then we drop whatever we previously popped and move on.
                // This prevents one failed completion message from throwing off the ordering of the whole queue.
                BsonDocument startEvent = recordQueue.Dequeue();
                if (IsValidJobStartEvent(startEvent, jobType))
                {
                    if (IsValidJobFinishEvent(recordQueue.Peek(), jobType))
                    {
                        BsonDocument endEvent = recordQueue.Dequeue();
                        try
                        {
                            var job = new BackgrounderJob(startEvent, endEvent, logsetHash);
                            AppendDetailsToJob(job, endEvent);
                            backgrounderPersister.Enqueue(job);
                        }
                        catch (Exception ex)
                        {
                            Log.ErrorFormat("Failed to extract job info from events '{0}' & '{1}': {2}", startEvent, endEvent, ex.Message);
                        }
                    }
                    // If the next event in the list isnt a finish event then we can assume the previous job timed out.
                    else
                    {
                        try
                        {
                            var job = new BackgrounderJob(startEvent, true, logsetHash);
                            AppendDetailsToJob(job, recordQueue.Peek());
                            backgrounderPersister.Enqueue(job);
                        }
                        catch (Exception ex)
                        {
                            Log.ErrorFormat("Failed to extract job info from timed-out event '{0}': {1}", startEvent, ex.Message);
                        }
                    }
                }
            }
        }

        private void AppendDetailsToJob(BackgrounderJob job, BsonDocument endEvent)
        {
            DateTime endTime = BsonDocumentHelper.GetDateTime("ts", endEvent);
            var eventsInJobRange = MongoQueryHelper.GetEventsInRange(backgrounderJavaCollection, job.WorkerId, job.BackgrounderId, job.StartTime, endTime).ToList();

            // Append all errors associated with job.
            job.Errors = CollectErrorsForJob(job, eventsInJobRange);

            // Append details for certain job types of interest.
            if (job.JobType.Equals("refresh_extracts") || job.JobType.Equals("increment_extracts"))
            {
                BackgrounderExtractJobDetail extractJobDetail = new BackgrounderExtractJobDetail(job, GetVqlSessionServiceEvents(eventsInJobRange));
                if (!String.IsNullOrEmpty(extractJobDetail.VizqlSessionId))
                {
                    job.BackgrounderJobDetail = extractJobDetail;
                }
            }
            else if (job.JobType.Equals("single_subscription_notify"))
            {
                List<BsonDocument> eventList = new List<BsonDocument>(GetVqlSessionServiceEvents(eventsInJobRange));
                eventList.AddRange(GetSubscriptionRunnerEvents(eventsInJobRange));

                BackgrounderSubscriptionJobDetail subscriptionJobDetail = new BackgrounderSubscriptionJobDetail(job, eventList);
                if (!String.IsNullOrEmpty(subscriptionJobDetail.VizqlSessionId))
                {
                    job.BackgrounderJobDetail = subscriptionJobDetail;
                }
            }
        }

        private ICollection<BackgrounderJobError> CollectErrorsForJob(BackgrounderJob job, IEnumerable<BsonDocument> eventsInJobRange)
        {
            var errorsForJob = new List<BackgrounderJobError>();

            foreach (BsonDocument eventInJobRange in eventsInJobRange)
            {
                if (IsErrorEvent(eventInJobRange))
                {
                    if (ErrorDocumentMatchesJobType(job.JobType, eventInJobRange))
                    {
                        errorsForJob.Add(new BackgrounderJobError(job.JobId, eventInJobRange));
                    }
                }
            }

            return errorsForJob;
        }

        private IEnumerable<BsonDocument> GetVqlSessionServiceEvents(IEnumerable<BsonDocument> documents)
        {
            return documents.Where(document => BsonDocumentHelper.GetString("class", document).Equals(BackgrounderConstants.VqlSessionServiceClass, StringComparison.OrdinalIgnoreCase));
        }

        private IEnumerable<BsonDocument> GetSubscriptionRunnerEvents(IEnumerable<BsonDocument> documents)
        {
            return documents.Where(document => BsonDocumentHelper.GetString("class", document).Equals(BackgrounderConstants.SubscriptionRunnerClass, StringComparison.OrdinalIgnoreCase) ||
                                               BsonDocumentHelper.GetString("class", document).Equals(BackgrounderConstants.EmailHelperClass, StringComparison.OrdinalIgnoreCase));
        }

        private bool ErrorDocumentMatchesJobType(string jobType, BsonDocument errorDocument)
        {
            string errorMessage = BsonDocumentHelper.GetString("message", errorDocument);

            //If this is a fatal job error, lets make sure the message matches the job type
            if (errorMessage.StartsWith("Error executing backgroundjob:"))
            {
                return errorMessage.Contains(jobType);
            }
            else
            {
                return true;
            }
        }

        private static bool IsValidJobStartEvent(BsonDocument jobStartEvent, string jobType)
        {
            if (!jobStartEvent.Contains("message"))
            {
                return false;
            }

            return BsonDocumentHelper.GetString("message", jobStartEvent).StartsWith(String.Format("Running job of type :{0}", jobType));
        }

        private static bool IsValidJobFinishEvent(BsonDocument jobFinishEvent, string jobType)
        {
            if (!jobFinishEvent.Contains("message") || !jobFinishEvent.Contains("sev"))
            {
                return false;
            }

            string severity = BsonDocumentHelper.GetString("sev", jobFinishEvent);

            if (severity.Equals("INFO", StringComparison.OrdinalIgnoreCase))
            {
                string message = BsonDocumentHelper.GetString("message", jobFinishEvent);
                return message.StartsWith("Job finished") && message.Contains(String.Format("type :{0}", jobType));
            }
            else if (severity.Equals("ERROR", StringComparison.OrdinalIgnoreCase))
            {
                return BsonDocumentHelper.GetString("message", jobFinishEvent).StartsWith(String.Format("Error executing backgroundjob: :{0}", jobType));
            }

            return false;
        }

        private static bool IsErrorEvent(BsonDocument eventDocument)
        {
            string eventSeverity = BsonDocumentHelper.GetString("sev", eventDocument);
            return eventSeverity.Equals("ERROR", StringComparison.OrdinalIgnoreCase) || eventSeverity.Equals("FATAL", StringComparison.OrdinalIgnoreCase);
        }

        #endregion Private Methods
    }
}