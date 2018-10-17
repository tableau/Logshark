using Logshark.PluginLib.Extensions;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using Tableau.ExtractApi.DataAttributes;

namespace Logshark.Plugins.Hyper.Models
{
    public class HyperError : BaseHyperEvent
    {
        [BsonElement("v")]
        [ExtractIgnore]
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