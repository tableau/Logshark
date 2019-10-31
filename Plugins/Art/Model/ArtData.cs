using System;
using System.Collections.Generic;
using System.ComponentModel;
using MongoDB.Bson.Serialization.Attributes;
using Tableau.ExtractApi.DataAttributes;
using MongoDB.Bson;

namespace Logshark.Plugins.Art.Model
{
    // Schema located at: https://mytableau.tableaucorp.com/display/devft/Quick+Start
    // "required by start event": [ "id", "name", "type", "depth" ],
    // "required by end event": [ "id", "name", "type", "depth", "elapsed", "res", "rk", "rv" ],
    [BsonIgnoreExtraElements]
    public class ArtData : ISupportInitialize
    {
        #region Shared between Begin and End
        
        [BsonElement("id")]
        public string UniqueId { get; set; }

        [BsonElement("depth")]
        public int Depth { get; set; }

        [BsonElement("req-desc")]
        public string Description { get; set; }
        
        [BsonElement("name")]
        public string Name { get; set; }
        
        [BsonElement("type")]
        public string Type { get; set; }
        
        [BsonElement("vw")]
        [BsonIgnoreIfNull]
        public string View { get; set; }
        
        [BsonElement("wb")]
        [BsonIgnoreIfNull]
        public string Workbook { get; set; }
        
        [BsonElement("v")]
        [BsonIgnoreIfNull]
        public string CustomAttributes { get; set; }
        
        [BsonElement("sponsor")]
        [BsonIgnoreIfNull]
        public string SponsorId { get; set; }
        
        [BsonElement("begin")]
        [BsonIgnoreIfNull]
        [ExtractIgnore]
        public DateTime? BeginTimestamp { get; set; }
        
        #endregion Shared between Begin and End
        
        #region Only for Begin
        
        [BsonElement("root")]
        [BsonIgnoreIfNull]
        public string RootId { get; set; }
        
        #endregion Only for Begin 
        
        #region Only for End
        
        [BsonElement("elapsed")]
        [BsonIgnoreIfNull]
        public double ElapsedSeconds { get; set; }
        
        [BsonElement("res")]
        [BsonIgnoreIfNull]
        public ResourceConsumptionMetrics ResourceConsumptionMetrics { get; set; }
        
        [BsonElement("rk")]
        [BsonIgnoreIfNull]
        public string ResultKey { get; set; }
        
        [BsonElement("rv")]
        [BsonIgnoreIfNull]
        public object ResultValue { get; set; }
        
        [BsonElement("end")]
        [BsonIgnoreIfNull]
        public DateTime? EndTimestamp { get; set; }
        
        #endregion Only for End
        
        [BsonExtraElements]
        [ExtractIgnore]
        public IDictionary<string, object> ExtraElements { get; set; }

        public void BeginInit()
        {
        }

        public void EndInit()
        {
            if (ExtraElements != null)
            {
                if (ExtraElements.ContainsKey("workbook") && ExtraElements["workbook"] is string)
                {
                    Workbook = (string) ExtraElements["workbook"];
                }
                if (ExtraElements.ContainsKey("view") && ExtraElements["view"] is string)
                {
                    View = (string) ExtraElements["view"];
                }
                if (ExtraElements.ContainsKey("result-c") && ExtraElements["result-c"] is string)
                {
                    ResultKey = (string) ExtraElements["result-c"];
                }
                if (ExtraElements.ContainsKey("result-i") && ExtraElements["result-i"] is string)
                {
                    ResultValue = (string)ExtraElements["result-i"];
                }

            }
        }
    }
}