using log4net;
using Logshark.Core.Controller.Metadata.Logset.Mongo;
using Logshark.RequestModel;
using System;
using System.Reflection;
using System.Threading;

namespace Logshark.Core.Controller.Parsing
{
    internal class MongoProcessingHeartbeatTimer : IDisposable
    {
        private readonly LogsetMetadataWriter metadataWriter;
        private readonly Timer timer;
        private bool disposed;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public MongoProcessingHeartbeatTimer(LogsharkRequest logsharkRequest)
        {
            metadataWriter = new LogsetMetadataWriter(logsharkRequest);
            long heartbeatDelayMs = 1000 * CoreConstants.MONGO_PROCESSING_HEARTBEAT_INTERVAL;
            timer = new Timer(WriteHeartbeat, null, 0, heartbeatDelayMs);
        }

        private void WriteHeartbeat(Object state)
        {
            try
            {
                metadataWriter.WriteProperty("processing_heartbeat", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Failed to write processing heartbeat to MongoDB: {0}", ex.Message);
            }
        }

        private void RemoveHeartbeat()
        {
            try
            {
                metadataWriter.RemoveProperty("processing_heartbeat");
            }
            catch (Exception ex)
            {
                // Failure to remove the heartbeat doesn't actually cause any problems, but we take note of it since it is potentially interesting.
                Log.DebugFormat("Failed to remove heartbeat property from MongoDB: {0}", ex.Message);
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
                return;

            if (disposing)
            {
                timer.Dispose();
                RemoveHeartbeat();
            }

            disposed = true;
        }

        #endregion IDisposable Implementation
    }
}