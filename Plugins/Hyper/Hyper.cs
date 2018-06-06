using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.PluginInterfaces;
using Logshark.PluginLib.Extensions;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginLib.Persistence;
using Logshark.PluginModel.Model;
using Logshark.Plugins.Hyper.Helpers;
using Logshark.Plugins.Hyper.Models;
using MongoDB.Driver;
using ServiceStack.Common.Extensions;
using System;
using System.Collections.Generic;

namespace Logshark.Plugins.Hyper
{
    /// <summary>
    /// Hyper Workbook Creation Plugin
    /// </summary>
    public sealed class Hyper : BaseWorkbookCreationPlugin, IServerClassicPlugin, IServerTsmPlugin
    {
        private IPluginResponse pluginResponse;

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
                    "Hyper.twb"
                };
            }
        }

        public override IPluginResponse Execute(IPluginRequest pluginRequest)
        {
            pluginResponse = CreatePluginResponse();

            ProcessEvents<HyperError>(pluginRequest, MongoQueryHelper.GetErrors);
            ProcessEvents<HyperQuery>(pluginRequest, MongoQueryHelper.GetQueryEndEvents);

            if (!PersistedData())
            {
                Log.Info("Failed to persist any data from Hyper logs!");
                pluginResponse.GeneratedNoData = true;
            }

            return pluginResponse;
        }

        private void ProcessEvents<T>(IPluginRequest pluginRequest, Func<IMongoCollection<T>, IAsyncCursor<T>> query) where T : BaseHyperEvent, new()
        {
            Log.InfoFormat("Processing {0} events..", typeof(T).Name);

            var collection = MongoDatabase.GetCollection<T>(ParserConstants.HyperCollectionName);
            GetOutputDatabaseConnection().CreateOrMigrateTable<T>();

            IPersister<T> persister = GetConcurrentBatchPersister<T>(pluginRequest);
            using (GetPersisterStatusWriter(persister))
            {
                IAsyncCursor<T> cursor = query(collection);
                while (cursor.MoveNext())
                {
                    cursor.Current.ForEach(document =>
                    {
                        document.LogsetHash = pluginRequest.LogsetHash;
                        persister.Enqueue(document);
                    });
                }

                persister.Shutdown();
            }

            Log.InfoFormat("Finished processing {0} events!", typeof(T).Name);
        }
    }
}