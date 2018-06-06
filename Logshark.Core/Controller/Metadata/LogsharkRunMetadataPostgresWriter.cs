using log4net;
using Logshark.ConnectionModel.Postgres;
using Logshark.Core.Exceptions;
using Logshark.PluginLib.Extensions;
using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Logshark.Core.Controller.Metadata
{
    /// <summary>
    /// Handles writing metadata about a Logshark run to Postgres.
    /// </summary>
    internal class LogsharkRunMetadataPostgresWriter : ILogsharkRunMetadataWriter
    {
        // The name of the Postgres database where any metadata should be stored.
        protected const string LogsharkMetadataDatabaseName = "logshark_metadata";

        protected readonly OrmLiteConnectionFactory connectionFactory;

        protected int? metadataRecordId;

        protected bool isDatabaseInitialized;
        protected bool isCustomMetadataWritten;
        protected bool isPluginExecutionMetadataWritten;
        protected bool isPublishedWorkbookMetadataWritten;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public LogsharkRunMetadataPostgresWriter(PostgresConnectionInfo postgresConnectionInfo)
        {
            connectionFactory = postgresConnectionInfo.GetConnectionFactory(LogsharkMetadataDatabaseName);
        }

        #region Public Methods

        public void WriteMetadata(LogsharkRunContext run)
        {
            try
            {
                var metadata = new LogsharkRunMetadata(run, metadataRecordId);

                using (IDbConnection db = connectionFactory.OpenDbConnection())
                {
                    // Create or migrate metadata db tables.
                    if (!isDatabaseInitialized)
                    {
                        isDatabaseInitialized = InitializeTables(db);
                    }

                    // Update the existing record, if we have one; otherwise, create a new record.
                    if (!metadataRecordId.HasValue)
                    {
                        Log.Debug("Creating metadata record for this Logshark run in database..");
                        db.Insert(metadata);
                        metadataRecordId = Convert.ToInt32(db.GetLastInsertId());
                        metadata.Id = metadataRecordId.Value;
                    }
                    else
                    {
                        Log.DebugFormat("Updating metadata about the {0} phase of this Logshark run in database..", metadata.CurrentProcessingPhase);
                        db.Update(metadata);
                    }

                    // Explicitly handle writing of data to foreign tables only once, due to limitations of the ORM.
                    if (!isCustomMetadataWritten)
                    {
                        isCustomMetadataWritten = WriteMetadata(metadata.CustomMetadataRecords, db);
                    }
                    if (!isPluginExecutionMetadataWritten)
                    {
                        isPluginExecutionMetadataWritten = WriteMetadata(metadata.PluginExecutionMetadataRecords, db);
                    }
                    if (!isPublishedWorkbookMetadataWritten)
                    {
                        isPublishedWorkbookMetadataWritten = WriteMetadata(metadata.PublishedWorkbookMetadataRecords, db);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new MetadataWriterException(String.Format("Failed to update Logshark metadata for run '{0}' in database: {1}", run.Id, ex.Message));
            }
        }

        #endregion Public Methods

        #region Protected Methods

        protected bool InitializeTables(IDbConnection db)
        {
            Log.Debug("Initializing Logshark run metadata tables..");

            try
            {
                db.CreateOrMigrateTable<LogsharkRunMetadata>();
                db.CreateOrMigrateTable<LogsharkCustomMetadata>();
                db.CreateOrMigrateTable<LogsharkPluginExecutionMetadata>();
                db.CreateOrMigrateTable<LogsharkPublishedWorkbookMetadata>();

                return true;
            }
            catch (Exception ex)
            {
                throw new MetadataWriterException(String.Format("Failed to initialize metadata tables in database: {0}", ex.Message), ex);
            }
        }

        protected bool WriteMetadata<T>(IEnumerable<T> metadataRecords, IDbConnection db) where T : new()
        {
            if (metadataRecords == null || !metadataRecords.Any())
            {
                return false;
            }

            try
            {
                db.InsertAll(metadataRecords);
                return true;
            }
            catch (Exception ex)
            {
                throw new MetadataWriterException(String.Format("Failed to write {0} records to database: {1}", typeof(T).Name, ex.Message), ex);
            }
        }

        #endregion Protected Methods
    }
}