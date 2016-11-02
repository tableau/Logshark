using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Logshark.Plugins.Backgrounder.Model
{
    internal class BackgrounderExtractJobDetail : BackgrounderJobDetail
    {
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

                if (message.StartsWith("Storing to repository:"))
                {
                    var messageChunks = message.Replace("Storing to repository:", "").Split(' ').ToList();
                    foreach (var chunk in messageChunks)
                    {
                        if (chunk.Contains("/extract"))
                        {
                            ExtractUrl = chunk.Split('/')[0];
                        }

                        if (chunk.Contains("repoExtractId:"))
                        {
                            ExtractId = chunk.Split(':')[1];
                        }

                        if (chunk.Contains("(guid={"))
                        {
                            ExtractGuid = chunk.Replace("(guid={", "").Replace("})", "");
                        }

                        if (chunk.Contains("size:"))
                        {
                            TwbSize = Int64.Parse(chunk.Split(':')[1]);
                        }
                    }
                    TotalSize = Int64.Parse(messageChunks.Last());
                    ExtractSize = TotalSize - TwbSize;
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