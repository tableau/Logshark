using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.PluginInterfaces;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginLib.Processors;
using Logshark.PluginModel.Model;
using Logshark.Plugins.Postgres.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;

namespace Logshark.Plugins.Postgres
{
    public sealed class Postgres : BaseWorkbookCreationPlugin, IServerClassicPlugin, IServerTsmPlugin
    {
        public override ISet<string> CollectionDependencies
        {
            get
            {
                return new HashSet<string>
                {
                    ParserConstants.PgSqlCollectionName
                };
            }
        }

        public override ICollection<string> WorkbookNames
        {
            get
            {
                return new List<string>
                {
                    "Postgres.twbx"
                };
            }
        }

        public Postgres() { }
        public Postgres(IPluginRequest request) : base(request) { }

        public override IPluginResponse Execute()
        {
            var pluginResponse = CreatePluginResponse();

            IMongoCollection<PostgresEvent> collection = MongoDatabase.GetCollection<PostgresEvent>(ParserConstants.PgSqlCollectionName);

            using (var persister = ExtractFactory.CreateExtract<PostgresEvent>("PostgresEvents.hyper"))
            using (var processor = new SimpleModelProcessor<PostgresEvent, PostgresEvent>(persister, Log))
            {
                var postgresEventFilter = Builders<PostgresEvent>.Filter.Regex("file", new BsonRegularExpression("postgresql-*"));

                processor.Process(collection, new QueryDefinition<PostgresEvent>(postgresEventFilter), item => item, postgresEventFilter);

                if (persister.ItemsPersisted <= 0)
                {
                    Log.Warn("Failed to persist any data from Postgres logs!");
                    pluginResponse.GeneratedNoData = true;
                }

                return pluginResponse;
            }
        }
    }
}