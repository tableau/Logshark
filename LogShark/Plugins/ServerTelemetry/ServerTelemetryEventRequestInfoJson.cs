using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogShark.Plugins.ServerTelemetry
{
    public class ServerTelemetryEventRequestInfoJson
    {
        [JsonProperty(PropertyName = "action-name")]
        public string ActionName { get; set; }

        [JsonProperty(PropertyName = "action-result-size-bytes")]
        public int? ActionSizeBytes { get; set; }

        [JsonProperty(PropertyName = "action-type")]
        public string ActionType { get; set; }
       
        [JsonProperty(PropertyName = "annotation-count")]
        public int? AnnotationCount { get; set; }

        [JsonProperty(PropertyName = "client-render-mode")]
        public string ClientRenderMode { get; set; }

        [JsonProperty(PropertyName = "customshape-count")]
        public int? CustomShapeCount { get; set; }

        [JsonProperty(PropertyName = "customshape-pixel-count")]
        public int? CustomShapePixelCount { get; set; }
        
        [JsonProperty(PropertyName = "encoding-count")]
        public int? EncodingCount { get; set; }
        
        [JsonProperty(PropertyName = "filterfield-count")]
        public int? FilterFieldCount { get; set; }

        [JsonProperty(PropertyName = "height")]
        public int? Height { get; set; }

        [JsonProperty(PropertyName = "is-dashboard")]
        public string IsDashboard { get; set; }

        [JsonProperty(PropertyName = "node-count")]
        public int? NodeCount { get; set; }

        [JsonProperty(PropertyName = "num-views")]
        public int? NumViews { get; set; }

        [JsonProperty(PropertyName = "num-zones")]
        public int? NumZones { get; set; }

        [JsonProperty(PropertyName = "mark-count")]
        public int? MarkCount { get; set; }

        [JsonProperty(PropertyName = "marklabel-count")]
        public int? MarkLabelCount { get; set; }
        
        [JsonProperty(PropertyName = "pane-count")]
        public int? PaneCount { get; set; }     

        [JsonProperty(PropertyName = "refline-count")]
        public int? ReflineCount { get; set; }

        [JsonProperty(PropertyName = "rid")]
        public string RequestId { get; set; }
        
        [JsonProperty(PropertyName = "repository-url")]
        public string RepositoryURL { get; set; }
        
        [JsonProperty(PropertyName = "session-state")]
        public string SessionState { get; set; }
        
        [JsonProperty(PropertyName = "sheetname")]
        public string SheetName { get; set; }
        
        [JsonProperty(PropertyName = "textmark-count")]
        public int? TextMarkCount { get; set; }

        [JsonProperty(PropertyName = "tooltip-count")]
        public int? TooltipCount { get; set; }

        [JsonProperty(PropertyName = "transparent-linemark-count")]
        public int? TransparentLinemarkCount { get; set; }
        
        [JsonProperty(PropertyName = "vertex-count")]
        public int? VertexCount { get; set; }
        
        [JsonProperty(PropertyName = "width")]
        public int? Width { get; set; }

        [JsonProperty(PropertyName = "metrics")]
        public JToken Metrics { get; set; }
    }
}