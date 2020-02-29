using LogShark.Containers;
using LogShark.Extensions;
using LogShark.Plugins.Shared;

namespace LogShark.Plugins.VizqlDesktop.Model
{
    public class VizqlEndQueryEvent : VizqlDesktopBaseEvent
    {
        public int? Cols { get; }
        public double? Elapsed { get; }
        public long? ProtocolId { get; }
        public string Query { get; }
        public long? QueryHash { get; }
        public int? Rows { get; }

        public VizqlEndQueryEvent(
            NativeJsonLogsBaseEvent baseEvent,
            LogLine logLine,
            string sessionId,
            int maxQueryLength) 
            : base(logLine, baseEvent, sessionId)
        {
            var payload = baseEvent.EventPayload;
            
            var query = payload.GetStringFromPath("query-trunc") ?? payload.GetStringFromPath("query");
            if (query?.Length > maxQueryLength)
            {
                query = query.Substring(0, maxQueryLength);
            }
            
            Query = query;
            ProtocolId = payload.GetLongFromPath("protocol-id");
            Cols = payload.GetIntFromPath("cols");
            Rows = payload.GetIntFromPath("rows");
            QueryHash = payload.GetLongFromPath("query-hash");
            Elapsed = payload.GetDoubleFromPath("elapsed");
        }
    }
}