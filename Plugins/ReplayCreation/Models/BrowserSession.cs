using Newtonsoft.Json;
using System.Collections.Generic;

namespace Logshark.Plugins.ReplayCreation.Models
{
    /// <summary>
    /// Stores BrowserSession information
    /// </summary>
    public class BrowserSession
    {
        //URL that is being accessed
        [JsonProperty("Url")]
        public string Url { get; set; }

        [JsonProperty("BrowserStartTime")]
        public string BrowserStartTime { get; set; }

        //user who executed the browser session
        [JsonProperty("User")]
        public string User { get; set; }

        //request status
        [JsonProperty("HttpStatus")]
        public string HttpStatus { get; set; }

        //AccessRequestID from access logs
        [JsonProperty("AccessRequestID")]
        public string AccessRequestID { get; set; }

        //Request time showing how long it took  to load
        [JsonProperty("LoadTime")]
        public string LoadTime { get; set; }

        //correlated VizqlSession matching accessRequestID
        [JsonProperty("VizqlSession")]
        public string VizqlSession { get; set; }

        //commands that were executed in this session
        //has commands and Time
        [JsonProperty("Commands")]
        public List<TabCommand> Commands { get; set; } = new List<TabCommand>();
    }
}