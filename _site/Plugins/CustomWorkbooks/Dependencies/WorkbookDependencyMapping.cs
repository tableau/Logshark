using System.Collections.Generic;

namespace Logshark.Plugins.CustomWorkbooks.Dependencies
{
    public class WorkbookDependencyMapping
    {
        public string WorkbookName { get; internal set; }
        public ISet<string> PluginDependencies { get; internal set; }
    }
}