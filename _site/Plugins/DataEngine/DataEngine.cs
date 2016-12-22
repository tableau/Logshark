using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Helpers;
using Logshark.PluginLib.Model;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginLib.Persistence;
using Logshark.PluginLib.TaskSchedulers;
using Logshark.Plugins.DataEngine.Helpers;
using Logshark.Plugins.DataEngine.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Logshark.Plugins.DataEngine
{
    public class DataEngine : BaseWorkbookCreationPlugin, IDesktopPlugin, IServerPlugin
    {
        private PluginResponse pluginResponse;
        private Guid logsetHash;

        private IMongoCollection<BsonDocument> dataengineCollection;
        private static readonly string dataengineCollectionName = "dataengine";

        private IPersister<DataengineEvent> dataenginePersister;

        public override ISet<string> CollectionDependencies
        {
            get
            {
                return new HashSet<string>
                {
                    dataengineCollectionName
                };
            }
        }

        public override ICollection<string> WorkbookNames
        {
            get
            {
                return new List<string>
                {
                    "DataEngine.twb"
                };
            }
        }

        public override IPluginResponse Execute(IPluginRequest pluginRequest)
        {
            pluginResponse = CreatePluginResponse();
            logsetHash = pluginRequest.LogsetHash;

            GetOutputDatabaseConnection().CreateOrMigrateTable<DataengineEvent>();

            dataenginePersister = GetConcurrentBatchPersister<DataengineEvent>(pluginRequest);

            dataengineCollection = MongoDatabase.GetCollection<BsonDocument>(dataengineCollectionName);

            using (GetPersisterStatusWriter<DataengineEvent>(dataenginePersister))
            {
                ProcessDataEngineLogs(dataengineCollection);
                dataenginePersister.Shutdown();
            }
            Log.Info("Finished processing Data Engine events!");

            // Check if we persisted any data.
            if (!PersistedData())
            {
                Log.Info("Failed to persist any data from Data Engine logs!");
                pluginResponse.GeneratedNoData = true;
            }

            return pluginResponse;
        }

        protected void ProcessDataEngineLogs(IMongoCollection<BsonDocument> collection)
        {
            Log.Info("Queueing Data Engine events for processing..");

            List<Task> tasks = new List<Task>();

            LimitedConcurrencyLevelTaskScheduler lcts = new LimitedConcurrencyLevelTaskScheduler(dataenginePersister.GetPoolSize());
            TaskFactory factory = new TaskFactory(lcts);

            using (GetTaskStatusWriter(tasks, "Data Engine processing"))
            {
                int numWorkers = DataEngineMongoHelper.GetNumberOfWorkers(dataengineCollection);
                int currWorker = 0;
                while (currWorker <= numWorkers)
                {
                    var fileNames = DataEngineMongoHelper.GetDataEngineLogFilesForWorker(currWorker, dataengineCollection);
                    foreach (var fileName in fileNames)
                    {
                        IDictionary<int, IList<BsonDocument>> queriesBySession = DataEngineMongoHelper.GetQueriesBySessionIdForLogfile(fileName, dataengineCollection);

                        foreach (var session in queriesBySession.Keys)
                        {
                            tasks.Add(factory.StartNew(() => PersistSessionInformation(session, queriesBySession[session])));
                        }
                    }
                    currWorker++;
                }

                Task.WaitAll(tasks.ToArray());
            }
        }

        private void PersistSessionInformation(int sessionId, IList<BsonDocument> sessionLines)
        {
            IList<DataengineEvent> queryExecuteEvents = GetAllQueryExecuteEvents(sessionId, sessionLines);

            IDictionary<int, StatementPrepareEvent> statementPrepareEvents = GetAllStatementPrepareEvents(sessionLines);
            IList<DataengineEvent> statementExecuteEvents = GetAllStatementExecuteEvents(sessionId, sessionLines, statementPrepareEvents);

            foreach (var queryExecuteEvent in queryExecuteEvents)
            {
                dataenginePersister.Enqueue(queryExecuteEvent);
            }

            foreach (var statementExecuteEvent in statementExecuteEvents)
            {
                dataenginePersister.Enqueue(statementExecuteEvent);
            }
        }

        private IDictionary<int, StatementPrepareEvent> GetAllStatementPrepareEvents(IList<BsonDocument> sessionLines)
        {
            IDictionary<int, StatementPrepareEvent> statementPrepareEvents = new Dictionary<int, StatementPrepareEvent>();
            Queue<BsonDocument> lineQueue = new Queue<BsonDocument>();

            foreach (var line in sessionLines)
            {
                string message = BsonDocumentHelper.GetString("message", line);
                if (message.Contains("StatementPrepare"))
                {
                    lineQueue.Enqueue(line);
                }
            }

            lineQueue = SortByLogLine(lineQueue);

            while (lineQueue.Count >= 2)
            {
                StatementPrepareEvent prepareEvent = GetStatementPrepareEvent(lineQueue.Dequeue(), lineQueue.Dequeue());
                if (prepareEvent.Success)
                {
                    statementPrepareEvents[prepareEvent.Guid] = prepareEvent;
                }
            }

            return statementPrepareEvents;
        }

        private IList<DataengineEvent> GetAllQueryExecuteEvents(int sessionId, IList<BsonDocument> sessionLines)
        {
            IList<DataengineEvent> queryExecuteEvents = new List<DataengineEvent>();
            Queue<BsonDocument> lineQueue = new Queue<BsonDocument>();

            foreach (var line in sessionLines)
            {
                string message = BsonDocumentHelper.GetString("message", line);
                if (message.Contains("QueryExecute"))
                {
                    lineQueue.Enqueue(line);
                }
            }

            lineQueue = SortByLogLine(lineQueue);

            while (lineQueue.Count >= 2)
            {
                try
                {
                    DataengineEvent queryExecuteEvent = GetDataEngineEvent(sessionId, "QueryExecute", lineQueue.Dequeue(), lineQueue.Dequeue());
                    // Filter out all (database null) queries, which are used for closing a connection.
                    if (!queryExecuteEvent.Query.Equals("(database null)"))
                    {
                        queryExecuteEvents.Add(queryExecuteEvent);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Problem gathering QueryExecute event data on session:" + sessionId, ex);
                }
            }

            return queryExecuteEvents;
        }

        private IList<DataengineEvent> GetAllStatementExecuteEvents(int sessionId, IList<BsonDocument> sessionLines, IDictionary<int, StatementPrepareEvent> statementPrepareEvents)
        {
            IList<DataengineEvent> statementExecuteEvents = new List<DataengineEvent>();
            Queue<BsonDocument> lineQueue = new Queue<BsonDocument>();

            foreach (var line in sessionLines)
            {
                string message = BsonDocumentHelper.GetString("message", line);
                if (message.Contains("StatementExecute"))
                {
                    lineQueue.Enqueue(line);
                }
            }

            lineQueue = SortByLogLine(lineQueue);

            while (lineQueue.Count >= 2)
            {
                try
                {
                    BsonDocument startEvent = lineQueue.Dequeue();
                    BsonDocument endEvent = lineQueue.Dequeue();
                    DataengineEvent statementExecuteEvent = GetDataEngineEvent(sessionId, "StatementExecute", startEvent, endEvent);

                    int statementGuid = GetStatementGuidFromLogLine(startEvent);
                    StatementPrepareEvent prepareEvent = statementPrepareEvents[statementGuid];
                    string queryText = prepareEvent.Query;

                    statementExecuteEvent.Query = queryText;
                    statementExecuteEvents.Add(statementExecuteEvent);
                }
                catch (Exception ex)
                {
                    Log.ErrorFormat("Problem gathering StatementExecute event data on session '{0}': {1}", sessionId, ex.Message);
                }
            }

            return statementExecuteEvents;
        }

        private int GetStatementGuidFromLogLine(BsonDocument bsonDocument)
        {
            string message = BsonDocumentHelper.GetString("message", bsonDocument);
            if (message.Contains("stmt_guid="))
            {
                int stmtGuid;
                string stmtGuidString = message.Split(' ').Last().Split('=')[1];
                if (Int32.TryParse(stmtGuidString, out stmtGuid))
                {
                    return stmtGuid;
                }
            }

            throw new Exception("stmt_guid not on this log line");
        }

        private DataengineEvent GetDataEngineEvent(int sessionGuid, string eventType, BsonDocument startEvent, BsonDocument endEvent)
        {
            DataengineEvent dataEngineEvent = new DataengineEvent
            {
                SessionGuid = sessionGuid,
                ThreadId = Int32.Parse(BsonDocumentHelper.GetString("tid", startEvent)),
                Timestamp = BsonDocumentHelper.GetDateTime("ts", endEvent),
                Worker = BsonDocumentHelper.GetInt("worker", startEvent),
                LineNumber = BsonDocumentHelper.GetInt("line", endEvent),
                EventType = eventType
            };

            foreach (var sessionEvent in GetEventsForRange(startEvent, endEvent))
            {
                string message = BsonDocumentHelper.GetString("message", sessionEvent);
                if (!message.Contains(eventType) && !message.StartsWith("Compiling query"))
                {
                    dataEngineEvent.Query = String.Format("{0} {1}", dataEngineEvent.Query, message);
                }

                if (message.StartsWith("Compiling query with Memory Budget="))
                {
                    string memoryBudgetString = message.Replace("Compiling query with Memory Budget=", "");
                    Int64 memoryBudget;
                    if (Int64.TryParse(memoryBudgetString, out memoryBudget))
                    {
                        dataEngineEvent.MemoryBudget = memoryBudget;
                    }
                }

                if (message.Contains(eventType + ": OK"))
                {
                    dataEngineEvent.Success = true;

                    var messageChunks = message.Split(',').Select(s => s.Trim());
                    foreach (var chunk in messageChunks)
                    {
                        if (chunk.StartsWith("Elapsed time:"))
                        {
                            dataEngineEvent.ElapsedTime = ExtractTime(chunk, "Elapsed time:");
                        }

                        if (chunk.StartsWith("Compilation time:"))
                        {
                            dataEngineEvent.CompilationTime = ExtractTime(chunk, "Compilation time:");
                        }

                        if (chunk.StartsWith("Execution time:"))
                        {
                            dataEngineEvent.ExecutionTime = ExtractTime(chunk, "Execution time:");
                        }

                        if (chunk.StartsWith("n_columns="))
                        {
                            int columns;
                            if (Int32.TryParse(chunk.Split(' ').Last().Split('=')[1], out columns))
                            {
                                dataEngineEvent.Columns = columns;
                            }
                        }
                    }
                }

                if (message.Contains(eventType + ": FAILED"))
                {
                    dataEngineEvent.Success = false;

                    var messageChunks = message.Split(',').Select(s => s.Trim());
                    foreach (var chunk in messageChunks)
                    {
                        if (chunk.StartsWith("error="))
                        {
                            dataEngineEvent.Error = chunk.Replace("error=", "");
                        }
                    }
                }
            }

            // Trim unneccessary whitespace.
            if (!String.IsNullOrEmpty(dataEngineEvent.Query))
            {
                dataEngineEvent.Query = dataEngineEvent.Query.Trim();
            }

            dataEngineEvent.LogsetHash = logsetHash;
            dataEngineEvent.EventHash = HashHelper.GenerateHashGuid(dataEngineEvent.SessionGuid, dataEngineEvent.ThreadId, dataEngineEvent.Timestamp, dataEngineEvent.LineNumber);

            return dataEngineEvent;
        }

        private double? ExtractTime(string timeString, string prefix, string suffix = "s")
        {
            var match = Regex.Match(timeString, String.Format(@"{0}(?<time>[\d|\.]+?){1}.*", prefix, suffix), RegexOptions.ExplicitCapture);
            if (match.Success)
            {
                double timeValue;
                if (Double.TryParse(match.Groups["time"].Value, out timeValue))
                {
                    return timeValue;
                }
            }

            return null;
        }

        private IEnumerable<BsonDocument> GetEventsForRange(BsonDocument startEvent, BsonDocument endEvent)
        {
            int workerId = BsonDocumentHelper.GetInt("worker", startEvent);
            string tid = BsonDocumentHelper.GetString("tid", startEvent);
            string file = BsonDocumentHelper.GetString("file", startEvent);
            int startLine = BsonDocumentHelper.GetInt("line", startEvent);
            int endLine = BsonDocumentHelper.GetInt("line", endEvent);

            return DataEngineMongoHelper.GetDataEngineEventsForLineRange(workerId, file, tid, startLine, endLine, dataengineCollection);
        }

        private Queue<BsonDocument> SortByLogLine(Queue<BsonDocument> unsortedList)
        {
            return new Queue<BsonDocument>(unsortedList.OrderBy(doc => BsonDocumentHelper.GetInt("line", doc)).ToList());
        }

        private StatementPrepareEvent GetStatementPrepareEvent(BsonDocument startEvent, BsonDocument endEvent)
        {
            StatementPrepareEvent statementPrepareEvent = new StatementPrepareEvent();

            // Retrieve each associated event.
            var sessionEvents = GetEventsForRange(startEvent, endEvent).ToList();
            if (!sessionEvents.Any())
            {
                return statementPrepareEvent;
            }

            // Process each associated event.
            foreach (var sessionEvent in sessionEvents)
            {
                string message = BsonDocumentHelper.GetString("message", sessionEvent);
                if (String.IsNullOrWhiteSpace(message))
                {
                    continue;
                }

                if (!message.Contains("StatementPrepare") && !message.StartsWith("Compiling query"))
                {
                    if (String.IsNullOrWhiteSpace(statementPrepareEvent.Query))
                    {
                        statementPrepareEvent.Query = message.Trim();
                    }
                    else
                    {
                        statementPrepareEvent.Query = String.Format("{0} {1}", statementPrepareEvent.Query, message.Trim());
                    }
                }

                if (message.Contains("stmt_guid="))
                {
                    var guidSegments = message.Split(' ').Last().Split('=');
                    if (guidSegments.Length > 1)
                    {
                        int guid;
                        if (Int32.TryParse(guidSegments[1], out guid))
                        {
                            statementPrepareEvent.Guid = guid;
                        }
                    }
                }

                if (message.Contains("StatementPrepare: OK"))
                {
                    statementPrepareEvent.Success = true;
                }

                if (message.Contains("StatementPrepare: FAILED"))
                {
                    statementPrepareEvent.Success = false;
                    statementPrepareEvent.Error = message.Replace("StatementPrepare: FAILED, error=", "");
                }
            }

            return statementPrepareEvent;
        }
    }
}