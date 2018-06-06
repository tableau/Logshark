using Logshark.ArtifactProcessors.TableauDesktopLogProcessor.PluginInterfaces;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.PluginInterfaces;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginModel.Model;
using Logshark.Plugins.CustomWorkbooks.Dependencies;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace Logshark.Plugins.CustomWorkbooks
{
    public class CustomWorkbooks : BaseWorkbookCreationPlugin, IPostExecutionPlugin, IDesktopPlugin, IServerClassicPlugin, IServerTsmPlugin
    {
        private IPluginResponse pluginResponse;
        private ICollection<string> workbookNames;

        private static readonly string WorkbookDirectory = "CustomWorkbooks";
        private static readonly string WorkbookConfigFilename = "CustomWorkbookConfig.xml";

        public IEnumerable<IPluginResponse> PluginResponses { protected get; set; }

        public override ICollection<string> WorkbookNames { get { return workbookNames; } }

        public override ISet<string> CollectionDependencies
        {
            get { return new HashSet<string>(); }
        }

        public override IPluginResponse Execute(IPluginRequest pluginRequest)
        {
            pluginResponse = CreatePluginResponse();

            if (PluginResponses.All(response => response.GeneratedNoData))
            {
                Log.WarnFormat("No plugins associated with this run generated any data; skipping execution of this plugin.");
                pluginResponse.GeneratedNoData = true;
            }
            else
            {
                try
                {
                    var dependencyManager = new WorkbookDependencyManager(PluginResponses, WorkbookDirectory, WorkbookConfigFilename);
                    workbookNames = dependencyManager.GetValidWorkbooks();
                }
                catch (Exception ex)
                {
                    string errorMessage = String.Format("Failed to generate custom workbooks: {0}", ex.Message);
                    Log.Error(errorMessage);
                    pluginResponse.SetExecutionOutcome(false, errorMessage);
                }
            }

            return pluginResponse;
        }

        /// <summary>
        /// Loads the workbook associated with WorkBookName into an XmlDocument.
        /// </summary>
        /// <returns>XmlDocument containing the full body of the workbook.</returns>
        public override XmlDocument GetWorkbookXml(string workbookName)
        {
            XmlDocument doc = new XmlDocument();
            string workbookPath = Path.Combine(GetApplicationRootDirectory(), WorkbookDirectory, workbookName);
            doc.Load(workbookPath);
            return doc;
        }

        /// <summary>
        /// Returns the absolute path to the root Logshark directory.
        /// </summary>
        /// <returns></returns>
        private static string GetApplicationRootDirectory()
        {
            var pluginDirectory = new DirectoryInfo(Assembly.GetAssembly(typeof(CustomWorkbooks)).Location);
            return pluginDirectory.Parent.Parent.FullName;
        }
    }
}