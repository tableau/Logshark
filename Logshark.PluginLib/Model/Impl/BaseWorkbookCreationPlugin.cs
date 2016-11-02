using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

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

        /// <summary>
        /// Loads the workbook associated with WorkBookName into an XmlDocument. The workbook must be set as Embedded Resource.
        /// </summary>
        /// <returns>XmlDocument containing the full body of the workbook.</returns>
        public virtual XmlDocument GetWorkbookXml(string workbookName)
        {
            string fullyQualifiedResourceName = null;
            foreach (var resource in GetType().Assembly.GetManifestResourceNames())
            {
                if (resource.EndsWith("." + workbookName, StringComparison.InvariantCultureIgnoreCase))
                {
                    fullyQualifiedResourceName = resource;
                }
            }

            if (String.IsNullOrWhiteSpace(fullyQualifiedResourceName))
            {
                throw new ArgumentNullException(String.Format("Resource for {0} not found!", workbookName));
            }

            XmlDocument doc = new XmlDocument();
            Stream filestream = GetType().Assembly.GetManifestResourceStream(fullyQualifiedResourceName);
            doc.Load(filestream);
            return doc;
        }
    }
}