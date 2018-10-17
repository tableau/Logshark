using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.PluginInterfaces;
using Logshark.Common.Extensions;
using Logshark.PluginLib.Helpers;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginModel.Model;
using Logshark.Plugins.Backgrounder.Helpers;
using Logshark.Plugins.Backgrounder.Model;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Logshark.Plugins.Backgrounder
{
    public class Backgrounder : BaseWorkbookCreationPlugin, IServerClassicPlugin, IServerTsmPlugin
    {
        private static readonly Regex BackgrounderIdFromFileNameRegex = new Regex(@"backgrounder(_node\d+)?-(?<process_id>\d+)\.",
                                                                                  RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        public override ISet<string> CollectionDependencies => new HashSet<string>
        {
            ParserConstants.BackgrounderJavaCollectionName
        };

        public override ICollection<string> WorkbookNames => new List<string>
        {
            "Backgrounder.twbx"
        };

        public Backgrounder() { }
        public Backgrounder(IPluginRequest request) : base(request) { }

        #region Public Methods

        public override IPluginResponse Execute()
        {
            var pluginResponse = CreatePluginResponse();

            Log.Info("Processing Backgrounder job events..");

            var collection = MongoDatabase.GetCollection<BsonDocument>(ParserConstants.BackgrounderJavaCollectionName);

            using (var jobPersister = new BackgrounderJobPersister(ExtractFactory))
            using (GetPersisterStatusWriter(jobPersister))
            {
                foreach (var workerId in MongoQueryHelper.GetDistinctWorkerIds(collection))
                {
                    foreach (var backgrounderId in MongoQueryHelper.GetDistinctBackgrounderIdsForWorker(collection, workerId))
                    {
                        foreach (var jobType in GetBackgrounderJobTypes(collection))
                        {
                            try
                            {
                                var jobs = ProcessJobsForBackgrounderId(workerId, backgrounderId, jobType, collection);
                                foreach (var job in jobs)
                                {
                                    jobPersister.Enqueue(job);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.ErrorFormat($"Failed to process events for job type '{jobType}' on backgrounder '{workerId}:{backgrounderId}': {ex.Message}");
                            }
                        }
                    }
                }

                Log.Info("Finished processing Backgrounder job events!");

                if (jobPersister.ItemsPersisted <= 0)
                {
                    Log.Warn("Failed to persist any data from Backgrounder logs!");
                    pluginResponse.GeneratedNoData = true;
                }

                return pluginResponse;
            }
        }

        public static int? GetBackgrounderIdFromFilename(string fileName)
        {
            var match = BackgrounderIdFromFileNameRegex.Match(fileName);
            if (match.Success)
            {
                int processId;
                if (int.TryParse(match.Groups["process_id"].Value, out processId))
                {
                    return processId;
                }
            }

            return null;
        }

        #endregion Public Methods

        #region Private Methods

        private static ISet<string> GetBackgrounderJobTypes(IMongoCollection<BsonDocument> backgrounderJavaCollection)
        {
            var backgrounderJobTypesInMongo = MongoQueryHelper.GetDistinctBackgrounderJobTypes(backgrounderJavaCollection).ToHashSet();

            if (!backgrounderJobTypesInMongo.Any())
            {
                // This could be a legacy logset; defer to the set of known job types.
                return BackgrounderConstants.KnownBackgrounderJobTypes;
            }

            return backgrounderJobTypesInMongo;
        }

        private IEnumerable<BackgrounderJob> ProcessJobsForBackgrounderId(string workerId, int backgrounderId, string jobType, IMongoCollection<BsonDocument> collection)
        {
            var recordQueue = new Queue<BsonDocument>(MongoQueryHelper.GetJobEventsForProcessByType(collection, workerId, backgrounderId, jobType));

            while (recordQueue.Count >= 2)
            {
                // This logic is a bit messy but unfortunately our backgrounder logs are messy.
                // We pop the next element off the record queue, make sure its a valid start event and then peek at the next element to make sure its the corresponding
                // Completion message, if it is then we go forward with processing, if it isn't then we drop whatever we previously popped and move on.
                // This prevents one failed completion message from throwing off the ordering of the whole queue.
                var startEvent = recordQueue.Dequeue();

                if (IsValidJobStartEvent(startEvent, jobType))
                {
                    BackgrounderJob job = null;

                    if (IsValidJobFinishEvent(recordQueue.Peek(), jobType))
                    {
                        var endEvent = recordQueue.Dequeue();
                        try
                        {
                            job = new BackgrounderJob(startEvent, endEvent);
                            job = AppendDetailsToJob(job, endEvent, collection);
                        }
                        catch (Exception ex)
                        {
                            Log.ErrorFormat($"Failed to extract job info from events '{startEvent}' & '{endEvent}': {ex.Message}");
                        }
                    }
                    // If the next event in the list isnt a finish event then we can assume the previous job timed out.
                    else
                    {
                        try
                        {
                            job = new BackgrounderJob(startEvent, true);
                            job = AppendDetailsToJob(job, recordQueue.Peek(), collection);
                        }
                        catch (Exception ex)
                        {
                            Log.ErrorFormat($"Failed to extract job info from timed-out event '{startEvent}': {ex.Message}");
                        }
                    }

                    yield return job;
                }
            }
        }

        private BackgrounderJob AppendDetailsToJob(BackgrounderJob job, BsonDocument endEvent, IMongoCollection<BsonDocument> collection)
        {
            var endTime = BsonDocumentHelper.GetDateTime("ts", endEvent);
            var eventsInJobRange = MongoQueryHelper.GetEventsInRange(collection, job.WorkerId, job.BackgrounderId, job.StartTime, endTime).ToList();

            // Append all errors associated with job.
            job.Errors = CollectErrorsForJob(job, eventsInJobRange);

            // Append details for certain job types of interest.
            if (job.JobType.Equals("refresh_extracts") || job.JobType.Equals("increment_extracts"))
            {
                var extractJobDetail = new BackgrounderExtractJobDetail(job, GetVqlSessionServiceEvents(eventsInJobRange));
                if (!string.IsNullOrEmpty(extractJobDetail.VizqlSessionId))
                {
                    job.BackgrounderJobDetail = extractJobDetail;
                }
            }
            else if (job.JobType.Equals("single_subscription_notify"))
            {
                var eventList = new List<BsonDocument>(GetVqlSessionServiceEvents(eventsInJobRange));
                eventList.AddRange(GetSubscriptionRunnerEvents(eventsInJobRange));

                var subscriptionJobDetail = new BackgrounderSubscriptionJobDetail(job, eventList);
                if (!string.IsNullOrEmpty(subscriptionJobDetail.VizqlSessionId))
                {
                    job.BackgrounderJobDetail = subscriptionJobDetail;
                }
            }

            return job;
        }

        private static ICollection<BackgrounderJobError> CollectErrorsForJob(BackgrounderJob job, IEnumerable<BsonDocument> eventsInJobRange)
        {
            var errorsForJob = new List<BackgrounderJobError>();

            foreach (var eventInJobRange in eventsInJobRange)
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

        private static IEnumerable<BsonDocument> GetVqlSessionServiceEvents(IEnumerable<BsonDocument> documents)
        {
            return documents.Where(document => BsonDocumentHelper.GetString("class", document).Equals(BackgrounderConstants.VqlSessionServiceClass, StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<BsonDocument> GetSubscriptionRunnerEvents(IEnumerable<BsonDocument> documents)
        {
            return documents.Where(document => BsonDocumentHelper.GetString("class", document).Equals(BackgrounderConstants.SubscriptionRunnerClass, StringComparison.OrdinalIgnoreCase) ||
                                               BsonDocumentHelper.GetString("class", document).Equals(BackgrounderConstants.EmailHelperClass, StringComparison.OrdinalIgnoreCase));
        }

        private static bool ErrorDocumentMatchesJobType(string jobType, BsonDocument errorDocument)
        {
            if (errorDocument.Contains("job_type")) // 10.5+
            {
                return BsonDocumentHelper.GetString("job_type", errorDocument).Equals(jobType);
            }
            
            // 9.0 - 10.4
            // If this is a fatal job error, lets make sure the message matches the job type
            var errorMessage = BsonDocumentHelper.GetString("message", errorDocument);
            return !errorMessage.StartsWith("Error executing backgroundjob:") || errorMessage.Contains(jobType);
        }

        private static bool IsValidJobStartEvent(BsonDocument jobStartEvent, string jobType)
        {
            if (!jobStartEvent.Contains("message"))
            {
                return false;
            }

            var message = BsonDocumentHelper.GetString("message", jobStartEvent);
            var messageHasJobStartText = message.StartsWith("Running job of type");

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

            var message = BsonDocumentHelper.GetString("message", jobFinishEvent);
            var messageHasJobFinishedText = message.StartsWith("Job finished:") || message.StartsWith("Error executing backgroundjob:");

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
            var eventSeverity = BsonDocumentHelper.GetString("sev", eventDocument);
            return eventSeverity.Equals("ERROR", StringComparison.OrdinalIgnoreCase) || eventSeverity.Equals("FATAL", StringComparison.OrdinalIgnoreCase);
        }

        #endregion Private Methods
    }
}