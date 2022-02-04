using System;
using LogShark.Shared.LogReading.Containers;

namespace LogShark.Plugins.Apache
{
    public class ApacheEvent : BaseEvent
    {
        public long? ContentLength { get; }
        public int? Port { get; }
        public string RequestBody { get; }
        public string Requester { get; }
        public string RequestId { get; }
        public string RequestIp { get; }
        public string RequestMethod { get; }
        public long? RequestTimeMS { get; }
        public int? StatusCode { get; }
        public string TimestampOffset { get; }
        public string XForwardedFor { get; }
        public string TableauErrorSource { get; }
        public int? TableauStatusCode { get; }
        public string TableauErrorCode { get; }
        public string TableauServiceName { get; }
        public string TableauTrace { get; set; }
        public string RefererStub { get; set; }
        public string LocalIp { get; set; }
        public string RemoteLogName { get; set; }
        public int? OriginalRequestStatus { get; set; }
        public ApacheEvent(
            LogLine logLine,
            DateTime timestamp,
            long? contentLength,
            int? port,
            string requestBody,
            string requester,
            string requestId,
            string requestIp,
            string requestMethod,
            long? requestTimeMs,
            int? statusCode,
            string timestampOffset,
            string xForwardedFor,
            string tableauErrorSource,
            int? tableauStatusCode,
            string tableauErrorCode,
            string tableauServiceName,
            string tableauTrace,
            string refererStub,
            string localIp,
            string remoteLogName,
            int? originalRequestStatus
            ) : base(logLine, timestamp)
        {
            ContentLength = contentLength;
            Port = port;
            RequestBody = requestBody;
            Requester = requester;
            RequestId = requestId;
            RequestIp = requestIp;
            RequestMethod = requestMethod;
            RequestTimeMS = requestTimeMs;
            StatusCode = statusCode;
            TimestampOffset = timestampOffset;
            XForwardedFor = xForwardedFor;
            TableauErrorSource = tableauErrorSource;
            TableauErrorCode = tableauErrorCode;
            TableauServiceName = tableauServiceName;
            TableauStatusCode = tableauStatusCode;
            TableauTrace = tableauTrace;
            RefererStub = refererStub;
            LocalIp = localIp;
            RemoteLogName = remoteLogName;
            OriginalRequestStatus = originalRequestStatus;
        }
    }
}