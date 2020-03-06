using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogShark.Plugins.Art.Model
{
    public class ArtData
    {
        #region Shared between Begin and End
        
        [JsonProperty(PropertyName = "id")]
        public string UniqueId { get; set; }

        [JsonProperty(PropertyName = "depth")]
        public int Depth { get; set; }
        
        [JsonProperty(PropertyName = "req-desc")]
        public string Description { get; set; }
        
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
        
        [JsonProperty(PropertyName = "vw")]
        public string View { get; set; }
        
        [JsonProperty(PropertyName = "view")]
        public string ViewOldFormat { get; set; }
        
        [JsonProperty(PropertyName = "wb")]
        public string Workbook { get; set; }
        
        [JsonProperty(PropertyName = "workbook")]
        public string WorkbookOldFormat { get; set; }
        
        [JsonProperty(PropertyName = "v")]
        public string CustomAttributes { get; set; }
        
        [JsonProperty(PropertyName = "sponsor")]
        public string SponsorId { get; set; }
        
        [JsonProperty(PropertyName = "begin")]
        public DateTime? BeginTimestamp { get; set; }
        
        #endregion Shared between Begin and End
        
        #region Only for Begin
        
        [JsonProperty(PropertyName = "root")]
        public string RootId { get; set; }
        
        #endregion Only for Begin 
        
        #region Only for End
        
        [JsonProperty(PropertyName = "elapsed")]
        public double ElapsedSeconds { get; set; }
        
        [JsonProperty(PropertyName = "res")]
        public ResourceConsumptionMetrics ResourceConsumptionMetrics { get; set; }
        
        [JsonProperty(PropertyName = "rk")]
        public string ResultKey { get; set; }
        
        [JsonProperty(PropertyName = "result-c")]
        public string ResultKeyOldFormat { get; set; }
        
        [JsonProperty(PropertyName = "rv")]
        public JToken ResultValue { get; set; }
        
        [JsonProperty(PropertyName = "result-i")]
        public string ResultValueOldFormat { get; set; }
        
        [JsonProperty(PropertyName = "end")]
        public DateTime? EndTimestamp { get; set; }
        
        #endregion Only for End
    }
}