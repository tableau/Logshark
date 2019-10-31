using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.PluginInterfaces;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginLib.Processors;
using Logshark.PluginModel.Model;
using Logshark.Plugins.Tabadmin.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;

namespace Logshark.Plugins.Tabadmin
{
    /// <summary>
    /// Process tabadmin log data and extract action, error, and version information.
    /// </summary>
    public class Tabadmin : BaseWorkbookCreationPlugin, IServerClassicPlugin
    {
        public override ISet<string> CollectionDependencies
        {
            get
            {
                return new HashSet<string>
                {
                    ParserConstants.TabAdminCollectionName
                };
            }
        }

        public override ICollection<string> WorkbookNames
        {
            get
            {
                return new List<string>
                {
                    "Tabadmin.twbx"
                };
            }
        }

        public Tabadmin() { }
        public Tabadmin(IPluginRequest request) : base(request) { }

        public override IPluginResponse Execute()
        {
            var pluginResponse = CreatePluginResponse();

            IMongoCollection<BsonDocument> collection = MongoDatabase.GetCollection<BsonDocument>(ParserConstants.TabAdminCollectionName);

            Log.Info("Processing Tableau Server version data from tabadmin logs...");
            var versions = TabadminVersionProcessor.BuildVersionTimeline(collection).ToList();

            bool persistedData;

            using (var persister = ExtractFactory.CreateExtract<TableauServerVersion>())
            using (GetPersisterStatusWriter(persister, versions.Count))
            {
                persister.Enqueue(versions);
                persistedData = persister.ItemsPersisted > 0;
            }

            using (var persister = ExtractFactory.CreateExtract<TabadminError>())
            using (var processor = new SimpleModelProcessor<BsonDocument, TabadminError>(persister, Log))
            {
                var filter = BuildTabadminErrorFilter();
                processor.Process(collection, new QueryDefinition<BsonDocument>(filter), document => new TabadminError(document, versions), filter);

                persistedData = persistedData || persister.ItemsPersisted > 0;
            }

            using (var persister = ExtractFactory.CreateExtract<TabadminAction>())
            using (var processor = new SimpleModelProcessor<BsonDocument, TabadminAction>(persister, Log))
            {
                var filter = BuildTabadminActionFilter();
                processor.Process(collection, new QueryDefinition<BsonDocument>(filter), document => new TabadminAction(document, versions), filter);

                persistedData = persistedData || persister.ItemsPersisted > 0;
            }

            if (!persistedData)
            {
                Log.Info("Failed to persist any data from Tabadmin logs!");
                pluginResponse.GeneratedNoData = true;
            }

            return pluginResponse;
        }

        private static FilterDefinition<BsonDocument> BuildTabadminErrorFilter()
        {
            string[] errorSeverities = { "WARN", "ERROR", "FATAL" };
            return Builders<BsonDocument>.Filter.In("sev", errorSeverities);
        }

        private static FilterDefinition<BsonDocument> BuildTabadminActionFilter()
        {
            return Builders<BsonDocument>.Filter.Regex("message", new BsonRegularExpression("/^run as: <script>/"));
        }
    }
}