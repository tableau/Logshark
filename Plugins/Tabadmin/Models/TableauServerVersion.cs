using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Helpers;
using Logshark.Plugins.Tabadmin.Helpers;
using MongoDB.Bson;
using System;
using System.Text.RegularExpressions;

namespace Logshark.Plugins.Tabadmin.Models
{
    /// <summary>
    /// Tableau Server version information.
    /// </summary>
    public sealed class TableauServerVersion : BaseTabadminModel
    {
        // Example String: "====>> <script> 9.2 (build: 9200.16.0204.1543): Starting at 2016-02-21 23:09:00.553 -0800 <<===="
        // Would return: shortVersion="9.2"; longVersion="9200.16.0204.1543";
        private static readonly Regex versionRegex = new Regex(@"^====>> <script> (?<shortVersion>.+?) \(build: (?<longVersion>.+?)\):.*<<====$",
                                                               RegexOptions.Compiled);

        public string Version { get; set; }
        public string VersionLong { get; set; }
        public string TimestampOffset { get; set; }  // Expected TimestampOffset format is "+1000" or "-200", for GMT+10 and GMT-2 respectively.
        public DateTime StartDate { get; set; }  // Date this version was first detected (not necessarily actual installation date of version.) TFS:488820
        public DateTime? EndDate { get; set; }  // Date next version was first detected (not necessarily actual end date of this version.)
        public DateTime StartDateGmt { get; set; }
        public DateTime? EndDateGmt { get; set; }

        public TableauServerVersion()
        {
        }

        public TableauServerVersion(BsonDocument document) : base(document)
        {
            Match match = versionRegex.Match(BsonDocumentHelper.GetString("message", document));
            Version = match.Groups["shortVersion"].Value;
            VersionLong = match.Groups["longVersion"].Value;

            StartDate = document.GetDateTime("ts");
            TimestampOffset = document.GetString("ts_offset");
            StartDateGmt = (DateTime) DateTimeConversionHelper.ConvertDateTime(StartDate, TimestampOffset);
            EndDateGmt = DateTimeConversionHelper.ConvertDateTime(EndDate, TimestampOffset);
        }
    }
}