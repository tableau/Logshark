using Logshark.PluginModel.Model;
using System;
using System.Collections.Generic;

namespace Logshark.Core.Controller.Plugin
{
    public class PluginExecutionResult
    {
        public ICollection<Type> PluginsExecuted { get; protected set; }

        public ICollection<IPluginResponse> PluginResponses { get; protected set; }

        public string PluginOutputLocation { get; protected set; }

        public PluginExecutionResult(ICollection<Type> pluginsExecuted, ICollection<IPluginResponse> pluginResponses, string pluginOutputLocation)
        {
            PluginsExecuted = pluginsExecuted;
            PluginResponses = pluginResponses;
            PluginOutputLocation = pluginOutputLocation;
        }
    }
}