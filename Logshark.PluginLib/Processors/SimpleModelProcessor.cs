using log4net;
using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Logging;
using Logshark.PluginLib.Persistence;
using Logshark.PluginLib.StatusWriter;
using Logshark.PluginModel.Model;
using MongoDB.Driver;
using ServiceStack.Common.Extensions;
using ServiceStack.OrmLite;
using System;
using System.Reflection;
using System.Threading;

namespace Logshark.PluginLib.Processors
{
    public class SimpleModelProcessor<TDocument, TModel> where TModel : new()
    {
        protected readonly IPluginRequest pluginRequest;
        protected readonly IDbConnectionFactory outputConnectionFactory;
        protected readonly IPersisterFactory<TModel> persisterFactory;

        private static readonly ILog Log = PluginLogFactory.GetLogger(Assembly.GetExecutingAssembly(), MethodBase.GetCurrentMethod());

        public SimpleModelProcessor(IPluginRequest pluginRequest, IDbConnectionFactory outputConnectionFactory, IPersisterFactory<TModel> persisterFactory)
        {
            this.pluginRequest = pluginRequest;
            this.outputConnectionFactory = outputConnectionFactory;
            this.persisterFactory = persisterFactory;
        }

        public void Process(IMongoCollection<TDocument> documents,
                            QueryDefinition<TDocument> query,
                            Func<TDocument, TModel> transform,
                            CancellationToken cancellationToken = default(CancellationToken))
        {
            Log.InfoFormat("Processing {0} events..", typeof(TModel).Name);

            using (var connection = outputConnectionFactory.OpenDbConnection())
            {
                connection.CreateOrMigrateTable<TModel>();
            }

            using (IPersister<TModel> persister = persisterFactory.BuildPersister())
            using (new PersisterStatusWriter<TModel>(persister, Log))
            {
                IAsyncCursor<TDocument> cursor = query.BuildQuery(documents).ToCursor();

                while (cursor.MoveNext(cancellationToken))
                {
                    cursor.Current.ForEach(document =>
                    {
                        TModel model = transform(document);
                        persister.Enqueue(model);
                    });
                }

                persister.Shutdown();
            }

            Log.InfoFormat("Finished processing {0} events!", typeof(TModel).Name);
        }
    }
}