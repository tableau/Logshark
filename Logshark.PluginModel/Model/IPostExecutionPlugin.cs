using System.Collections.Generic;

namespace Logshark.PluginModel.Model
{
    public interface IPostExecutionPlugin : IPlugin
    {
        IEnumerable<IPluginResponse> PluginResponses { set; }
    }
}