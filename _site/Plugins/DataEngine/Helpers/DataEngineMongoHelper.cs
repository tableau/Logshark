using log4net;
using Logshark.PluginLib.Helpers;
using Logshark.PluginLib.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Logshark.Plugins.DataEngine.Helpers
{
    public class DataEngineMongoHelper
    {
        public static readonly FilterDefinitionBuilder<BsonDocument> Query = Builders<BsonDocument>.Filter;

        private static readonly ILog Log = PluginLogFactory.GetLogger(Assembly.GetExecutingAssembly(),
                                                                      MethodBase.GetCurrentMethod());

        public static int GetNumberOfWorkers(IMongoCollection<BsonDocument> collection)
        {
            var filter = Query.Exists("worker");
            var workers = collection.Distinct<int>("worker", filter).ToList();
            workers.Sort();
            return workers.Last();
        }

        public static IEnumerable<string> GetDataEngineLogFilesForWorker(int workerId, IMongoCollection<BsonDocument> collection)
        {
            var filter = Query.And(Query.Eq("worker", workerId));
            return collection.Distinct<string>("file", filter).ToList();
        }

        public static Dictionary<int, IList<BsonDocument>> GetQueriesBySessionIdForLogfile(string file, IMongoCollection<BsonDocument> collection)
        {
            Dictionary<int, IList<BsonDocument>> queryLinesBySession = new Dictionary<int, IList<BsonDocument>>();
            var filter = Query.And(
                                Query.Eq("file", file),
                                Query.Or(Query.Regex("message", new BsonRegularExpression("QueryExecute")),
                                         Query.Regex("message", new BsonRegularExpression("StatementPrepare")),
                                         Query.Regex("message", new BsonRegularExpression("StatementExecute"))));

            var cursor = collection.Find(filter).ToCursor();

            foreach (var document in cursor.ToEnumerable())
            {
                string message = BsonDocumentHelper.GetString("message", document);
                if (String.IsNullOrWhiteSpace(message))
                {
                    continue;
                }

                // Get the first space-delimited segment in the message.
                int indexOfSpace = message.IndexOf(' ');
                if (indexOfSpace == -1)
                {
                    var line = BsonDocumentHelper.GetInt("line", document);
                    Log.WarnFormat("Failed to find expected session ID in message '{0}'. ({1}:{2})  Skipping event..", message, file, line);
                    continue;
                }
                string firstMessageSegment = message.Substring(0, indexOfSpace);

                // Get Session ID out of the segment.
                int sessionId = 0;
                bool parsedSessionId = false;
                if (!String.IsNullOrWhiteSpace(firstMessageSegment))
                {
                    string sessionIdString = firstMessageSegment.Replace("Session", "").Replace(":", "");
                    parsedSessionId = Int32.TryParse(sessionIdString, out sessionId);
                }
                if (!parsedSessionId)
                {
                    var line = BsonDocumentHelper.GetInt("line", document);
                    Log.WarnFormat("Failed to parse expected session ID in message '{0}'. ({1}:{2})  Skipping event..", message, file, line);
                    continue;
                }

                if (!queryLinesBySession.ContainsKey(sessionId))
                {
                    queryLinesBySession[sessionId] = new List<BsonDocument>();
                }
                queryLinesBySession[sessionId].Add(document);
            }

            return queryLinesBySession;
        }

        public static IList<BsonDocument> GetDataEngineEventsForLineRange(int workerId, string file, string threadId, int startLine, int endLine, IMongoCollection<BsonDocument> collection)
        {
            var filter = Query.And(Query.Eq("worker", workerId),
                                   Query.Eq("tid", threadId),
                                   Query.Eq("file", file),
                                   Query.Gte("line", startLine),
                                   Query.Lte("line", endLine));

            var sort = Builders<BsonDocument>.Sort.Ascending("line");

            return collection.Find(filter).Sort(sort).ToList();
        }
    }
}