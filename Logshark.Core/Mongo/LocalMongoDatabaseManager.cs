using log4net;
using Logshark.RequestModel;
using System;
using System.Reflection;

namespace Logshark.Core.Mongo
{
    internal sealed class LocalMongoDatabaseManager : IDisposable
    {
        private readonly LocalMongoProcessManager localMongoProcessManager;
        private bool disposed;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public LocalMongoDatabaseManager(LogsharkRequest request)
        {
            localMongoProcessManager = StartLocalMongoIfRequested(request);
        }

        public bool IsRunning()
        {
            return localMongoProcessManager != null && localMongoProcessManager.IsMongoRunning();
        }

        /// <summary>
        /// Spin up local MongoDB instance if the user requested it.
        /// </summary>
        private LocalMongoProcessManager StartLocalMongoIfRequested(LogsharkRequest request)
        {
            if (!request.StartLocalMongo)
            {
                return null;
            }

            // Start local Mongo instance.
            var processManager = new LocalMongoProcessManager(request.LocalMongoPort);
            processManager.StartMongoProcess(request.Configuration.LocalMongoOptions.PurgeLocalMongoOnStartup);

            // Update MongoConnectionInfo on the request to point to the local instance.
            request.Configuration.MongoConnectionInfo = processManager.GetConnectionInfo();

            return processManager;
        }

        /// <summary>
        /// Stop local MongoDB instance if the user requested it.
        /// </summary>
        private bool StopLocalMongoIfRequested()
        {
            if (localMongoProcessManager == null || !localMongoProcessManager.IsMongoRunning())
            {
                // Nothing to do.
                return true;
            }

            Log.Debug("Shutting down local MongoDB process..");
            try
            {
                localMongoProcessManager.KillAllMongoProcesses();
                Log.Debug("Successfully shut down local MongoDB process.");
                return true;
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Failed to gracefully shut down local MongoDB process: {0}", ex.Message);
                return false;
            }
        }

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    StopLocalMongoIfRequested();
                }

                disposed = true;
            }
        }

        #endregion IDisposable Implementation
    }
}