using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
using System;
using System.Text.RegularExpressions;

namespace Logshark.Plugins.ClusterController.Models
{
    public class ZookeeperFsyncLatency : ClusterControllerEvent
    {
        public static Regex FsyncLatencyRegex = new Regex(@"fsync-ing the write ahead log in .* took (?<fsync_latency>\d+?)ms.*", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        public int FsyncLatencyMs { get; set; }

        public ZookeeperFsyncLatency()
        {
        }

        public ZookeeperFsyncLatency(BsonDocument document, Guid logsetHash)
            : base(document, logsetHash)
        {
            FsyncLatencyMs = GetFsyncLatency(document);
            EventHash = GetEventHash();
        }

        protected int GetFsyncLatency(BsonDocument document)
        {
            try
            {
                string fsyncString = BsonDocumentHelper.GetString("message", document);
                var match = FsyncLatencyRegex.Match(fsyncString);
                if (match.Success)
                {
                    return Int32.Parse(match.Groups["fsync_latency"].Value);
                }
                else
                {
                    throw new Exception("Could not gather fsync information from logline: " + fsyncString);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Could not gather fsync information from logline: " + document, ex);
            }
        }

        protected Guid GetEventHash()
        {
            return HashHelper.GenerateHashGuid(Timestamp, Worker, FsyncLatencyMs, Filename, LineNumber);
        }
    }
}