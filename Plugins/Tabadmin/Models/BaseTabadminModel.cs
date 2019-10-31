using Logshark.PluginLib.Extensions;
using MongoDB.Bson;

namespace Logshark.Plugins.Tabadmin.Models
{
    public abstract class BaseTabadminModel
    {
        public string Id { get; set; }

        public string Worker { get; set; }
        public string FilePath { get; set; }
        public string File { get; set; }
        public int Line { get; set; }

        protected BaseTabadminModel()
        {
        }

        protected BaseTabadminModel(BsonDocument document)
        {
            Id = document.GetString("_id");

            Worker = document.GetString("worker");
            FilePath = document.GetString("file_path");
            File = document.GetString("file");
            Line = document.GetInt("line");
        }
    }
}