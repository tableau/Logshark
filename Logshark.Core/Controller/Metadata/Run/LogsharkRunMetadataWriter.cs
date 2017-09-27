using log4net;
using Logshark.ConnectionModel.Postgres;
using Logshark.Core.Exceptions;
using Logshark.PluginLib.Extensions;
using Logshark.RequestModel;
using ServiceStack.OrmLite;
using System;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Logshark.Core.Controller.Metadata.Run
{
    /// <summary>
    /// Handles writing metadata about a Logshark run to Postgres.
    /// </summary>
    public class LogsharkRunMetadataWriter
    {
        protected readonly OrmLiteConnectionFactory connectionFactory;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public LogsharkRunMetadataWriter(PostgresConnectionInfo postgresConnectionInfo)
        {
            connectionFactory = postgresConnectionInfo.GetConnectionFactory(CoreConstants.LOGSHARK_METADATA_DATABASE_NAME);
            InitializeTables();
        }

        #region Public Methods

        public void UpdateMetadata(LogsharkRequest request)
        {
            try
            {
                if (request.RunContext.MetadataRecordId == null)
                {
                    request.RunContext.MetadataRecordId = CreateInitialMetadataRecord(request);
                }
                else
                {
                    UpdateExistingMetadataRecord(request);
                }
            }
            catch (Exception ex)
            {
                throw new MetadataWriterException(String.Format("Failed to update Logshark metadata for run '{0}' in database: {1}", request.RunId, ex.Message));
            }
        }

        public void WriteCustomMetadata(LogsharkRequest request)
        {
            try
            {
                var metadata = new LogsharkRunMetadata(request);
                InsertCustomMetadata(metadata);
            }
            catch (Exception ex)
            {
                throw new MetadataWriterException(String.Format("Failed to insert Logshark custom metadata records for run '{0}' in database: {1}", request.RunId, ex.Message));
            }
        }

        public void WritePluginExecutionMetadata(LogsharkRequest request)
        {
            try
            {
                var metadata = new LogsharkRunMetadata(request);
                InsertPluginExecutionMetadata(metadata);
                InsertPublishedWorkbookMetadata(metadata);
            }
            catch (Exception ex)
            {
                throw new MetadataWriterException(String.Format("Failed to insert Logshark metadata records for run '{0}' in database: {1}", request.RunId, ex.Message));
            }
        }

        #endregion Public Methods

        #region Protected Methods

        protected void InitializeTables()
        {
            Log.Debug("Initializing Logshark run metadata tables..");

            try
            {
                using (IDbConnection db = connectionFactory.OpenDbConnection())
                {
                    db.CreateOrMigrateTable<LogsharkRunMetadata>();
                    db.CreateOrMigrateTable<LogsharkCustomMetadata>();
                    db.CreateOrMigrateTable<LogsharkPluginExecutionMetadata>();
                    db.CreateOrMigrateTable<LogsharkPublishedWorkbookMetadata>();
                }
            }
            catch (Exception ex)
            {
                throw new MetadataWriterException(String.Format("Failed to initialize metadata tables in database: {0}", ex.Message), ex);
            }
        }

        /// <summary>
        /// Writes metadata about this Logshark run to the backing database.
        /// </summary>
        /// <returns>Id of the inserted metadata record.</returns>
        protected int CreateInitialMetadataRecord(LogsharkRequest request)
        {
            try
            {
                var metadata = new LogsharkRunMetadata(request);

                Log.Debug("Creating metadata record for this Logshark run in database..");
                using (IDbConnection db = connectionFactory.OpenDbConnection())
                {
                    db.Insert(metadata);
                    int insertedRecordId = Convert.ToInt32(db.GetLastInsertId());
                    return insertedRecordId;
                }
            }
            catch (Exception ex)
            {
                throw new MetadataWriterException(String.Format("Failed to create Logshark metadata record for run '{0}' in database: {1}", request.RunId, ex.Message), ex);
            }
        }

        protected void UpdateExistingMetadataRecord(LogsharkRequest request)
        {
            try
            {
                var metadata = new LogsharkRunMetadata(request);

                Log.DebugFormat("Updating metadata about the {0} phase of this Logshark run in database..", metadata.CurrentProcessingPhase);
                using (IDbConnection db = connectionFactory.OpenDbConnection())
                {
                    db.Update(metadata);
                }
            }
            catch (Exception ex)
            {
                throw new MetadataWriterException(String.Format("Failed to update Logshark metadata for run '{0}' to database: {1}", request.RunId, ex.Message));
            }
        }

        protected void InsertCustomMetadata(LogsharkRunMetadata metadata)
        {
            if (metadata.CustomMetadataRecords.Any())
            {
                using (IDbConnection db = connectionFactory.OpenDbConnection())
                {
                    db.InsertAll(metadata.CustomMetadataRecords);
                }
            }
        }

        protected void InsertPluginExecutionMetadata(LogsharkRunMetadata metadata)
        {
            if (metadata.PluginExecutionMetadataRecords.Any())
            {
                using (IDbConnection db = connectionFactory.OpenDbConnection())
                {
                    db.InsertAll(metadata.PluginExecutionMetadataRecords);
                }
            }
        }

        protected void InsertPublishedWorkbookMetadata(LogsharkRunMetadata metadata)
        {
            if (metadata.PublishedWorkbookMetadataRecords.Any())
            {
                using (IDbConnection db = connectionFactory.OpenDbConnection())
                {
                    db.InsertAll(metadata.PublishedWorkbookMetadataRecords);
                }
            }
        }

        #endregion Protected Methods
    }
}