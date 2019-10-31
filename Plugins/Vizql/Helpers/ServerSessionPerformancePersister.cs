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
    public sealed class ServerSessionPerformancePersister : IPersister<VizqlServerSession>
    {
        private const string MaxQueryLengthArgumentKey = "VizqlServerPerformance.MaxQueryLength";
        private const int MaxQueryLengthDefault = 10000;

        private readonly int maxQueryLength;

        private readonly IPersister<VizqlServerSession> sessionPersister;
        private readonly IPersister<VizqlErrorEvent> errorPersister;
        private readonly IPersister<VizqlPerformanceEvent> performanceEventPersister;
        private readonly IPersister<VizqlEndQuery> endQueryPersister;
        private readonly IPersister<VizqlQpQueryEnd> qpQueryEndPersister;

        private static readonly ILog Log = PluginLogFactory.GetLogger(Assembly.GetExecutingAssembly(), MethodBase.GetCurrentMethod());

        public long ItemsPersisted { get; private set; }

        public ServerSessionPerformancePersister(IPluginRequest pluginRequest, IExtractPersisterFactory extractFactory)
        {
            maxQueryLength = VizqlPluginArgumentHelper.GetMaxQueryLength(pluginRequest, MaxQueryLengthArgumentKey, MaxQueryLengthDefault);

            sessionPersister = extractFactory.CreateExtract<VizqlServerSession>("VizqlPerformanceSessions.hyper");
            errorPersister = extractFactory.CreateExtract<VizqlErrorEvent>("VizqlPerformanceErrorEvents.hyper");
            performanceEventPersister = extractFactory.CreateExtract<VizqlPerformanceEvent>("VizqlPerformanceEvents.hyper");
            endQueryPersister = extractFactory.CreateExtract<VizqlEndQuery>("VizqlEndQueryEvents.hyper");
            qpQueryEndPersister = extractFactory.CreateExtract<VizqlQpQueryEnd>("VizqlQpQueryEndEvents.hyper");
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
                sessionPersister.Enqueue(session);
                errorPersister.Enqueue(session.ErrorEvents);
                performanceEventPersister.Enqueue(session.PerformanceEvents);
                endQueryPersister.Enqueue(session.EndQueryEvents.Select(query => query.WithTruncatedQueryText(maxQueryLength)));
                qpQueryEndPersister.Enqueue(session.QpQueryEndEvents);

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
            if (qpQueryEndPersister != null) qpQueryEndPersister.Dispose();
        }
    }
}