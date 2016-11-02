using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Model;
using Logshark.PluginLib.Model.Impl;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Logshark.Plugins.$safeprojectname$
{
    public class $safeprojectname$ : BaseWorkbookCreationPlugin, IServerPlugin  // Change "IServerPlugin" to "IDesktopPlugin" if this is a Tableau Desktop plugin.
    {
        // The PluginResponse contains state about whether this plugin ran successfully, as well as any errors encountered.  Append any non-fatal errors to this.
        private IPluginResponse pluginResponse;

        // DATABASE CONNECTIONS:
        // To retrieve a handle on the MongoDB input database, use the property:
        //   MongoDatabase
        // To retrieve a database connection to the Postgres output database, use the method:
        //   GetOutputDatabaseConnection()

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
                    // Enter all of your collection dependencies here.  I.E. "vizqlserver_cpp"
                };
            }
        }
        
        // List of embedded workbooks to publish at the end of the plugin execution.
        // These workbooks should all be set as "Embedded Resource" in Visual Studio.
        public override ICollection<string> WorkbookNames
        {
            get
            { 
                return new List<string>
                {
                    "$safeprojectname$.twb"
                };
            }
        }

        public override IPluginResponse Execute(IPluginRequest pluginRequest)
        {
            pluginResponse = CreatePluginResponse();

            // Your plugin logic goes here.

            return pluginResponse;
        }
    }
}