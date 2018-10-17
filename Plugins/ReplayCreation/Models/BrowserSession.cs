using System.Collections.Generic;

namespace Logshark.Plugins.ReplayCreation.Models
{
    /// <summary>
    /// Stores BrowserSession information
    /// </summary>
    public class BrowserSession
    {
        //URL that is being accessed
        public string Url { get; set; }
        
        public string BrowserStartTime { get; set; }

        //user who executed the browser session
        public string User { get; set; }

        //request status
        public string HttpStatus { get; set; }

        //AccessRequestID from access logs
        public string AccessRequestID { get; set; }

        //Request time showing how long it took  to load
        public string LoadTime { get; set; }

        //correlated VizqlSession matching accessRequestID
        public string VizqlSession { get; set; }

        //commands that were executed in this session
        //has commands and Time
        public List<TabCommand> Commands { get; set; } = new List<TabCommand>();
    }
}