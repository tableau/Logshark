using Logshark.PluginLib.Helpers;
using Logshark.Plugins.Tabadmin.Helpers;
using MongoDB.Bson;
using ServiceStack.DataAnnotations;
using System;
using System.Text.RegularExpressions;

namespace Logshark.Plugins.Tabadmin.Models
{
    /// <summary>
    /// Tableau Server version information.
    /// </summary>
    [Alias("tabadmin_ts_version")]
    public class TSVersion : TabadminModelBase, IComparable<TSVersion>
    {
        // Example String: "====>> <script> 9.2 (build: 9200.16.0204.1543): Starting at 2016-02-21 23:09:00.553 -0800 <<===="
        // Would return: shortVersion="9.2"; longVersion="9200.16.0204.1543";
        private static readonly Regex versionRegex = new Regex(@"^====>> <script> (?<shortVersion>.+?) \(build: (?<longVersion>.+?)\):.*<<====$",
                                                               RegexOptions.Compiled);

        public string VersionStringShort { get; set; }
        public string VersionStringLong { get; set; }
        public string TimestampOffset { get; set; }  // Expected TimestampOffset format is "+1000" or "-200", for GMT+10 and GMT-2 respectively.
        public DateTime StartDate { get; set; }  // Date this version was first detected (not necessarily actual installation date of version.) TFS:488820
        public DateTime? EndDate { get; set; }  // Date next version was first detected (not necessarily actual end date of this version.)
        public DateTime StartDateGmt { get; set; }
        public DateTime? EndDateGmt { get; set; }
        public int? Worker { get; set; }
        public string File { get; set; }
        public string FilePath { get; set; }
        public int Line { get; set; }

        public TSVersion()
        {
        }

        public TSVersion(BsonDocument logLine, Guid logsetHash)
        {
            LogsetHash = logsetHash;
            Match match = versionRegex.Match(BsonDocumentHelper.GetString("message", logLine));
            VersionStringShort = match.Groups["shortVersion"].Value;
            VersionStringLong = match.Groups["longVersion"].Value;

            StartDate = BsonDocumentHelper.GetDateTime("ts", logLine);
            TimestampOffset = BsonDocumentHelper.GetString("ts_offset", logLine);
            StartDateGmt = (DateTime)DateTimeConversionHelper.ConvertDateTime(StartDate, TimestampOffset);
            EndDateGmt = DateTimeConversionHelper.ConvertDateTime(EndDate, TimestampOffset);
            Worker = BsonDocumentHelper.GetNullableInt("worker", logLine);
            File = BsonDocumentHelper.GetString("file", logLine);
            FilePath = BsonDocumentHelper.GetString("file_path", logLine);
            Line = BsonDocumentHelper.GetInt("line", logLine);
            EventHash = HashHelper.GenerateHashGuid(StartDate, TimestampOffset, VersionStringLong, Worker, File, FilePath, Line);
        }

        /// <summary>
        /// Compare two TSVersion objects by their StartDateTimeGMT field.
        /// </summary>
        /// <param name="other">Another TSVersion to compare against this one.</param>
        /// <returns>An int less than zero if this is older than other. An int greater than zero if this is newer than other. Zero if the two objects have equal timestamps.</returns>
        public int CompareTo(TSVersion other)
        {
            return StartDateGmt.CompareTo(other.StartDateGmt);
        }
    }
}