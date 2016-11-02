using log4net;
using System;
using System.Timers;

namespace Logshark.PluginLib.StatusWriter
{
    public enum StatusWriterOptions { WriteOnlyOnTimer, WriteOnStart, WriteOnStop, WriteOnStartAndStop }

    public abstract class BaseStatusWriter : IDisposable, IStatusWriter
    {
        protected readonly ILog logger;
        protected readonly string progressFormatMessage;
        protected readonly StatusWriterOptions options;
        protected Timer progressHeartbeatTimer;
        private bool disposed;

        /// <summary>
        /// Creates a new task progress heartbeat timer with the given parameters.
        /// </summary>
        /// <param name="logger">The logger to append messages to.</param>
        /// <param name="progressFormatMessage">The progress message. Can contain tokens (see class summary).</param>
        /// <param name="pollIntervalSeconds">The number of seconds to wait between heartbeats.</param>
        /// <param name="options">Options as to whether to write status when starting/stopping (in addition to writing on timer tick).</param>
        protected BaseStatusWriter(ILog logger, string progressFormatMessage, int pollIntervalSeconds, StatusWriterOptions options = StatusWriterOptions.WriteOnStop)
        {
            this.logger = logger;
            this.progressFormatMessage = progressFormatMessage;
            this.options = options;

            progressHeartbeatTimer = new Timer(pollIntervalSeconds * 1000);
            progressHeartbeatTimer.Elapsed += OnHeartbeat;
            progressHeartbeatTimer.AutoReset = true;
        }

        protected BaseStatusWriter() { }

        public void Start()
        {
            if (options == StatusWriterOptions.WriteOnStart || options == StatusWriterOptions.WriteOnStartAndStop)
            {
                WriteStatus();
            }
            progressHeartbeatTimer.Start();
        }

        public void Stop()
        {
            progressHeartbeatTimer.Stop();
            if (options == StatusWriterOptions.WriteOnStop || options == StatusWriterOptions.WriteOnStartAndStop)
            {
                WriteStatus();
            }
        }

        public virtual void WriteStatus()
        {
            string statusMessage = GetStatusMessage();

            // Check to make sure the substitution was successful so we dont accidentally print out the template.
            if (!statusMessage.Equals(progressFormatMessage))
            {
                logger.Info(statusMessage);
            }
        }

        protected virtual void OnHeartbeat(object source, ElapsedEventArgs e)
        {
            WriteStatus();
        }

        protected abstract string GetStatusMessage();

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
                Stop();
                progressHeartbeatTimer.Dispose();
            }

            disposed = true;
        }

        #endregion IDisposable Implementation
    }
}