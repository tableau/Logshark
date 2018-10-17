using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.PluginInterfaces;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginLib.Processors;
using Logshark.PluginModel.Model;
using Logshark.Plugins.Filestore.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;

namespace Logshark.Plugins.Filestore
{
    public class Filestore : BaseWorkbookCreationPlugin, IServerClassicPlugin, IServerTsmPlugin
    {
        public override ISet<string> CollectionDependencies
        {
            get
            {
                return new HashSet<string>
                {
                    ParserConstants.FilestoreCollectionName
                };
            }
        }

        public override ICollection<string> WorkbookNames
        {
            get
            {
                return new List<string>
                {
                    "Filestore.twbx"
                };
            }
        }

        public Filestore() { }
        public Filestore(IPluginRequest request) : base(request) { }

        public override IPluginResponse Execute()
        {
            var pluginResponse = CreatePluginResponse();

            IMongoCollection<FilestoreEvent> collection = MongoDatabase.GetCollection<FilestoreEvent>(ParserConstants.FilestoreCollectionName);

            using (var persister = ExtractFactory.CreateExtract<FilestoreEvent>("FilestoreEvents.hyper"))
            using (var processor = new SimpleModelProcessor<FilestoreEvent, FilestoreEvent>(persister, Log))
            {
                var filestoreEventFilter = Builders<FilestoreEvent>.Filter.Regex("file", new BsonRegularExpression("filestore.*"));

                processor.Process(collection, new QueryDefinition<FilestoreEvent>(filestoreEventFilter), item => item, filestoreEventFilter);

                if (persister.ItemsPersisted <= 0)
                {
                    Log.Warn("Failed to persist any data from Filestore logs!");
                    pluginResponse.GeneratedNoData = true;
                }

                return pluginResponse;
            }
        }
    }
}