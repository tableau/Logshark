using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Logshark.Plugins.Backgrounder.Model
{
    internal sealed class BackgrounderExtractJobDetail : BackgrounderJobDetail
    {
        // Regex that can be used to extract Tableau Extract details from a VqlSessionService log message
        private static readonly Regex vqlSessionExtractDetailsRegex = new Regex(@"
            ^Storing\sto\s(repository|SOS):\s
            (?<extract_url>.+?)/extract\s
            (repoExtractId|reducedDataId):(?<extract_id>.+?)\s
            size:(?<twb_size>\d+?)\s\(twb\)\s
            \+\s(?<extract_size>\d+?)\s
            \(guid={(?<extract_guid>[0-9A-F-]+?)}\)\s
            =\s(?<total_size>\d+?)$",
            RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        [AutoIncrement]
        [PrimaryKey]
        public int Id { get; set; }

        [Index]
        public long BackgrounderJobId { get; set; }

        [Index]
        public Guid BackgrounderJobEventHash { get; set; }

        public string ExtractUrl { get; set; }
        public string ExtractId { get; set; }
        public string ResourceName { get; set; }
        public long TwbSize { get; set; }
        public long ExtractSize { get; set; }
        public long TotalSize { get; set; }
        public string ExtractGuid { get; set; }

        [Index]
        public string VizqlSessionId { get; set; }

        public string ResourceType { get; set; }

        public BackgrounderExtractJobDetail()
        {
        }

        public BackgrounderExtractJobDetail(BackgrounderJob backgrounderJob, IEnumerable<BsonDocument> vqlSessionServiceEvents)
        {
            BackgrounderJobId = backgrounderJob.JobId;
            BackgrounderJobEventHash = backgrounderJob.EventHash;
            ParseBackgroundJobArgs(backgrounderJob.Args);
            foreach (var vqlSessionServiceEvent in vqlSessionServiceEvents)
            {
                string message = BsonDocumentHelper.GetString("message", vqlSessionServiceEvent);

                if (message.StartsWith("Created session id:"))
                {
                    string sessionPrefix = message.Split(':')[1];
                    string sessionSuffix = GetSessionSuffix(vqlSessionServiceEvent);

                    VizqlSessionId = sessionPrefix + "-" + sessionSuffix;
                }
                else if (message.StartsWith("Storing"))
                {
                    IDictionary<string, string> extractDetails = vqlSessionExtractDetailsRegex.MatchNamedCaptures(message);

                    if (extractDetails.ContainsKey("extract_url"))
                    {
                        ExtractUrl = extractDetails["extract_url"];
                    }
                    if (extractDetails.ContainsKey("extract_id"))
                    {
                        ExtractId = extractDetails["extract_id"];
                    }
                    if (extractDetails.ContainsKey("extract_guid"))
                    {
                        ExtractGuid = extractDetails["extract_guid"];
                    }
                    if (extractDetails.ContainsKey("twb_size"))
                    {
                        TwbSize = Int64.Parse(extractDetails["twb_size"]);
                    }
                    if (extractDetails.ContainsKey("extract_size"))
                    {
                        ExtractSize = Int64.Parse(extractDetails["extract_size"]);
                    }
                    if (extractDetails.ContainsKey("total_size"))
                    {
                        TotalSize = Int64.Parse(extractDetails["total_size"]);
                    }
                }
            }
        }

        private string GetSessionSuffix(BsonDocument vqlSessionServiceEvent)
        {
            int worker = BsonDocumentHelper.GetInt("worker", vqlSessionServiceEvent);
            string fileName = BsonDocumentHelper.GetString("file", vqlSessionServiceEvent);
            string process = fileName.Split('-')[1].Split('.')[0];

            return worker + ":" + process;
        }

        private void ParseBackgroundJobArgs(string jobArgs)
        {
            var argChunks = jobArgs.Replace("[", "").Replace("]", "").Split(',').ToArray();
            ResourceType = argChunks[0].Trim();
            ResourceName = argChunks[2].Trim();
        }
    }
}