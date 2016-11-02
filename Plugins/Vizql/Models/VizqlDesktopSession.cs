using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
using System;

namespace Logshark.Plugins.Vizql.Models
{
    public class VizqlDesktopSession : VizqlSession
    {
        public string TableauVersion { get; set; }
        public string CurrentWorkingDirectory { get; set; }
        public int ProcessId { get; set; }
        public string Domain { get; set; }
        public string Hostname { get; set; }
        public string Os { get; set; }
        public DateTime StartTime { get; set; }

        public VizqlDesktopSession()
        {
        }

        public VizqlDesktopSession(BsonDocument startupinfo, Guid logsetHash)
        {
            BsonDocument values = BsonDocumentHelper.GetValuesStruct(startupinfo);
            TableauVersion = BsonDocumentHelper.GetString("tableau-version", values);
            CurrentWorkingDirectory = BsonDocumentHelper.GetString("cwd", values);
            ProcessId = BsonDocumentHelper.GetInt("pid", startupinfo);
            Domain = BsonDocumentHelper.GetString("domain", values);
            if (Domain.Equals("\'\'"))
            {
                Domain = null;
            }
            Hostname = BsonDocumentHelper.GetString("hostname", values);
            Os = BsonDocumentHelper.GetString("os", values);
            StartTime = BsonDocumentHelper.GetDateTime("ts", startupinfo);

            VizqlSessionId = Hostname + "_" + ProcessId + "_" + StartTime.ToString("yyMMdd_HHmmssff");
            LogsetHash = logsetHash;

            CreateEventCollections();
        }
    }
}