using log4net;
using LogParsers.Base;
using LogParsers.Base.Helpers;
using LogParsers.Base.Parsers;
using Logshark.Common.Extensions;
using Logshark.Common.TaskSchedulers;
using Logshark.ConnectionModel.Helpers;
using Logshark.ConnectionModel.Mongo;
using Logshark.Core.Controller.Metadata.Logset.Mongo;
using Logshark.Core.Helpers;
using Logshark.Core.Helpers.StatusWriter;
using Logshark.RequestModel;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Logshark.Core.Controller.Parsing
{
    /// <summary>
    /// Handles the processing of a logset into MongoDB.
    /// </summary>
    internal class MongoWriter
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly LogsharkRequest logsharkRequest;
        private readonly IParserFactory parserFactory;
        private readonly LogsetPreprocessor logsetPreprocessor;
        private readonly IMongoDatabase database;

        public MongoWriter(LogsharkRequest request, IParserFactory parserFactory)
        {
            logsharkRequest = request;
            this.parserFactory = parserFactory;
            logsetPreprocessor = new LogsetPreprocessor(request, parserFactory);
            database = request.Configuration.MongoConnectionInfo.GetDatabase(request.RunContext.MongoDatabaseName);
        }

        /// <summary>
        /// Processes an entire directory of log files.
        /// </summary>
        public void ProcessLogset()
        {
            Log.InfoFormat("Processing log directory '{0}'..", logsharkRequest.RunContext.RootLogDirectory);

            var parseTimer = logsharkRequest.RunContext.CreateTimer("Parsed Files");
            Queue<LogFileContext> logFiles = logsetPreprocessor.Preprocess();

            var metadataWriter = new LogsetMetadataWriter(logsharkRequest);
            metadataWriter.WritePreProcessingMetadata();

            using (new MongoProcessingHeartbeatTimer(logsharkRequest))
            {
                ProcessFiles(logFiles);
            }

            metadataWriter.WritePostProcessingMetadata(processedSuccessfully: true);
            metadataWriter.WriteMasterMetadataRecord();

            parseTimer.Stop();
            Log.InfoFormat("Finished processing log directory {0}! [{1}]", logsharkRequest.RunContext.RootLogDirectory, parseTimer.Elapsed.Print());
        }

        /// <summary>
        /// Spins off processing threads for a collection of log files.
        /// </summary>
        /// <param name="logFiles">The log files to process.</param>
        private void ProcessFiles(IEnumerable<LogFileContext> logFiles)
        {
            CreateMongoDbCollections();

            // Set up task scheduler.
            var factory = GetFileProcessingTaskFactory();

            var tasks = new List<Task>();
            foreach (LogFileContext logFile in logFiles)
            {
                tasks.Add(factory.StartNew(() => ProcessFile(logFile)));
            }

            const string progressMessage = "Logset processing is approximately {PercentComplete} complete. {TasksRemaining} files remaining..";
            using (new TaskStatusWriter(tasks, Log, progressMessage, pollIntervalSeconds: 15))
            {
                Task.WaitAll(tasks.ToArray());
            }
        }

        /// <summary>
        /// Process a single log file.
        /// </summary>
        /// <param name="fileContext"></param>
        private void ProcessFile(LogFileContext fileContext)
        {
            try
            {
                Log.InfoFormat("Processing {0}.. ({1})", fileContext, fileContext.FileSize.ToPrettySize());
                var parseTimer = logsharkRequest.RunContext.CreateTimer("Parse File", fileContext.ToString());

                // Attempt to process the file; register a failure if we don't yield at least one document for a file
                // with at least one byte of content.
                var fileProcessor = new MongoInsertionFileProcessor(fileContext, logsharkRequest, parserFactory);
                bool processedSuccessfully = fileProcessor.ProcessFile();
                if (fileContext.FileSize > 0 && !processedSuccessfully)
                {
                    Log.WarnFormat("Failed to parse any log events from {0}!", fileContext);
                    logsharkRequest.RunContext.RegisterParseFailure(fileContext.ToString());
                }

                parseTimer.Stop();
                Log.InfoFormat("Completed processing of {0} ({1}) [{2}]", fileContext,
                    fileContext.FileSize.ToPrettySize(), parseTimer.Elapsed.Print());
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }

            Cleanup(fileContext);
        }

        /// <summary>
        /// Creates a task factory to handle all file processing tasks.
        /// </summary>
        /// <returns></returns>
        private TaskFactory GetFileProcessingTaskFactory()
        {
            int maxFileProcessingConcurrency = Environment.ProcessorCount * logsharkRequest.Configuration.TuningOptions.FileProcessorConcurrencyLimitPerCore;
            Log.InfoFormat("Setting file processing concurrency limit to {0} concurrent files. ({1} logical {2} present)",
                           maxFileProcessingConcurrency, Environment.ProcessorCount, "core".Pluralize(Environment.ProcessorCount));
            LimitedConcurrencyLevelTaskScheduler lcts = new LimitedConcurrencyLevelTaskScheduler(maxFileProcessingConcurrency);
            TaskFactory factory = new TaskFactory(lcts);
            return factory;
        }

        /// <summary>
        /// Handles any resource cleanup associated with processing this file.
        /// </summary>
        private void Cleanup(LogFileContext fileContext)
        {
            // Now that we've processed the file, we can delete it.
            try
            {
                File.Delete(fileContext.FilePath);
            }
            catch (Exception ex)
            {
                // Log & swallow exception; cleanup is a nice-to-have, not a need-to-have.
                Log.DebugFormat("Failed to remove processed file '{0}': {1}", fileContext.FilePath, ex.Message);
            }
        }

        /// <summary>
        /// Creates all of the required MongoDB collections that this logset requires.
        /// </summary>
        private void CreateMongoDbCollections()
        {
            var collections = new Dictionary<string, HashSet<string>>();

            ISet<IParser> parsers = parserFactory.GetAllParsers();

            // Stuff collection names & indexes into the dictionary, deduping in the process.
            foreach (var parser in parsers)
            {
                var collectionName = parser.CollectionSchema.CollectionName.ToLowerInvariant();
                IList<string> indexes = parser.CollectionSchema.Indexes;

                if (!collections.ContainsKey(collectionName))
                {
                    if (LogsetDependencyHelper.IsCollectionRequiredForRequest(collectionName, logsharkRequest))
                    {
                        collections.Add(collectionName, new HashSet<string>());
                    }
                }

                // Add indexes.
                if (collections.ContainsKey(collectionName))
                {
                    foreach (var index in indexes)
                    {
                        if (collections.ContainsKey(collectionName))
                        {
                            collections[collectionName].Add(index);
                        }
                    }
                }
            }

            // New up collections & indexes using the dictionary.
            foreach (var collection in collections)
            {
                var collectionName = collection.Key;
                ISet<string> indexes = collection.Value;

                var dbCollection = database.GetCollection<BsonDocument>(collectionName);
                logsharkRequest.RunContext.CollectionsGenerated.Add(collectionName);

                foreach (var index in indexes)
                {
                    var indexKeysBuilder = new IndexKeysDefinitionBuilder<BsonDocument>();
                    CreateIndexOptions indexOptions = new CreateIndexOptions { Sparse = false };
                    dbCollection.Indexes.CreateOne(indexKeysBuilder.Ascending(index), indexOptions);
                }

                // If we are working against a sharded Mongo cluster, we need to explicitly shard each collection.
                MongoConnectionInfo mongoConnectionInfo = logsharkRequest.Configuration.MongoConnectionInfo;
                if (mongoConnectionInfo.ConnectionType == MongoConnectionType.ShardedCluster)
                {
                    MongoAdminUtil.EnableShardingOnCollectionIfNotEnabled(mongoConnectionInfo.GetClient(), logsharkRequest.RunContext.MongoDatabaseName, collectionName);
                }
            }
        }
    }
}