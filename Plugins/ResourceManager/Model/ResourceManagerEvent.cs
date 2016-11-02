using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
using ServiceStack.DataAnnotations;
using System;
using System.Text.RegularExpressions;

namespace Logshark.Plugins.ResourceManager.Model
{
    public class ResourceManagerEvent
    {
        private static readonly Regex ProcessIdRegex = new Regex(@".*?_(?<process_id>\d+)_.*", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        public Guid LogsetHash { get; set; }

        [Index(Unique = true)]
        public Guid EventHash { get; set; }

        public string ProcessName { get; set; }
        public int WorkerId { get; set; }
        public int? ProcessId { get; set; }
        public DateTime Timestamp { get; set; }
        public int Pid { get; set; }

        public ResourceManagerEvent()
        {
        }

        protected ResourceManagerEvent(BsonDocument document, string processName)
        {
            ProcessName = processName;
            WorkerId = BsonDocumentHelper.GetInt("worker", document);
            Timestamp = BsonDocumentHelper.GetDateTime("ts", document);
            Pid = BsonDocumentHelper.GetInt("pid", document);
            SetProcessId(document);
        }

        protected void SetProcessId(BsonDocument document)
        {
            string filename = BsonDocumentHelper.GetString("file", document);

            if (filename.StartsWith("tabprotosrv")) 
            {
                ProcessId = null;
                return;
            }

            var match = ProcessIdRegex.Match(filename);
            if (match.Success)
            {
                ProcessId = Int32.Parse(match.Groups["process_id"].Value);
            }
            else
            {
                throw new Exception("Could not gather processId information from filename: " + filename);
            }
        }
    }
}