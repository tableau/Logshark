using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.PluginInterfaces;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginLib.Processors;
using Logshark.PluginModel.Model;
using Logshark.Plugins.Vizportal.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;

namespace Logshark.Plugins.Vizportal
{
    public class Vizportal : BaseWorkbookCreationPlugin, IServerClassicPlugin, IServerTsmPlugin
    {
        public override ISet<string> CollectionDependencies
        {
            get
            {
                return new HashSet<string>
                {
                    ParserConstants.VizportalJavaCollectionName
                };
            }
        }

        public override ICollection<string> WorkbookNames
        {
            get
            {
                return new List<string>
                {
                    "Vizportal.twbx"
                };
            }
        }

        public Vizportal() { }
        public Vizportal(IPluginRequest request) : base(request) { }

        public override IPluginResponse Execute()
        {
            var pluginResponse = CreatePluginResponse();

            IMongoCollection<VizportalEvent> collection = MongoDatabase.GetCollection<VizportalEvent>(ParserConstants.VizportalJavaCollectionName);

            using (var persister = ExtractFactory.CreateExtract<VizportalEvent>("VizportalEvents.hyper"))
            using (var processor = new SimpleModelProcessor<VizportalEvent, VizportalEvent>(persister, Log))
            {
                var vizportalEventFilter = Builders<VizportalEvent>.Filter.Regex("file", new BsonRegularExpression("vizportal.*"));

                processor.Process(collection, new QueryDefinition<VizportalEvent>(vizportalEventFilter), item => item, vizportalEventFilter);

                if (persister.ItemsPersisted <= 0)
                {
                    Log.Warn("Failed to persist any data from Vizportal logs!");
                    pluginResponse.GeneratedNoData = true;
                }

                return pluginResponse;
            }
        }
    }
}