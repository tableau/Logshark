using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.PluginInterfaces;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginLib.Processors;
using Logshark.PluginModel.Model;
using Logshark.Plugins.Art.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;

namespace Logshark.Plugins.Art
{
    /// <summary>
    /// Art (Activity and Resource Tracing) Workbook Creation Plugin
    /// </summary>
    public class Art : BaseWorkbookCreationPlugin, IServerClassicPlugin, IServerTsmPlugin
    {
        public override ISet<string> CollectionDependencies => new HashSet<string>
        {
            ParserConstants.VizqlServerCppCollectionName
        };

        public override ICollection<string> WorkbookNames => new List<string>
        {
            "Art.twbx"
        };

        public Art() { }
        public Art(IPluginRequest request) : base(request) { }

        public override IPluginResponse Execute()
        {
            // The PluginResponse contains state about whether this plugin ran successfully, as well as any errors encountered.  Append any non-fatal errors to this.
            var pluginResponse = CreatePluginResponse();

            var collection = MongoDatabase.GetCollection<FlattenedArtEvent>(ParserConstants.VizqlServerCppCollectionName);
            var filter = Builders<FlattenedArtEvent>.Filter.Where(line => line.ArtData != null);
            using (var persister = ExtractFactory.CreateExtract<FlattenedArtEvent>("Art.hyper"))
            using (var processor = new SimpleModelProcessor<FlattenedArtEvent, FlattenedArtEvent>(persister, Log))
            {
                processor.Process(collection, new QueryDefinition<FlattenedArtEvent>(filter), item => item, filter);
                
                if (persister.ItemsPersisted == 0)
                {
                    Log.Info("Failed to persist any ART events!");
                    pluginResponse.GeneratedNoData = true;
                }
            }
            
            return pluginResponse;
        }
    }
}