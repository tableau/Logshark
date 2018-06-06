using Logshark.PluginLib.Extensions;
using MongoDB.Bson.Serialization.Attributes;
using ServiceStack.DataAnnotations;
using System.Collections.Generic;

namespace Logshark.Plugins.Hyper.Models
{
    [Alias("hyper_errors")]
    public class HyperError : BaseHyperEvent
    {
        [BsonElement("v")]
        [Ignore]
        public IDictionary<string, object> ValuePayload { get; set; }

        [BsonIgnore]
        public string Value
        {
            get
            {
                return ValuePayload == null ? null : ValuePayload.PrintFormatted("{0}: {1}");
            }
        }
    }
}