using System;
using MongoDB.Bson;

namespace Logshark.Plugins.ReplayCreation.Models
{
    /// <summary>
    /// AccessSessionInfo contains info from access log
    /// </summary>
    internal class AccessSessionInfo
    {
        public string ApacheRequestId { get; set; }
        public string RequestMethod { get; set; }
        public string RequestId { get; set; }
        public string Resource { get; set; }
        public DateTime RequestTime { get; set; }
        public string LoadTime { get; set; }
        public string StatusCode { get; set; }

        /// <summary>
        /// Parse logLine and extract required data from it
        /// </summary>
        /// <param name="logLine"></param>
        public AccessSessionInfo(BsonDocument logLine)
        {
            ApacheRequestId = logLine.GetValue("request_id").AsString;
            RequestMethod = logLine.GetValue("request_method").AsString;
            RequestId = logLine.GetValue("request_ip").AsString;
            Resource = logLine.GetValue("resource").AsString;
            LoadTime = logLine.GetValue("request_time").AsString;
            var offset = logLine.GetElement("ts_offset").Value.ToString();
            var timeOffSet = int.Parse(offset);
            var ts = new TimeSpan(timeOffSet / 100, timeOffSet % 100, 0);

            //convert the current time to UTC
            var browserStartTime = (DateTime)logLine.GetElement("ts").Value - ts;
            RequestTime = browserStartTime.ToUniversalTime();

            StatusCode = logLine.GetValue("status_code").AsString;
        }
    }
}