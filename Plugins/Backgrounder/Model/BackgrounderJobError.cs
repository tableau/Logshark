using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
using System;

namespace Logshark.Plugins.Backgrounder.Model
{
    internal class BackgrounderJobError
    {
        public long BackgrounderJobId { get; set; }

        public DateTime Timestamp { get; set; }
        public string Site { get; set; }
        public string Thread { get; set; }
        public string Severity { get; set; }
        public string Class { get; set; }
        public string Message { get; set; }
        public string File { get; set; }
        public int Line { get; set; }

        public BackgrounderJobError() { }

        public BackgrounderJobError(long backgrounderJobId, BsonDocument errorDocument)
        {
            BackgrounderJobId = backgrounderJobId;
            Timestamp = BsonDocumentHelper.GetDateTime("ts", errorDocument);
            Site = BsonDocumentHelper.GetString("site", errorDocument);
            Thread = BsonDocumentHelper.GetString("thread", errorDocument);
            Severity = BsonDocumentHelper.GetString("sev", errorDocument);
            Class = BsonDocumentHelper.GetString("class", errorDocument);
            Message = BsonDocumentHelper.GetString("message", errorDocument);
            File = String.Format(@"{0}\{1}", BsonDocumentHelper.GetString("file_path", errorDocument), BsonDocumentHelper.GetString("file", errorDocument));
            Line = BsonDocumentHelper.GetInt("line", errorDocument);
        }
    }
}