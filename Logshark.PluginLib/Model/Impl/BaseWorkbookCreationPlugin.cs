using Logshark.PluginModel.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Logshark.PluginLib.Model.Impl
{
    /// <summary>
    /// The class the workbook creation plugin creators will need to extend from.
    /// </summary>
    public abstract class BaseWorkbookCreationPlugin : BasePlugin, IWorkbookCreationPlugin
    {
        /// <summary>
        /// The names of the embedded workbook files.
        /// </summary>
        public abstract ICollection<string> WorkbookNames { get; }

        protected BaseWorkbookCreationPlugin() { }
        protected BaseWorkbookCreationPlugin(IPluginRequest pluginRequest) : base(pluginRequest) { }

        /// <summary>
        /// Loads the workbook associated with WorkbookName into a stream. The workbook must be set as Embedded Resource.
        /// </summary>
        public virtual Stream GetWorkbook(string workbookName)
        {
            string fullyQualifiedResourceName = GetManifestResourceName(workbookName);

            if (String.IsNullOrWhiteSpace(fullyQualifiedResourceName))
            {
                throw new ArgumentException(String.Format("Resource for {0} not found!", workbookName), workbookName);
            }

            return GetType().Assembly.GetManifestResourceStream(fullyQualifiedResourceName);
        }

        private string GetManifestResourceName(string resourceName)
        {
            return GetType().Assembly.GetManifestResourceNames()
                            .FirstOrDefault(resource => resource.EndsWith("." + resourceName, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}