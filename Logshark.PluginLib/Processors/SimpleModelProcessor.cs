using log4net;
using Logshark.PluginLib.StatusWriter;
using Logshark.PluginModel.Model;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading;

namespace Logshark.PluginLib.Processors
{
    public class SimpleModelProcessor<TDocument, TModel> : IDisposable where TModel : new()
    {
        protected readonly IPersister<TModel> persister;

        protected readonly ILog Log;

        public SimpleModelProcessor(IPersister<TModel> persister, ILog log)
        {
            this.persister = persister;
            Log = log;
        }

        public void Process(IMongoCollection<TDocument> documents,
                            QueryDefinition<TDocument> query,
                            Func<TDocument, TModel> transform,
                            FilterDefinition<TDocument> estimationQuery = null,
                            CancellationToken cancellationToken = default(CancellationToken))
        {
            Log.InfoFormat("Processing {0} events..", typeof(TModel).Name);

            using (var statusWriter = BuildPersisterStatusWriter(documents, estimationQuery))
            {
                IAsyncCursor<TDocument> cursor = query.BuildQuery(documents).ToCursor();

                while (cursor.MoveNext(cancellationToken))
                {
                    foreach (TModel model in cursor.Current.Select(transform))
                    {
                        persister.Enqueue(model);
                    }
                }
            }

            Log.InfoFormat("Finished processing {0} events!", typeof(TModel).Name);
        }

        protected PersisterStatusWriter<TModel> BuildPersisterStatusWriter(IMongoCollection<TDocument> documents, FilterDefinition<TDocument> estimationQuery)
        {
            if (estimationQuery != null)
            {
                try
                {
                    long estimatedRecordCount = documents.CountDocuments(estimationQuery);

                    return new PersisterStatusWriter<TModel>(persister,
                                                             logger: Log,
                                                             progressFormatMessage: PluginLibConstants.DEFAULT_PERSISTER_STATUS_WRITER_PROGRESS_MESSAGE_WITH_TOTAL,
                                                             pollIntervalSeconds: 20,
                                                             expectedTotalPersistedItems: estimatedRecordCount);
                }
                catch
                {
                    Log.WarnFormat("Unable to estimate total number of records that will be returned");
                }
            }

            return new PersisterStatusWriter<TModel>(persister, Log);
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