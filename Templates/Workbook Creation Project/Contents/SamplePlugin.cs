using Logshark.ArtifactProcessors.TableauServerLogProcessor.PluginInterfaces;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginModel.Model;
using Logshark.Plugins.$safeprojectname$.Model;
using System.Collections.Generic;

namespace Logshark.Plugins.$safeprojectname$
{
    /// <summary>
    /// $safeprojectname$ Workbook Creation Plugin
    /// TODO: Add this project as a build dependency to the Logshark.CLI project.
    /// </summary>
    public class $safeprojectname$ : BaseWorkbookCreationPlugin, IServerClassicPlugin, IServerTsmPlugin  // TODO: Change the plugin interface to match the artifact type and add a reference to the correct artifact processor project, if you're working with something other than Tableau Server logs.
    {
        // DATABASE CONNECTIONS:
        // To retrieve a handle on the MongoDB input database, use the property:
        //   MongoDatabase
        // To retrieve a handle on an output extract, use the property:
        //   ExtractFactory

        // LOGGING:
        // To log any notable events to console and to file, use the property:
        //   Log
        // To create a logger handle in other classes in this plugin, just copy & paste the following line.
        //   private static readonly ILog Log = PluginLogFactory.GetLogger(Assembly.GetExecutingAssembly(), MethodBase.GetCurrentMethod());
        
        // List of all of the collections in MongoDB that this plugin is dependent on.        
        public override ISet<string> CollectionDependencies
        {
            get
            {
                return new HashSet<string>
                {
                    // TODO: Enter all of your collection dependencies here.  I.E. "ParserConstants.VizqlServerCppCollectionName"
                };
            }
        }
        
        // List of embedded packaged workbooks to publish at the end of the plugin execution.
        // These workbooks should all be set as "Embedded Resource" in Visual Studio.
        public override ICollection<string> WorkbookNames
        {
            get
            { 
                return new List<string>
                {
                    "$safeprojectname$.twbx"
                };
            }
        }

        public $safeprojectname$() { }
        public $safeprojectname$(IPluginRequest request) : base(request) { }

        public override IPluginResponse Execute()
        {
            // The PluginResponse contains state about whether this plugin ran successfully, as well as any errors encountered.  Append any non-fatal errors to this.
            var pluginResponse = CreatePluginResponse();

            // TODO: Your plugin logic goes here.
            // The common flow here goes:
            //   1. Initialize a persister
            //   2. Query MongoDB to populate instances of a model
            //   3. Enqueue those instances for persistence
            //   4. Validate the plugin's output was successful and set pluginResponse appropriately
            // For example:
            using (var persister = ExtractFactory.CreateExtract<Widget>())
            using (GetPersisterStatusWriter(persister))
            {
                var helloWidget = new Widget { FooType = "Hello", FooRating = 42 };
                var worldWidget = new Widget { FooType = "World", FooRating = 999 };

                persister.Enqueue(helloWidget);
                persister.Enqueue(worldWidget);

                if (persister.ItemsPersisted <= 0)
                {
                    Log.Warn("Failed to persist any data!");
                    pluginResponse.GeneratedNoData = true; // When true, the framework will correctly report a failure and refrain from publishing any resulting workbooks.
                }

                return pluginResponse;
            }
        }
    }
}