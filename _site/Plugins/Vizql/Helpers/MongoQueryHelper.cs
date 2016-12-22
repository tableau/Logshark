using log4net;
using Logshark.PluginLib.Helpers;
using Logshark.PluginLib.Logging;
using Logshark.Plugins.Vizql.Models;
using Logshark.Plugins.Vizql.Models.Events;
using Logshark.Plugins.Vizql.Models.Events.Caching;
using Logshark.Plugins.Vizql.Models.Events.Compute;
using Logshark.Plugins.Vizql.Models.Events.Connection;
using Logshark.Plugins.Vizql.Models.Events.Error;
using Logshark.Plugins.Vizql.Models.Events.Etc;
using Logshark.Plugins.Vizql.Models.Events.Query;
using Logshark.Plugins.Vizql.Models.Events.Render;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Logshark.Plugins.Vizql.Helpers
{
    public class MongoQueryHelper
    {
        private static readonly IDictionary<string, Type> VizqlEventClassesByKeytype = new Dictionary<string, Type> {
                                                           {"compute-percentages",typeof(VizqlComputePercentages)},
                                                           {"compute-x-axis-descriptor",typeof(VizqlComputeXAxisDescriptor)},
                                                           {"compute-x-set-interp",typeof(VizqlComputeXSetInterp)},
                                                           {"compute-y-axis-descriptor",typeof(VizqlComputeYAxisDescriptor)},
                                                           {"compute-y-set-interp",typeof(VizqlComputeYSetInterp)},
                                                           {"construct-protocol",typeof(VizqlConstructProtocol)},
                                                           {"construct-protocol-group",typeof(VizqlConstructProtocolGroup)},
                                                           {"dll-version-info",typeof(VizqlDllVersionInfo)},
                                                           {"ds-interpret-metadata", typeof(VizqlDsInterpretMetadata)},
                                                           {"ds-connect", typeof(VizqlDsConnect)},
                                                           {"ec-drop",typeof(VizqlEcDrop)},
                                                           {"ec-load",typeof(VizqlEcLoad)},
                                                           {"ec-store",typeof(VizqlEcStore)},
                                                           {"end-compute-quick-filter-state",typeof(VizqlEndComputeQuickFilterState)},
                                                           {"end-data-interpreter",typeof(VizqlEndDataInterpreter)},
                                                           {"end-partition-interpreter",typeof(VizqlEndPartitionInterpreter)},
                                                           {"end-prepare-primary-mapping-table",typeof(VizqlEndPreparePrimaryMappingTable)},
                                                           {"end-prepare-quick-filter-queries",typeof(VizqlEndPrepareQuickFilterQueries)},
                                                           {"end-query",typeof(VizqlEndQuery)},
                                                           {"end-sql-temp-table-tuples-create",typeof(VizqlEndSqlTempTableTuplesCreate)},
                                                           {"end-update-sheet",typeof(VizqlEndUpdateSheet)},
                                                           {"end-visual-interpreter",typeof(VizqlEndVisualInterpreter)},
                                                           {"eqc-load",typeof(VizqlEqcLoad)},
                                                           {"eqc-store",typeof(VizqlEqcStore)},
                                                           {"generate-axis-encodings",typeof(VizqlGenerateAxisEncodings)},
                                                           {"msg",typeof(VizqlMessage)},
                                                           {"process_query", typeof(VizqlProcessQuery)},
                                                           {"qp-batch-summary",typeof(VizqlQpBatchSummary)},
                                                           {"qp-query-end",typeof(VizqlQpQueryEnd)},
                                                           {"set-collation",typeof(VizqlSetCollation)}};

        private static readonly ISet<string> VizqlDesktopSupportedKeytypes = new HashSet<string> {
                                                           "compute-percentages",
                                                           "compute-x-axis-descriptor",
                                                           "compute-x-set-interp",
                                                           "compute-y-axis-descriptor",
                                                           "compute-y-set-interp",
                                                           "construct-protocol",
                                                           "construct-protocol-group",
                                                           "dll-version-info",
                                                           "ds-interpret-metadata",
                                                           "ds-connect",
                                                           "ec-drop",
                                                           "ec-load",
                                                           "ec-store",
                                                           "end-compute-quick-filter-state",
                                                           "end-data-interpreter",
                                                           "end-partition-interpreter",
                                                           "end-prepare-primary-mapping-table",
                                                           "end-prepare-quick-filter-queries",
                                                           "end-query",
                                                           "end-sql-temp-table-tuples-create",
                                                           "end-update-sheet",
                                                           "end-visual-interpreter",
                                                           "eqc-load",
                                                           "eqc-store",
                                                           "etc",
                                                           "generate-axis-encodings",
                                                           "msg",
                                                           "process_query",
                                                           "qp-batch-summary",
                                                           "qp-query-end",
                                                           "set-collation"};

        private static readonly ISet<string> VizqlServerSupportedKeytypes = new HashSet<string> {
                                                           "compute-percentages",
                                                           "compute-x-axis-descriptor",
                                                           "compute-x-set-interp",
                                                           "compute-y-axis-descriptor",
                                                           "compute-y-set-interp",
                                                           "construct-protocol",
                                                           "construct-protocol-group",
                                                           "ec-drop",
                                                           "ec-load",
                                                           "ec-store",
                                                           "end-compute-quick-filter-state",
                                                           "end-data-interpreter",
                                                           "end-partition-interpreter",
                                                           "end-prepare-primary-mapping-table",
                                                           "end-prepare-quick-filter-queries",
                                                           "end-query",
                                                           "end-sql-temp-table-tuples-create",
                                                           "end-update-sheet",
                                                           "end-visual-interpreter",
                                                           "eqc-load",
                                                           "eqc-store",
                                                           "generate-axis-encodings",
                                                           "qp-batch-summary",
                                                           "qp-query-end"};

        private static readonly FilterDefinitionBuilder<BsonDocument> Query = Builders<BsonDocument>.Filter;
        private static readonly ILog Log = PluginLogFactory.GetLogger(Assembly.GetExecutingAssembly(), MethodBase.GetCurrentMethod());

        public static ICollection<string> GetAllUniqueServerSessionIds(IMongoCollection<BsonDocument> collection)
        {
            ISet<string> uniqueSessionIds = new HashSet<string>();

            var filter = Builders<BsonDocument>.Filter.Empty;
            var distinctSessionCursor = collection.Distinct<string>("sess", filter).ToEnumerable();

            foreach (var session in distinctSessionCursor)
            {
                if (session != null && !session.Equals("default"))
                {
                    uniqueSessionIds.Add(session);
                }
            }

            return uniqueSessionIds;
        }

        public static ICollection<VizqlDesktopSession> GetAllDesktopSessions(IMongoCollection<BsonDocument> collection, Guid logsetHash)
        {
            IList<VizqlDesktopSession> sessions = new List<VizqlDesktopSession>();
            foreach (var file in GetAllUniqueFiles(collection))
            {
                foreach (var pid in GetAllUniquePidsFromFile(file, collection))
                {
                    try
                    {
                        VizqlDesktopSession vizqlDesktopSession = GetDesktopSession(file, pid, collection, logsetHash);
                        if (vizqlDesktopSession != null)
                        {
                            sessions.Add(vizqlDesktopSession);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Exception constructing Desktop Session for pid " + pid + ":" + ex);
                    }
                }
            }

            return sessions;
        }

        public static VizqlDesktopSession GetDesktopSession(string file, int pid, IMongoCollection<BsonDocument> collection, Guid logsetHash)
        {
            var startupInfoQuery = Query.And(Query.Eq("pid", pid),
                                               Query.Eq("file", file),
                                               Query.Eq("k", "startup-info"));

            BsonDocument document = collection.Find(startupInfoQuery).ToList().First();

            VizqlDesktopSession session = new VizqlDesktopSession(document, logsetHash);

            return session;
        }

        public static ICollection<int> GetAllUniquePidsFromFile(string file, IMongoCollection<BsonDocument> collection)
        {
            var filter = Query.Eq("file", file);
            return collection.Distinct<int>("pid", filter).ToList();
        }

        public static ICollection<string> GetAllUniqueFiles(IMongoCollection<BsonDocument> collection)
        {
            var filter = Builders<BsonDocument>.Filter.Empty;
            return collection.Distinct<string>("file", filter).ToList();
        }

        public static long GetUniqueSessionIdCount(IEnumerable<IMongoCollection<BsonDocument>> collections)
        {
            return collections.Sum(collection => GetAllUniqueServerSessionIds(collection).Count);
        }

        public static VizqlServerSession GetServerSession(string sessionId, IMongoCollection<BsonDocument> collection, Guid logsetHash)
        {
            var sessionQuery = Query.Eq("sess", sessionId);

            // Only include fields which we actually need to new up the session.
            var projection = Builders<BsonDocument>.Projection.Include("ts")
                                                              .Include("sess")
                                                              .Include("site")
                                                              .Include("file")
                                                              .Include("user")
                                                              .Include("worker");

            BsonDocument firstEvent = collection.Find(sessionQuery).Project(projection).Sort(Builders<BsonDocument>.Sort.Ascending("ts")).Limit(1).First();
            BsonDocument lastEvent = collection.Find(sessionQuery).Project(projection).Sort(Builders<BsonDocument>.Sort.Descending("ts")).Limit(1).First();

            string processName = collection.CollectionNamespace.CollectionName.Split('_')[0];

            VizqlSession session = new VizqlServerSession(firstEvent, lastEvent, GetWorkbookForSession(sessionId, collection), processName, GetBootstrapRequestIdForSession(sessionId, collection), logsetHash);

            return session as VizqlServerSession;
        }

        public static string GetBootstrapRequestIdForSession(string sessionId, IMongoCollection<BsonDocument> collection)
        {
            try
            {
                var bootstrapRequestQuery = Query.And(Query.Eq("sess", sessionId),
                                                      Query.Eq("k", "lock-session"),
                                                      Query.Ne("v.workbook", "Book1"));

                BsonDocument bootstrapRequestEvent = collection.Find(bootstrapRequestQuery).Sort(Builders<BsonDocument>.Sort.Ascending("ts")).Limit(1).First();

                return BsonDocumentHelper.GetString("req", bootstrapRequestEvent);
            }
            catch
            {
                return null;
            }
        }

        public static string GetWorkbookForSession(string sessionId, IMongoCollection<BsonDocument> collection)
        {
            try
            {
                var unlockSessionQuery = Query.And(Query.Eq("sess", sessionId),
                                                   Query.Eq("k", "unlock-session"));

                BsonDocument unlockSession = collection.Find(unlockSessionQuery).Sort(Builders<BsonDocument>.Sort.Ascending("ts")).Limit(1).First();
                BsonDocument values = BsonDocumentHelper.GetValuesStruct(unlockSession);
                return BsonDocumentHelper.GetString("workbook", values);
            }
            catch
            {
                return null;
            }
        }

        public static VizqlSession AppendAllSessionEvents(VizqlSession session, IMongoCollection<BsonDocument> collection)
        {
            //Errors
            AppendErrorEvents(session, collection);

            AppendPerformanceEvents(session, collection);

            return session;
        }

        public static VizqlSession AppendPerformanceEvents(VizqlSession session, IMongoCollection<BsonDocument> collection)
        {
            foreach (var keyType in VizqlEventClassesByKeytype.Keys)
            {
                AppendEventsForKeyType(session, keyType, collection);
            }

            if (session is VizqlDesktopSession)
            {
                AppendEtcEventsForSession(session, collection);
            }

            return session;
        }

        public static IList<VizqlDllVersionInfo> GetDllVersionInfoEvents(IMongoCollection<BsonDocument> collection)
        {
            IList<VizqlDllVersionInfo> dllVersionInfoEvents = new List<VizqlDllVersionInfo>();
            foreach (var dllVersionInfoDocument in GetEventsForKey("dll-version-info", collection))
            {
                dllVersionInfoEvents.Add(new VizqlDllVersionInfo(dllVersionInfoDocument));
            }

            return dllVersionInfoEvents;
        }

        public static IAsyncCursor<BsonDocument> GetCursorForKey(string keyType, IMongoCollection<BsonDocument> collection)
        {
            var keyQuery = Query.Eq("k", keyType);

            return collection.Find(keyQuery).ToCursor();
        }

        public static VizqlSession AppendErrorEvents(VizqlSession session, IMongoCollection<BsonDocument> collection)
        {
            IEnumerable<BsonDocument> errors = GetErrorsForSession(session, collection);

            foreach (BsonDocument logline in errors)
            {
                string message = logline.GetValue("v").AsString;
                if (!message.StartsWith("Exception '' while executing command"))
                {
                    session.AppendEvent(new VizqlErrorEvent(logline));
                }
            }

            ICollection<BsonDocument> fatals = GetFatalsForSession(session, collection).ToList();

            if (fatals.Any())
            {
                string message = AssembleStackWalk(fatals);
                session.AppendEvent(new VizqlErrorEvent(fatals.First(), message));
            }

            return session;
        }

        public static VizqlSession AppendEtcEventsForSession(VizqlSession session, IMongoCollection<BsonDocument> collection)
        {
            var filter = Query.Nin("k", VizqlEventClassesByKeytype.Keys);
            var unsupportedKeyTypes = collection.Distinct<string>("k", filter).ToEnumerable();

            foreach (var keyType in unsupportedKeyTypes)
            {
                IEnumerable<BsonDocument> documents;
                if (session is VizqlServerSession)
                {
                    documents = GetEventsForKeyBySession(session.VizqlSessionId, keyType, collection);
                }
                else if (session is VizqlDesktopSession)
                {
                    VizqlDesktopSession desktopSession = session as VizqlDesktopSession;
                    documents = GetEventsForKeyByPid(desktopSession.ProcessId, keyType, collection);
                }
                else
                {
                    throw new Exception("VizqlSession must be of type Server or Desktop");
                }

                foreach (var document in documents)
                {
                    try
                    {
                        session.AppendEvent(new VizqlEtc(document));
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Unable to create VizqlEtc record for " + session.VizqlSessionId + " for keyType " + keyType + ":" + ex.Message);
                    }
                }
            }

            return session;
        }

        public static VizqlSession AppendEventsForKeyType(VizqlSession session, string keyType, IMongoCollection<BsonDocument> collection)
        {
            IEnumerable<BsonDocument> documentList;
            if (session is VizqlServerSession)
            {
                if (!VizqlServerSupportedKeytypes.Contains(keyType))
                {
                    return session;
                }
                else
                {
                    documentList = GetEventsForKeyBySession(session.VizqlSessionId, keyType, collection);
                }
            }
            else if (session is VizqlDesktopSession)
            {
                if (!VizqlDesktopSupportedKeytypes.Contains(keyType))
                {
                    return session;
                }
                else
                {
                    VizqlDesktopSession desktopSession = session as VizqlDesktopSession;
                    documentList = GetEventsForKeyByPid(desktopSession.ProcessId, keyType, collection);
                }
            }
            else
            {
                throw new Exception("VizqlSession not of type Desktop or Server!");
            }

            foreach (var document in documentList)
            {
                try
                {
                    Object[] args = { document };
                    Type t = VizqlEventClassesByKeytype[keyType];
                    VizqlEvent vizqlEvent = (VizqlEvent)Activator.CreateInstance(t, args);
                    session.AppendEvent(vizqlEvent);
                }
                catch (Exception ex)
                {
                    Log.ErrorFormat("Exception processing {0} events on session {1}: {2}", keyType, session.VizqlSessionId, ex);
                }
            }

            return session;
        }

        public static IEnumerable<BsonDocument> GetEventsForKeyBySession(string sessionId, string keyType, IMongoCollection<BsonDocument> collection)
        {
            if (String.IsNullOrEmpty(sessionId))
            {
                throw new Exception("SessionId cannot be null");
            }
            else
            {
                var keyQuery = Query.And(Query.Eq("sess", sessionId),
                                         Query.Eq("k", keyType));

                return collection.Find(keyQuery).ToList();
            }
        }

        public static IEnumerable<BsonDocument> GetEventsForKeyByPid(int pid, string keyType, IMongoCollection<BsonDocument> collection)
        {
            var keyQuery = Query.And(Query.Eq("pid", pid),
                                     Query.Eq("k", keyType));

            return collection.Find(keyQuery).ToList();
        }

        public static IEnumerable<BsonDocument> GetEventsForKey(string keyType, IMongoCollection<BsonDocument> collection)
        {
            var keyQuery = Query.Eq("k", keyType);

            return collection.Find(keyQuery).ToList();
        }

        private static IEnumerable<BsonDocument> GetSeveritiesforSession(VizqlSession session, string severity, IMongoCollection<BsonDocument> collection)
        {
            FilterDefinition<BsonDocument> severityQuery;
            if (session is VizqlServerSession)
            {
                severityQuery = Query.And(Query.Eq("sess", session.VizqlSessionId),
                                          Query.Eq("sev", severity));
            }
            else if (session is VizqlDesktopSession)
            {
                VizqlDesktopSession desktopSession = session as VizqlDesktopSession;
                severityQuery = Query.And(Query.Eq("pid", desktopSession.ProcessId),
                                          Query.Eq("sev", severity));
            }
            else
            {
                throw new Exception("VizqlSession not of type Server or Desktop");
            }

            return collection.Find(severityQuery).ToList();
        }

        public static IEnumerable<BsonDocument> GetErrorsForSession(VizqlSession session, IMongoCollection<BsonDocument> collection)
        {
            return GetSeveritiesforSession(session, "error", collection);
        }

        public static IEnumerable<BsonDocument> GetFatalsForSession(VizqlSession session, IMongoCollection<BsonDocument> collection)
        {
            return GetSeveritiesforSession(session, "fatal", collection);
        }

        public static string AssembleStackWalk(IEnumerable<BsonDocument> fatals)
        {
            StringBuilder builder = new StringBuilder();
            foreach (BsonDocument logLine in fatals)
            {
                string message = logLine.GetValue("v").AsString;
                if (message.StartsWith("0x000"))
                {
                    IEnumerable<string> segments = message.Split(' ');

                    IEnumerable<string> sanitizedMessage = segments.Where(segment => !segment.StartsWith("0x000"));

                    message = String.Join(" ", sanitizedMessage);
                }

                builder.AppendLine(message);

                if (message.StartsWith("End of call stack for LogicAssert"))
                {
                    return builder.ToString();
                }
            }

            return builder.ToString();
        }
    }
}