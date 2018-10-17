using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.PluginInterfaces;
using Logshark.PluginLib.Helpers;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginLib.Processors;
using Logshark.PluginModel.Model;
using Logshark.Plugins.Apache.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace Logshark.Plugins.Apache
{
    public sealed class Apache : BaseWorkbookCreationPlugin, IServerClassicPlugin, IServerTsmPlugin
    {
        private static readonly string IncludeGatewayHealthChecksPluginArgumentKey = "Apache.IncludeGatewayHealthChecks";

        private readonly bool includeGatewayHealthCheckRequests;

        public override ISet<string> CollectionDependencies
        {
            get
            {
                return new HashSet<string>
                {
                    ParserConstants.HttpdCollectionName
                };
            }
        }

        public override ICollection<string> WorkbookNames
        {
            get
            {
                return new List<string>
                {
                    "Apache.twbx"
                };
            }
        }

        public Apache() { }

        public Apache(IPluginRequest request) : base(request)
        {
            includeGatewayHealthCheckRequests = ParseIncludeGatewayHealthCheck(request);
        }

        public override IPluginResponse Execute()
        {
            var pluginResponse = CreatePluginResponse();

            IMongoCollection<HttpdRequest> collection = MongoDatabase.GetCollection<HttpdRequest>(ParserConstants.HttpdCollectionName);

            using (var persister = ExtractFactory.CreateExtract<HttpdRequest>("ApacheRequests.hyper"))
            using (var processor = new SimpleModelProcessor<HttpdRequest, HttpdRequest>(persister, Log))
            {
                var apacheRequestFilter = BuildApacheRequestFilter(includeGatewayHealthCheckRequests);

                processor.Process(collection, new QueryDefinition<HttpdRequest>(apacheRequestFilter), item => item, apacheRequestFilter);

                if (persister.ItemsPersisted <= 0)
                {
                    Log.Warn("Failed to persist any data from Apache logs!");
                    pluginResponse.GeneratedNoData = true;
                }

                return pluginResponse;
            }
        }

        private bool ParseIncludeGatewayHealthCheck(IPluginRequest request)
        {
            bool includeGatewayHealthChecksValue = false;

            if (request.ContainsRequestArgument(IncludeGatewayHealthChecksPluginArgumentKey))
            {
                try
                {
                    includeGatewayHealthChecksValue = PluginArgumentHelper.GetAsBoolean(IncludeGatewayHealthChecksPluginArgumentKey, request);
                }
                catch (FormatException)
                {
                    Log.WarnFormat("Invalid value was specified for plugin argument key '{0}': valid values are either 'true' or 'false'.  Proceeding with default value of '{1}'..", IncludeGatewayHealthChecksPluginArgumentKey, includeGatewayHealthChecksValue);
                }
            }

            // Log results.
            if (includeGatewayHealthChecksValue)
            {
                Log.Info("Including gateway health check requests due to user request.");
            }
            else
            {
                Log.InfoFormat("Excluding gateway health check requests from plugin output.  Use the plugin argument '{0}:true' if you wish to include them.", IncludeGatewayHealthChecksPluginArgumentKey);
            }

            return includeGatewayHealthChecksValue;
        }

        private static FilterDefinition<HttpdRequest> BuildApacheRequestFilter(bool includeGatewayHealthCheckRequests = false)
        {
            // Filter down to only access files.
            var query = Builders<HttpdRequest>.Filter.Regex("file", new BsonRegularExpression("^access.*"));

            // Gateway health check requests are generally noise, but may be desired in some situations.
            if (!includeGatewayHealthCheckRequests)
            {
                query = query & Builders<HttpdRequest>.Filter.Ne("resource", "/favicon.ico");
            }

            return query;
        }
    }
}