using Logshark.Plugins.CustomWorkbooks.Dependencies;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Logshark.Plugins.CustomWorkbooks.Config
{
    internal class ConfigReader
    {
        public static IEnumerable<WorkbookDependencyMapping> LoadWorkbookDependencyMappings(string configPath)
        {
            var document = XElement.Load(configPath);
            var workbookElements = document.Elements("Workbook");

            return workbookElements.Select(BuildWorkbookDependencyMapping).ToList();
        }

        private static WorkbookDependencyMapping BuildWorkbookDependencyMapping(XElement workbookElement)
        {
            var pluginDependencies = workbookElement.Elements("PluginDependency")
                                                    .Select(pluginDependencyElement => pluginDependencyElement.Attribute("name").Value);
            var workbookDependencyMapping = new WorkbookDependencyMapping
            {
                WorkbookName = workbookElement.Attribute("name").Value,
                PluginDependencies = new HashSet<string>(pluginDependencies)
            };
            return workbookDependencyMapping;
        }
    }
}