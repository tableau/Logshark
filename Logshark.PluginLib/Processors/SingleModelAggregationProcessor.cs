using log4net;
using Logshark.PluginLib.StatusWriter;
using Logshark.PluginModel.Model;
using MongoDB.Driver;
using Optional;
using System;

namespace Logshark.PluginLib.Processors
{
    public abstract class SingleModelAggregationProcessor<TDocument, TModel> : IDisposable where TModel : new()
    {
        protected readonly IPluginRequest pluginRequest;
        protected readonly IMongoDatabase mongoDatabase;
        protected readonly IPersister<TModel> persister;

        protected readonly ILog Log;

        protected SingleModelAggregationProcessor(IPluginRequest pluginRequest,
                                                  IMongoDatabase mongoDatabase,
                                                  IPersister<TModel> persister,
                                                  ILog log)
        {
            this.pluginRequest = pluginRequest;
            this.mongoDatabase = mongoDatabase;
            this.persister = persister;
            Log = log;
        }

        public abstract Option<TModel> Process(FilterDefinition<TDocument> filter);

        public Option<TModel> Persist(TModel model)
        {
            try
            {
                using (new PersisterStatusWriter<TModel>(persister, Log))
                {
                    persister.Enqueue(model);
                }

                return model.Some();
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Failed to persist {0}: {1}", typeof(TModel).Name, ex.Message);
                return Option.None<TModel>();
            }
        }

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (persister != null)
                {
                    persister.Dispose();
                }
            }
        }

        #endregion IDisposable Implementation
    }
}