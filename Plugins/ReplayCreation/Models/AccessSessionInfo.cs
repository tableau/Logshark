using System;
using MongoDB.Bson;

namespace Logshark.Plugins.ReplayCreation.Models
{
    /// <summary>
    /// AccessSessionInfo contains info from access log
    /// </summary>
    internal class AccessSessionInfo
    {
        public string apache_request_id { get; set; }
        public string request_method { get; set; }
        public string request_ip { get; set; }
        public string resource { get; set; }
        public DateTime request_time { get; set; }
        public string load_time { get; set; }
        public string status_code { get; set; }

        /// <summary>
        /// Parse logLine and extract required data from it
        /// </summary>
        /// <param name="logLine"></param>
        public AccessSessionInfo(BsonDocument logLine)
        {
            apache_request_id = logLine.GetValue("request_id").AsString;
            request_method = logLine.GetValue("request_method").AsString;
            request_ip = logLine.GetValue("request_ip").AsString;
            resource = logLine.GetValue("resource").AsString;
            load_time = logLine.GetValue("request_time").AsString;
            var offset = logLine.GetElement("ts_offset").Value.ToString();
            var timeOffSet = int.Parse(offset);
            var ts = new System.TimeSpan(timeOffSet / 100, timeOffSet % 100, 0);

            //convert the current time to UTC
            var browserStartTime = (DateTime)logLine.GetElement("ts").Value - ts;
            request_time = browserStartTime.ToUniversalTime();

            status_code = logLine.GetValue("status_code").AsString;
        }
    }
}