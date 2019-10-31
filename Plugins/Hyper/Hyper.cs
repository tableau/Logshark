using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.PluginInterfaces;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginLib.Processors;
using Logshark.PluginModel.Model;
using Logshark.Plugins.Hyper.Helpers;
using Logshark.Plugins.Hyper.Models;
using MongoDB.Driver;
using System.Collections.Generic;

namespace Logshark.Plugins.Hyper
{
    public sealed class Hyper : BaseWorkbookCreationPlugin, IServerClassicPlugin, IServerTsmPlugin
    {
        public override ISet<string> CollectionDependencies
        {
            get
            {
                return new HashSet<string>
                {
                    ParserConstants.HyperCollectionName
                };
            }
        }

        public override ICollection<string> WorkbookNames
        {
            get
            {
                return new List<string>
                {
                    "Hyper.twbx"
                };
            }
        }

        public Hyper() { }
        public Hyper(IPluginRequest request) : base(request) { }

        public override IPluginResponse Execute()
        {
            var pluginResponse = CreatePluginResponse();

            bool persistedErrorData = ProcessEvents(Queries.GetErrors());
            bool persistedQueryData = ProcessEvents(Queries.GetQueryEndEvents());

            if (!persistedErrorData && !persistedQueryData)
            {
                Log.Info("Failed to persist any data from Hyper logs!");
                pluginResponse.GeneratedNoData = true;
            }

            return pluginResponse;
        }

        private bool ProcessEvents<T>(FilterDefinition<T> filter) where T : BaseHyperEvent, new()
        {
            var collection = MongoDatabase.GetCollection<T>(ParserConstants.HyperCollectionName);

            using (var persister = ExtractFactory.CreateExtract<T>())
            using (var processor = new SimpleModelProcessor<T, T>(persister, Log))
            {
                processor.Process(collection, new QueryDefinition<T>(filter), item => item, filter);
                return persister.ItemsPersisted > 0;
            }
        }
    }
}