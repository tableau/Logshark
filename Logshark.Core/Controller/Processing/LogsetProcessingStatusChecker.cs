using log4net;
using Logshark.Common.Extensions;
using Logshark.ConnectionModel.Helpers;
using Logshark.ConnectionModel.Mongo;
using Logshark.Core.Controller.Parsing.Mongo.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Logshark.Core.Controller.Processing
{
    internal class LogsetProcessingStatusChecker
    {
        // The time span after which a processing heartbeat is no longer considered valid, in seconds.
        protected const int MongoProcessingHeartbeatExpirationTime = 60;

        protected readonly MongoConnectionInfo mongoConnectionInfo;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public LogsetProcessingStatusChecker(MongoConnectionInfo mongoConnectionInfo)
        {
            this.mongoConnectionInfo = mongoConnectionInfo;
        }

        public LogsetProcessingStatus GetStatus(string logsetHash, IEnumerable<string> requiredCollections)
        {
            // Retrieve logset metadata for the given logset hash from MongoDB
            LogProcessingMetadata logsetMetadata;
            try
            {
                if (!RemoteLogsetHasData(logsetHash))
                {
                    return new LogsetProcessingStatus(ProcessedLogsetState.NonExistent);
                }

                var metadataReader = new MongoLogProcessingMetadataWriter(mongoConnectionInfo);
                logsetMetadata = metadataReader.Read(logsetHash);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Unable to retrieve logset metadata from MongoDB: {0}", ex.Message);
                return new LogsetProcessingStatus(ProcessedLogsetState.Indeterminable);
            }

            // Lack of metadata is treated as a corrupt state.
            if (logsetMetadata == null || logsetMetadata.CollectionsParsed == null || String.IsNullOrWhiteSpace(logsetMetadata.LogsetType))
            {
                return new LogsetProcessingStatus(ProcessedLogsetState.Corrupt);
            }

            // Check to see if the run completed successfully.
            if (!logsetMetadata.ProcessedSuccessfully)
            {
                if (IsHeartbeatExpired(logsetMetadata))
                {
                    return new LogsetProcessingStatus(ProcessedLogsetState.Corrupt);
                }

                return new LogsetProcessingStatus(ProcessedLogsetState.InFlight, logsetMetadata.ProcessedSize);
            }

            // Check to see if the remote logset has all of the collections we need.
            var missingCollections = requiredCollections.Except(logsetMetadata.CollectionsParsed).ToHashSet();
            if (missingCollections.Any())
            {
                Log.DebugFormat("Remote {0} logset does not contain required collections: {1}", logsetMetadata.LogsetType, String.Join(", ", missingCollections));
                return new LogsetProcessingStatus(ProcessedLogsetState.Incomplete, logsetMetadata.ProcessedSize);
            }

            return new LogsetProcessingStatus(ProcessedLogsetState.Valid, logsetMetadata.ProcessedSize);
        }

        protected bool RemoteLogsetHasData(string mongoDatabaseName)
        {
            return MongoAdminHelper.DatabaseExists(mongoConnectionInfo.GetClient(), mongoDatabaseName);
        }

        protected bool IsHeartbeatExpired(LogProcessingMetadata metadata)
        {
            TimeSpan timeSinceLastHeartbeat = DateTime.UtcNow - metadata.ProcessingHeartbeat;
            return timeSinceLastHeartbeat.TotalSeconds >= MongoProcessingHeartbeatExpirationTime;
        }
    }
}