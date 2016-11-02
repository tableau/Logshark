using log4net;
using Logshark.PluginLib;
using Logshark.PluginLib.Logging;
using Logshark.PluginLib.Persistence;
using Logshark.Plugins.Backgrounder.Model;
using Npgsql;
using ServiceStack.OrmLite;
using System.Data;
using System.Reflection;

namespace Logshark.Plugins.Backgrounder.Helpers
{
    internal class BackgrounderPersistenceHelper
    {
        private static readonly ILog Log = PluginLogFactory.GetLogger(Assembly.GetExecutingAssembly(), MethodBase.GetCurrentMethod());

        public static InsertionResult PersistBackgrounderJob(IDbConnection dbConnection, BackgrounderJob backgrounderJob)
        {
            try
            {
                dbConnection.Insert(backgrounderJob);

                if (backgrounderJob.Errors != null && backgrounderJob.Errors.Count > 0)
                {
                    dbConnection.InsertAll(backgrounderJob.Errors);
                }

                if (backgrounderJob.BackgrounderJobDetail != null)
                {
                    if (backgrounderJob.BackgrounderJobDetail is BackgrounderExtractJobDetail)
                    {
                        dbConnection.Insert(backgrounderJob.BackgrounderJobDetail as BackgrounderExtractJobDetail);
                    }

                    if (backgrounderJob.BackgrounderJobDetail is BackgrounderSubscriptionJobDetail)
                    {
                        dbConnection.Insert(backgrounderJob.BackgrounderJobDetail as BackgrounderSubscriptionJobDetail);
                    }
                }

                Log.DebugFormat("Persisted Backgrounder Job '{0}' ({1}).", backgrounderJob.JobId, backgrounderJob.JobType);
                return new InsertionResult
                {
                    SuccessfulInserts = 1,
                    FailedInserts = 0
                };
            }
            catch (PostgresException ex)
            {
                // Log an error only if this isn't a duplicate key exception.
                if (!ex.SqlState.Equals(PluginLibConstants.POSTGRES_ERROR_CODE_UNIQUE_VIOLATION))
                {
                    Log.ErrorFormat("Failed to persist Backgrounder Job '{0}' ({1}): {2}", backgrounderJob.JobId, backgrounderJob.JobType, ex.Message);
                }

                return new InsertionResult
                {
                    SuccessfulInserts = 0,
                    FailedInserts = 1
                };
            }
            catch (NpgsqlException ex)
            {
                Log.ErrorFormat("Failed to persist Backgrounder Job '{0}' ({1}): {2}", backgrounderJob.JobId, backgrounderJob.JobType, ex.Message);

                return new InsertionResult
                {
                    SuccessfulInserts = 0,
                    FailedInserts = 1
                };
            }
        }
    }
}