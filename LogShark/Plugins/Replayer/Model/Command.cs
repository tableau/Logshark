using Newtonsoft.Json;
using System.Collections.Generic;

namespace Logshark.Plugins.Replayer.Models
{
    /// <summary>
    /// Tableau command structure that gets executed as javascript
    /// </summary>
    public class Command
    {
        public Command(string cmmdNameSpace, string cmdName, Dictionary<string, object> cmdParams)
        {
            CommandNamespace = cmmdNameSpace;
            CommandName = cmdName;
            CommandParams = cmdParams;
        }

        // namespace of the command "tabdoc" or "tabsrv"
        [JsonProperty("commandNamespace")]
        public string CommandNamespace { get; set; }

        // commandName "Select", "sort-from-indicator"
        [JsonProperty("commandName")]
        public string CommandName { get; set; }

        // parameters for the command
        [JsonProperty("commandParams")]
        public Dictionary<string, object> CommandParams { get; set; }
    }
}