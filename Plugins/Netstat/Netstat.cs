using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.PluginInterfaces;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginModel.Model;
using Logshark.Plugins.Netstat.Helpers;
using Logshark.Plugins.Netstat.Model;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;

namespace Logshark.Plugins.Netstat
{
    public sealed class Netstat : BaseWorkbookCreationPlugin, IServerClassicPlugin, IServerTsmPlugin
    {
        private static readonly ISet<string> CollectionDependenciesStatic = new HashSet<string> { ParserConstants.NetstatCollectionName };
        private static readonly ICollection<string> WorkbookNamesStatic = new List<string> { "Netstat.twbx" };

        public override ISet<string> CollectionDependencies => CollectionDependenciesStatic;
        public override ICollection<string> WorkbookNames => WorkbookNamesStatic;

        public Netstat() { }
        public Netstat(IPluginRequest request) : base(request) { }

        public override IPluginResponse Execute()
        {
            IPluginResponse pluginResponse = CreatePluginResponse();

            var collection = MongoDatabase.GetCollection<NetstatDocument>(ParserConstants.NetstatCollectionName);

            using (var persister = ExtractFactory.CreateExtract<NetstatActiveConnection>("NetstatEntries.hyper"))
            using (GetPersisterStatusWriter(persister))
            {
                foreach (var worker in NetstatQueries.GetDistinctWorkers(collection))
                {
                    var activeConnectionsForWorker = GetActiveConnectionsForWorker(worker, collection);
                    persister.Enqueue(activeConnectionsForWorker);
                }

                if (persister.ItemsPersisted <= 0)
                {
                    Log.Warn("Failed to persist any Netstat data!");
                    pluginResponse.GeneratedNoData = true;
                }

                return pluginResponse;
            }
        }

        private IEnumerable<NetstatActiveConnection> GetActiveConnectionsForWorker(string worker, IMongoCollection<NetstatDocument> collection)
        {
            Log.InfoFormat($"Retrieving netstat information for worker '{worker}'..");

            var netstatQuery = NetstatQueries.GetNetstatForWorker(worker);

            var netstatForWorker = collection.Find(netstatQuery).FirstOrDefault();
            if (netstatForWorker?.ActiveConnections == null)
            {
                Log.WarnFormat($"No netstat data found for worker '{worker}'");
                return Enumerable.Empty<NetstatActiveConnection>();
            }

            return netstatForWorker.ActiveConnections.Select(entry => new NetstatActiveConnection(entry, netstatForWorker.Worker, netstatForWorker.FileLastModified));
        }
    }
}