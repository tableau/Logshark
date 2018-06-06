using log4net;
using Logshark.Core.Controller.Parsing.Mongo.Metadata;
using System;
using System.Reflection;
using System.Threading;

namespace Logshark.Core.Controller.Parsing.Mongo
{
    internal class MongoProcessingHeartbeatTimer : IDisposable
    {
        // The delay between writing out a heartbeat to Mongo, in seconds.
        protected const int MongoProcessingHeartbeatInterval = 15;

        protected readonly MongoLogProcessingMetadataWriter metadataWriter;
        protected readonly Timer timer;

        private bool disposed;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public MongoProcessingHeartbeatTimer(MongoLogProcessingMetadataWriter metadataWriter, string databaseName)
        {
            this.metadataWriter = metadataWriter;

            long heartbeatDelayMs = 1000 * MongoProcessingHeartbeatInterval;
            timer = new Timer(WriteHeartbeat, databaseName, 0, heartbeatDelayMs);
        }

        protected void WriteHeartbeat(object databaseName)
        {
            try
            {
                metadataWriter.WriteField("processing_heartbeat", DateTime.UtcNow, databaseName.ToString());
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Failed to write processing heartbeat to MongoDB: {0}", ex.Message);
            }
        }

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                timer.Dispose();
            }

            disposed = true;
        }

        #endregion IDisposable Implementation
    }
}