using log4net;
using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Logging;
using Logshark.PluginLib.Persistence;
using Logshark.PluginLib.StatusWriter;
using Logshark.PluginModel.Model;
using MongoDB.Driver;
using Optional;
using ServiceStack.OrmLite;
using System;
using System.Reflection;

namespace Logshark.PluginLib.Processors
{
    public abstract class SingleModelAggregationProcessor<TDocument, TModel> where TModel : new()
    {
        protected readonly IPluginRequest pluginRequest;
        protected readonly IMongoDatabase mongoDatabase;
        protected readonly IDbConnectionFactory outputConnectionFactory;
        protected readonly IPersisterFactory<TModel> persisterFactory;

        private static readonly ILog Log = PluginLogFactory.GetLogger(Assembly.GetExecutingAssembly(), MethodBase.GetCurrentMethod());

        protected SingleModelAggregationProcessor(IPluginRequest pluginRequest,
                                                  IMongoDatabase mongoDatabase,
                                                  IDbConnectionFactory outputConnectionFactory,
                                                  IPersisterFactory<TModel> persisterFactory)
        {
            this.pluginRequest = pluginRequest;
            this.mongoDatabase = mongoDatabase;
            this.outputConnectionFactory = outputConnectionFactory;
            this.persisterFactory = persisterFactory;
        }

        public abstract Option<TModel> Process(FilterDefinition<TDocument> filter);

        public Option<TModel> Persist(TModel model)
        {
            try
            {
                using (var connection = outputConnectionFactory.OpenDbConnection())
                {
                    connection.CreateOrMigrateTable<TModel>();
                }

                using (IPersister<TModel> persister = persisterFactory.BuildPersister())
                using (new PersisterStatusWriter<TModel>(persister, Log))
                {
                    persister.Enqueue(model);
                    persister.Shutdown();
                }

                return model.Some();
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Failed to persist {0}: {1}", typeof(TModel).Name, ex.Message);
                return Option.None<TModel>();
            }
        }
    }
}