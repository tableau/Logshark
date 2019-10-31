using log4net;
using Logshark.PluginLib.Logging;
using Logshark.PluginModel.Model;
using Logshark.Plugins.Vizql.Models;
using Logshark.Plugins.Vizql.Models.Events.Error;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Logshark.Plugins.Vizql.Helpers
{
    public sealed class ServerSessionPersister : IPersister<VizqlServerSession>
    {
        private readonly IPersister<VizqlServerSession> _sessionPersister;
        private readonly IPersister<VizqlErrorEvent> _errorPersister;

        private static readonly ILog Log = PluginLogFactory.GetLogger(Assembly.GetExecutingAssembly(), MethodBase.GetCurrentMethod());

        public long ItemsPersisted { get; private set; }

        public ServerSessionPersister(IExtractPersisterFactory extractFactory)
        {
            _sessionPersister = extractFactory.CreateExtract<VizqlServerSession>("VizqlSessions.hyper");
            _errorPersister = extractFactory.CreateExtract<VizqlErrorEvent>("VizqlErrorEvents.hyper");
        }

        public void Enqueue(IEnumerable<VizqlServerSession> sessions)
        {
            foreach (var session in sessions)
            {
                Enqueue(session);
            }
        }

        public void Enqueue(VizqlServerSession session)
        {
            try
            {
                _sessionPersister.Enqueue(session);
                _errorPersister.Enqueue(session.ErrorEvents);

                ItemsPersisted++;
                Log.DebugFormat($"Persisted session {session.VizqlSessionId}");
            }
            catch (Exception ex)
            {
                Log.ErrorFormat($"Failed to persist session '{session.VizqlSessionId}': {ex.Message}");
            }
        }

        public void Dispose()
        {
            _sessionPersister?.Dispose();
            _errorPersister?.Dispose();
        }
    }
}