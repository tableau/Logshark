using Logshark.PluginLib.Extensions;
using Logshark.Plugins.Tabadmin.Helpers;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using Tableau.ExtractApi.DataAttributes;

namespace Logshark.Plugins.Tabadmin.Models
{
    /// <summary>
    /// A generalized object representing a log event. Will usually need to be inherited from to be useful.
    /// </summary>
    internal class TabadminLogEvent : BaseTabadminModel
    {

        public DateTime Timestamp { get; set; }
        public string TimestampOffset { get; set; }
        public DateTime TimestampGmt { get; set; }

        public string Hostname { get; set; }

        [ExtractIgnore]
        public TableauServerVersion ServerVersion { get; private set; }

        public string VersionId { get { return ServerVersion == null ? null : ServerVersion.Id; } }
        public string Version { get { return ServerVersion == null ? null : ServerVersion.Version; } }
        public string VersionLong { get { return ServerVersion == null ? null : ServerVersion.VersionLong; } }

        public TabadminLogEvent()
        {
        }

        public TabadminLogEvent(BsonDocument document, IEnumerable<TableauServerVersion> versionTimeline) : base(document)
        {
            Timestamp = document.GetDateTime("ts");
            TimestampOffset = document.GetString("ts_offset");
            TimestampGmt = (DateTime)DateTimeConversionHelper.ConvertDateTime(Timestamp, TimestampOffset);

            Hostname = document.GetString("hostname");

            ServerVersion = GetVersionByDate(versionTimeline);
        }

        private TableauServerVersion GetVersionByDate(IEnumerable<TableauServerVersion> versionTimeline)
        {
            foreach (var version in versionTimeline)
            {
                // The most recent version in the versionTimeline should have a null EndDate.
                if (version.StartDateGmt <= TimestampGmt &&
                    (version.EndDateGmt > TimestampGmt || version.EndDateGmt == null) &&
                    version.Worker == Worker)
                {
                    return version;
                }
            }

            return null;
        }
    }
}