using Logshark.PluginLib.Extensions;
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

        public VizqlDesktopSession() { }

        public VizqlDesktopSession(BsonDocument startupinfo)
        {
            BsonDocument values = BsonDocumentHelper.GetValuesStruct(startupinfo);
            TableauVersion = values.GetString("tableau-version");
            CurrentWorkingDirectory = values.GetString("cwd");
            ProcessId = startupinfo.GetInt("pid");
            Domain = values.GetString("domain");
            if (Domain.Equals("\'\'"))
            {
                Domain = null;
            }
            Hostname = values.GetString("hostname");
            Os = values.GetString("os");
            StartTime = startupinfo.GetDateTime("ts");

            VizqlSessionId = String.Format("{0}_{1}_{2:yyMMdd_HHmmssff}", Hostname, ProcessId, StartTime);

            CreateEventCollections();
        }
    }
}