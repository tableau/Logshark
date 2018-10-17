using Logshark.PluginLib.Extensions;
using MongoDB.Bson;
using System;
using System.Text.RegularExpressions;

namespace Logshark.Plugins.ClusterController.Models
{
    public sealed class ZookeeperFsyncLatency : BaseClusterControllerEvent
    {
        private static readonly Regex FsyncLatencyRegex = new Regex(@"fsync-ing the write ahead log in .* took (?<fsync_latency>\d+?)ms.*", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        public int FsyncLatencyMs { get; set; }

        public ZookeeperFsyncLatency()
        {
        }

        public ZookeeperFsyncLatency(BsonDocument document) : base(document)
        {
            FsyncLatencyMs = GetFsyncLatency(document);
        }

        private int GetFsyncLatency(BsonDocument document)
        {
            try
            {
                string fsyncString = document.GetString("message");
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
    }
}