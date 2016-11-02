using log4net;
using Logshark.PluginLib.Logging;
using Logshark.PluginLib.Persistence;
using Logshark.Plugins.Vizql.Models;
using ServiceStack.OrmLite;
using System;
using System.Data;
using System.Reflection;
using Logshark.Plugins.Vizql.Models.Events.Query;

namespace Logshark.Plugins.Vizql.Helpers
{
    public class DesktopSessionPersistenceHelper
    {
        private static readonly ILog Log = PluginLogFactory.GetLogger(Assembly.GetExecutingAssembly(), MethodBase.GetCurrentMethod());

        public static InsertionResult PersistSession(IDbConnection database, VizqlDesktopSession currentSession)
        {
            try
            {
                database.Insert(currentSession);

                //Error
                database.InsertAll(currentSession.ErrorEvents);

                //Performance
                database.InsertAll(currentSession.PerformanceEvents);

                //Connection
                database.InsertAll(currentSession.ConstructProtocolEvents);
                database.InsertAll(currentSession.ConstructProtocolGroupEvents);
                database.InsertAll(currentSession.DsConnectEvents);

                //Compute
                database.InsertAll(currentSession.EndComputeQuickFilterStateEvents);

                //Render
                database.InsertAll(currentSession.EndUpdateSheetEvents);

                //Caching
                database.InsertAll(currentSession.EcDropEvents);
                database.InsertAll(currentSession.EcStoreEvents);
                database.InsertAll(currentSession.EcLoadEvents);
                database.InsertAll(currentSession.EqcStoreEvents);
                database.InsertAll(currentSession.EqcLoadEvents);

                //Message
                database.InsertAll(currentSession.MessageEvents);

                //Etc
                database.InsertAll(currentSession.EtcEvents);

                //Query
                database.InsertAll(currentSession.DsInterpretMetadataEvents);
                database.InsertAll(currentSession.EndQueryEvents);
                database.InsertAll(currentSession.QpQueryEndEvents);
                database.InsertAll(currentSession.EndPrepareQuickFilterQueriesEvents);
                database.InsertAll(currentSession.EndSqlTempTableTuplesCreateEvents);
                database.InsertAll(currentSession.SetCollationEvents);
                database.InsertAll(currentSession.ProcessQueryEvents);

                foreach (VizqlQpBatchSummary qpBatchSummaryEvent in currentSession.QpBatchSummaryEvents)
                {
                    database.Insert(qpBatchSummaryEvent);
                    database.InsertAll(qpBatchSummaryEvent.QpBatchSummaryJobs);
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