using log4net;
using Logshark.PluginLib.Logging;
using Logshark.PluginModel.Model;
using Logshark.Plugins.Vizql.Models;
using Logshark.Plugins.Vizql.Models.Events.Error;
using Logshark.Plugins.Vizql.Models.Events.Performance;
using Logshark.Plugins.Vizql.Models.Events.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Logshark.Plugins.Vizql.Helpers
{
    public sealed class DesktopSessionPersister : IPersister<VizqlDesktopSession>
    {
        private const string MaxQueryLengthArgumentKey = "VizqlDesktop.MaxQueryLength";
        private const int MaxQueryLengthDefault = 10000;

        private readonly int maxQueryLength;

        private readonly IPersister<VizqlDesktopSession> sessionPersister;
        private readonly IPersister<VizqlErrorEvent> errorPersister;
        private readonly IPersister<VizqlPerformanceEvent> performanceEventPersister;
        private readonly IPersister<VizqlEndQuery> endQueryPersister;

        private static readonly ILog Log = PluginLogFactory.GetLogger(Assembly.GetExecutingAssembly(), MethodBase.GetCurrentMethod());

        public long ItemsPersisted { get; private set; }

        public DesktopSessionPersister(IPluginRequest pluginRequest, IExtractPersisterFactory extractFactory)
        {
            maxQueryLength = VizqlPluginArgumentHelper.GetMaxQueryLength(pluginRequest, MaxQueryLengthArgumentKey, MaxQueryLengthDefault);

            sessionPersister = extractFactory.CreateExtract<VizqlDesktopSession>("VizqlDesktopSessions.hyper");
            errorPersister = extractFactory.CreateExtract<VizqlErrorEvent>("VizqlDesktopErrorEvents.hyper");
            performanceEventPersister = extractFactory.CreateExtract<VizqlPerformanceEvent>("VizqlDesktopPerformanceEvents.hyper");
            endQueryPersister = extractFactory.CreateExtract<VizqlEndQuery>("VizqlDesktopEndQueryEvents.hyper");
        }

        public void Enqueue(IEnumerable<VizqlDesktopSession> sessions)
        {
            foreach (var session in sessions)
            {
                Enqueue(session);
            }
        }

        public void Enqueue(VizqlDesktopSession session)
        {
            try
            {
                sessionPersister.Enqueue(session);

                // Error
                errorPersister.Enqueue(session.ErrorEvents);

                // Performance
                performanceEventPersister.Enqueue(session.PerformanceEvents);

                // Query
                endQueryPersister.Enqueue(session.EndQueryEvents.Select(query => query.WithTruncatedQueryText(maxQueryLength)));

                ItemsPersisted++;
                Log.DebugFormat("Persisted session {0}", session.VizqlSessionId);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Failed to persist session '{0}': {1}", session.VizqlSessionId, ex.Message);
            }
        }

        public void Dispose()
        {
            if (sessionPersister != null) sessionPersister.Dispose();
            if (errorPersister != null) errorPersister.Dispose();
            if (performanceEventPersister != null) performanceEventPersister.Dispose();
            if (endQueryPersister != null) endQueryPersister.Dispose();
        }
    }
}