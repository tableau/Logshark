using log4net;
using Npgsql;
using ServiceStack.OrmLite;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Logshark.PluginLib.Persistence
{
    internal class BatchInsertionThread<T> : BaseInsertionThread<T> where T : new()
    {
        protected readonly int maxBatchSize;
        protected readonly ICollection<T> insertionBatch;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override int ItemsPendingInsertion
        {
            get { return insertionBatch.Count; }
        }

        public BatchInsertionThread(IDbConnection dbConnection, int maxBatchSize)
        {
            DbConnection = dbConnection;
            this.maxBatchSize = maxBatchSize;
            insertionBatch = new List<T>();
        }

        protected override void Insert(T item)
        {
            lock (insertionBatch)
            {
                insertionBatch.Add(item);
                if (insertionBatch.Count >= maxBatchSize || persistenceQueue.IsEmpty)
                {
                    InsertAllAsBatch();
                }
            }
        }

        protected void InsertAllAsBatch()
        {
            try
            {
                DbConnection.SaveAll(insertionBatch);
                ItemsPersisted += insertionBatch.Count;
            }
            catch (PostgresException ex)
            {
                if (ex.SqlState.Equals(PluginLibConstants.POSTGRES_ERROR_CODE_UNIQUE_VIOLATION))
                {
                    // We hit a duplicate record in the batch.  Try inserting one at a time so we don't drop any data.
                    InsertAllAsSingleRecords();
                }
                else if (ex.SqlState.Equals(PluginLibConstants.POSTGRES_ERROR_CODE_DEADLOCK_DETECTED))
                {
                    // We encountered a deadlock with another batch.  Try inserting one at a time so we don't drop any data.
                    InsertAllAsSingleRecords();
                }
                else
                {
                    Log.ErrorFormat("Failed to persist record batch into database: {0}", ex.Message);
                }
            }
            catch (NpgsqlException ex)
            {
                {
                    Log.ErrorFormat("Failed to persist record batch into database: {0}", ex.Message);
                }
            }
            finally
            {
                insertionBatch.Clear();
            }
        }

        protected void InsertAllAsSingleRecords()
        {
            foreach (var record in insertionBatch)
            {
                try
                {
                    DbConnection.Insert(record);
                    ItemsPersisted++;
                }
                catch (PostgresException ex)
                {
                    // Log an error only if this isn't a duplicate key exception.
                    if (!ex.SqlState.Equals(PluginLibConstants.POSTGRES_ERROR_CODE_UNIQUE_VIOLATION))
                    {
                        Log.ErrorFormat("Failed to persist single record into database: {0}", ex.Message);
                    }
                }
            }
        }
    }
}