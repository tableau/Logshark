using System.Collections.Generic;

namespace Logshark.PluginLib.Model
{
    public interface IPostExecutionPlugin : IPlugin
    {
        IEnumerable<IPluginResponse> PluginResponses { set; }
    }
}