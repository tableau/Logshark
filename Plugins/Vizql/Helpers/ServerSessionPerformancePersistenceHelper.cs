using log4net;
using Logshark.PluginLib;
using Logshark.PluginLib.Logging;
using Logshark.PluginLib.Persistence;
using Logshark.PluginModel.Model;
using Logshark.Plugins.Vizql.Models;
using Logshark.Plugins.Vizql.Models.Events.Query;
using Npgsql;
using ServiceStack.OrmLite;
using System;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Logshark.Plugins.Vizql.Helpers
{
    public static class ServerSessionPerformancePersistenceHelper
    {
        private static readonly string MaxQueryLengthArgumentKey = "VizqlServerPerformance.MaxQueryLength";
        private static readonly int DefaultMaxQueryLength = 10000;

        private static readonly ILog Log = PluginLogFactory.GetLogger(Assembly.GetExecutingAssembly(), MethodBase.GetCurrentMethod());

        public static InsertionResult PersistSession(IPluginRequest pluginRequest, IDbConnection database, VizqlServerSession currentSession)
        {
            try
            {
                try
                {
                    database.Insert(currentSession);
                    database.InsertAll(currentSession.ErrorEvents);
                }
                catch (PostgresException ex)
                {
                    // We now use these tables in both VizqlServer and VizqlServerPerformance.
                    // If someone runs both we need to swallow any duplicates exceptions that may arise.
                    if (!ex.SqlState.Equals(PluginLibConstants.POSTGRES_ERROR_CODE_UNIQUE_VIOLATION))
                    {
                        throw;
                    }
                }

                // Performance
                database.InsertAll(currentSession.PerformanceEvents);

                // Connection
                database.InsertAll(currentSession.ConstructProtocolEvents);
                database.InsertAll(currentSession.ConstructProtocolGroupEvents);

                // Compute
                database.InsertAll(currentSession.EndComputeQuickFilterStateEvents);

                // Render
                database.InsertAll(currentSession.EndUpdateSheetEvents);

                // Caching
                database.InsertAll(currentSession.EcDropEvents);
                database.InsertAll(currentSession.EcStoreEvents);
                database.InsertAll(currentSession.EcLoadEvents);
                database.InsertAll(currentSession.EqcStoreEvents);
                database.InsertAll(currentSession.EqcLoadEvents);

                // Query
                int maxQueryLength = VizqlPluginArgumentHelper.GetMaxQueryLength(pluginRequest, MaxQueryLengthArgumentKey, DefaultMaxQueryLength);
                database.InsertAll(currentSession.EndQueryEvents.Select(queryEvent => queryEvent.WithTruncatedQueryText(maxQueryLength)));
                database.InsertAll(currentSession.QpQueryEndEvents);
                database.InsertAll(currentSession.EndPrepareQuickFilterQueriesEvents);
                database.InsertAll(currentSession.EndSqlTempTableTuplesCreateEvents);
                foreach (VizqlQpBatchSummary qpBatchSummaryEvent in currentSession.QpBatchSummaryEvents)
                {
                    database.Insert(qpBatchSummaryEvent);
                    database.InsertAll(qpBatchSummaryEvent.QpBatchSummaryJobs.Select(queryEvent => queryEvent.WithTruncatedQueryText(maxQueryLength)));
                }

                Log.DebugFormat("Persisted session {0}", currentSession.VizqlSessionId);
                return new InsertionResult
                {
                    SuccessfulInserts = 1,
                    FailedInserts = 0
                };
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Failed to persist session '{0}': {1}", currentSession.VizqlSessionId, ex.Message);
                return new InsertionResult
                {
                    SuccessfulInserts = 0,
                    FailedInserts = 1
                };
            }
        }
    }
}