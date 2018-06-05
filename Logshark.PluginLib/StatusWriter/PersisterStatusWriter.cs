using log4net;
using Logshark.PluginLib.Persistence;
using System;

namespace Logshark.PluginLib.StatusWriter
{
    /// <summary>
    /// Helper class for writing out a progress message the state of an IPersister.
    ///
    /// Available tokens:
    ///     {ItemsPersisted} - The count of items that have been persisted by the persister.
    ///     {ItemsPendingInsertion} - The number of items that are currently queued and awaiting insertion.
    ///     {ItemsRemaining} - The number of expected items that have not yet been persisted.
    ///     {PercentComplete} - The percentage of expected items that have been persisted.
    ///     {PersistedType} - The name of the type being persisted.
    /// </summary>
    public class PersisterStatusWriter<T> : BaseStatusWriter where T : new()
    {
        protected readonly IPersister<T> persister;
        protected readonly string persistedType = typeof(T).Name;
        protected long expectedTotalPersistedItems;

        /// <summary>
        /// Creates a new persister status heartbeat timer with the given parameters.
        /// </summary>
        /// <param name="persister">The persister to monitor the status of.</param>
        /// <param name="logger">The logger to append messages to.</param>
        /// <param name="progressFormatMessage">The progress message. Can contain tokens (see class summary).</param>
        /// <param name="pollIntervalSeconds">The number of seconds to wait between heartbeats.</param>
        /// <param name="expectedTotalPersistedItems">The number of iitems expected to be persisted.  Optional.</param>
        /// <param name="options">Options about when to write status.</param>
        public PersisterStatusWriter(IPersister<T> persister, ILog logger, 
                                     string progressFormatMessage = PluginLibConstants.DEFAULT_PERSISTER_STATUS_WRITER_PROGRESS_MESSAGE,
                                     int pollIntervalSeconds = PluginLibConstants.DEFAULT_PROGRESS_MONITOR_POLLING_INTERVAL_SECONDS,
                                     long? expectedTotalPersistedItems = 0,
                                     StatusWriterOptions options = StatusWriterOptions.WriteOnStop)
            : base(logger, progressFormatMessage, pollIntervalSeconds, options)
        {
            this.persister = persister;
            if (expectedTotalPersistedItems.HasValue)
            {
                this.expectedTotalPersistedItems = expectedTotalPersistedItems.Value;
            }

            Start();
        }

        protected override string GetStatusMessage()
        {
            string message = progressFormatMessage;
            if (message.Contains("{ItemsPersisted}"))
            {
                message = message.Replace("{ItemsPersisted}", persister.ItemsPersisted.ToString());
            }
            if (message.Contains("{ItemsPendingInsertion}"))
            {
                message = message.Replace("{ItemsPendingInsertion}", persister.ItemsPendingInsertion.ToString());
            }
            if (message.Contains("{PersistedType}"))
            {
                message = message.Replace("{PersistedType}", persistedType);
            }

            if (expectedTotalPersistedItems > 0)
            {
                long itemsRemaining = expectedTotalPersistedItems - persister.ItemsPersisted;
                message = message.Replace("{ItemsRemaining}", itemsRemaining.ToString());
                message = message.Replace("{ItemsExpected}", expectedTotalPersistedItems.ToString());
                if (message.Contains("{PercentComplete}"))
                {
                    if (persister.ItemsPersisted < 0)
                    {
                        message = message.Replace("{PercentComplete}", "N/A");
                    }
                    else
                    {
                        int percentComplete = (int)Math.Floor(persister.ItemsPersisted * 100.0 / expectedTotalPersistedItems);
                        message = message.Replace("{PercentComplete}", String.Format("{0}%", percentComplete));
                    }
                }
            }

            return message;
        }
    }
}