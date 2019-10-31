using log4net;
using Logshark.PluginModel.Model;
using System;

namespace Logshark.PluginLib.StatusWriter
{
    /// <summary>
    /// Helper class for writing out a progress message conveying the state of an IPersister.
    ///
    /// Available tokens:
    ///     {ItemsPersisted} - The count of items that have been persisted by the persister.
    ///     {ItemsRemaining} - The number of expected items that have not yet been persisted.
    ///     {PercentComplete} - The percentage of expected items that have been persisted.
    ///     {PersistedType} - The name of the type being persisted.
    /// </summary>
    public sealed class PersisterStatusWriter<T> : BaseStatusWriter where T : new()
    {
        private readonly IPersister<T> persister;
        private readonly string persistedType = typeof(T).Name;
        private readonly long? expectedTotalPersistedItems;

        private bool disposed;

        /// <summary>
        /// Creates a new persister status heartbeat timer with the given parameters.
        /// </summary>
        /// <param name="persister">The persister to monitor the status of.</param>
        /// <param name="logger">The logger to append messages to.</param>
        /// <param name="progressFormatMessage">The progress message. Can contain tokens (see class summary).</param>
        /// <param name="pollIntervalSeconds">The number of seconds to wait between heartbeats.</param>
        /// <param name="expectedTotalPersistedItems">The number of iitems expected to be persisted.  Optional.</param>
        public PersisterStatusWriter(IPersister<T> persister, ILog logger, 
                                     string progressFormatMessage = PluginLibConstants.DEFAULT_PERSISTER_STATUS_WRITER_PROGRESS_MESSAGE,
                                     int pollIntervalSeconds = PluginLibConstants.DEFAULT_PROGRESS_MONITOR_POLLING_INTERVAL_SECONDS,
                                     long? expectedTotalPersistedItems = 0)
            : base(logger, progressFormatMessage, pollIntervalSeconds)
        {
            this.persister = persister;
            this.expectedTotalPersistedItems = expectedTotalPersistedItems;

            progressHeartbeatTimer.Start();
        }

        protected override string GetStatusMessage()
        {
            string message = progressFormatMessage;
            if (message.Contains("{ItemsPersisted}"))
            {
                message = message.Replace("{ItemsPersisted}", persister.ItemsPersisted.ToString());
            }
            if (message.Contains("{PersistedType}"))
            {
                message = message.Replace("{PersistedType}", persistedType);
            }

            if (expectedTotalPersistedItems.HasValue)
            {
                long itemsRemaining = expectedTotalPersistedItems.Value - persister.ItemsPersisted;
                message = message.Replace("{ItemsRemaining}", itemsRemaining.ToString());
                message = message.Replace("{ItemsExpected}", expectedTotalPersistedItems.ToString());
                if (message.Contains("{PercentComplete}"))
                {
                    if (persister.ItemsPersisted < 0 || expectedTotalPersistedItems.Value <= 0)
                    {
                        message = message.Replace("{PercentComplete}", "N/A");
                    }
                    else
                    {
                        int percentComplete = (int)Math.Floor(persister.ItemsPersisted * 100.0 / expectedTotalPersistedItems.Value);
                        message = message.Replace("{PercentComplete}", String.Format("{0}%", percentComplete));
                    }
                }
            }

            return message;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    persister.Dispose();
                }

                disposed = true;
                base.Dispose(disposing);
            }
        }
    }
}