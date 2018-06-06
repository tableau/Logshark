using log4net;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.Common.Extensions;
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

        public BackgrounderJobProcessor(IMongoDatabase mongoDatabase, IPersister<BackgrounderJob> backgrounderPersister, Guid logsetHash)
        {
            backgrounderJavaCollection = mongoDatabase.GetCollection<BsonDocument>(ParserConstants.BackgrounderJavaCollectionName);
            this.backgrounderPersister = backgrounderPersister;
            this.logsetHash = logsetHash;
        }

        #region Public Methods

        public void ProcessJobs()
        {
            ISet<string> backgrounderJobTypes = GetBackgrounderJobTypes();

            foreach (string workerId in GetDistinctWorkerIds())
            {
                foreach (int backgrounderId in GetDistinctBackgrounderIds(workerId))
                {
                    foreach (var jobType in backgrounderJobTypes)
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

        public ISet<string> GetBackgrounderJobTypes()
        {
            var backgrounderJobTypesInMongo = MongoQueryHelper.GetDistinctBackgrounderJobTypes(backgrounderJavaCollection).ToHashSet();

            if (!backgrounderJobTypesInMongo.Any())
            {
                // This could be a legacy logset; defer to the set of known job types.
                return BackgrounderConstants.KnownBackgrounderJobTypes;
            }

            return backgrounderJobTypesInMongo;
        }

        public IEnumerable<string> GetDistinctWorkerIds()
        {
            return MongoQueryHelper.GetDistinctWorkerIds(backgrounderJavaCollection);
        }

        public IEnumerable<int> GetDistinctBackgrounderIds(string workerId)
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

        private void ProcessJobsForBackgrounderId(string workerId, int backgrounderId, string jobType)
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
            if (errorDocument.Contains("job_type")) // 10.5+
            {
                return BsonDocumentHelper.GetString("job_type", errorDocument).Equals(jobType);
            }
            else // 9.0 - 10.4
            {
                // If this is a fatal job error, lets make sure the message matches the job type
                string errorMessage = BsonDocumentHelper.GetString("message", errorDocument);
                return !errorMessage.StartsWith("Error executing backgroundjob:") || errorMessage.Contains(jobType);
            }
        }

        private static bool IsValidJobStartEvent(BsonDocument jobStartEvent, string jobType)
        {
            if (!jobStartEvent.Contains("message"))
            {
                return false;
            }

            string message = BsonDocumentHelper.GetString("message", jobStartEvent);
            bool messageHasJobStartText = message.StartsWith("Running job of type");

            if (jobStartEvent.Contains("job_type")) // 10.5+
            {
                return messageHasJobStartText && BsonDocumentHelper.GetString("job_type", jobStartEvent).Equals(jobType);
            }
            else // 9.0 - 10.4
            {
                return messageHasJobStartText && message.Contains(String.Concat(" :", jobType));
            }
        }

        private static bool IsValidJobFinishEvent(BsonDocument jobFinishEvent, string jobType)
        {
            if (!jobFinishEvent.Contains("message"))
            {
                return false;
            }

            string message = BsonDocumentHelper.GetString("message", jobFinishEvent);
            bool messageHasJobFinishedText = message.StartsWith("Job finished:") || message.StartsWith("Error executing backgroundjob:");

            if (jobFinishEvent.Contains("job_type")) // 10.5+
            {
                return messageHasJobFinishedText && BsonDocumentHelper.GetString("job_type", jobFinishEvent).Equals(jobType);
            }
            else // 9.0 - 10.4
            {
                return messageHasJobFinishedText && message.Contains(String.Concat(" :", jobType));
            }
        }

        private static bool IsErrorEvent(BsonDocument eventDocument)
        {
            string eventSeverity = BsonDocumentHelper.GetString("sev", eventDocument);
            return eventSeverity.Equals("ERROR", StringComparison.OrdinalIgnoreCase) || eventSeverity.Equals("FATAL", StringComparison.OrdinalIgnoreCase);
        }

        #endregion Private Methods
    }
}