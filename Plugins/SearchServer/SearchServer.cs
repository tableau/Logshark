using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.PluginInterfaces;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginLib.Processors;
using Logshark.PluginModel.Model;
using Logshark.Plugins.SearchServer.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;

namespace Logshark.Plugins.SearchServer
{
    public class SearchServer : BaseWorkbookCreationPlugin, IServerClassicPlugin, IServerTsmPlugin
    {
        public override ISet<string> CollectionDependencies
        {
            get
            {
                return new HashSet<string>
                {
                    ParserConstants.SearchServerCollectionName
                };
            }
        }

        public override ICollection<string> WorkbookNames
        {
            get
            {
                return new List<string>
                {
                    "SearchServer.twbx"
                };
            }
        }

        public SearchServer() { }
        public SearchServer(IPluginRequest request) : base(request) { }

        public override IPluginResponse Execute()
        {
            var pluginResponse = CreatePluginResponse();

            IMongoCollection<SearchServerEvent> collection = MongoDatabase.GetCollection<SearchServerEvent>(ParserConstants.SearchServerCollectionName);

            using (var persister = ExtractFactory.CreateExtract<SearchServerEvent>("SearchServerEvents.hyper"))
            using (var processor = new SimpleModelProcessor<SearchServerEvent, SearchServerEvent>(persister, Log))
            {
                var searchServerEventFilter = Builders<SearchServerEvent>.Filter.Regex("file", new BsonRegularExpression("searchserver.*"));

                processor.Process(collection, new QueryDefinition<SearchServerEvent>(searchServerEventFilter), item => item, searchServerEventFilter);

                if (persister.ItemsPersisted <= 0)
                {
                    Log.Warn("Failed to persist any data from SearchServer logs!");
                    pluginResponse.GeneratedNoData = true;
                }

                return pluginResponse;
            }
        }
    }
}