using Logshark.ArtifactProcessors.TableauDesktopLogProcessor.Parsers;
using Logshark.ArtifactProcessors.TableauDesktopLogProcessor.PluginInterfaces;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginModel.Model;
using Logshark.Plugins.Vizql.Helpers;
using Logshark.Plugins.Vizql.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace Logshark.Plugins.Vizql
{
    public class VizqlDesktop : BaseWorkbookCreationPlugin, IDesktopPlugin
    {
        public override ISet<string> CollectionDependencies
        {
            get
            {
                return new HashSet<string>
                {
                    ParserConstants.DesktopCollectionName
                };
            }
        }

        public override ICollection<string> WorkbookNames
        {
            get
            {
                return new List<string>
                {
                    "VizqlDesktop.twbx"
                };
            }
        }

        public VizqlDesktop()
        {
        }

        public VizqlDesktop(IPluginRequest request) : base(request)
        {
        }

        public override IPluginResponse Execute()
        {
            var pluginResponse = CreatePluginResponse();

            IMongoCollection<BsonDocument> collection = MongoDatabase.GetCollection<BsonDocument>(ParserConstants.DesktopCollectionName);

            using (var persister = new DesktopSessionPersister(pluginRequest, ExtractFactory))
            using (GetPersisterStatusWriter(persister))
            {
                ProcessSessions(collection, persister);

                if (persister.ItemsPersisted <= 0)
                {
                    Log.Info("Failed to persist any data from Vizql desktop logs!");
                    pluginResponse.GeneratedNoData = true;
                }

                return pluginResponse;
            }
        }

        private void ProcessSessions(IMongoCollection<BsonDocument> collection, IPersister<VizqlDesktopSession> persister)
        {
            foreach (var session in Queries.GetAllDesktopSessions(collection))
            {
                try
                {
                    var processedSession = Queries.AppendAllSessionEvents(session, collection);
                    persister.Enqueue(processedSession as VizqlDesktopSession);
                }
                catch (Exception ex)
                {
                    Log.ErrorFormat("Failed to process session {0} in {1}: {2}", session.VizqlSessionId, collection.CollectionNamespace.CollectionName, ex.Message);
                }
            }
        }
    }
}