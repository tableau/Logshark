using log4net;
using Logshark.ConnectionModel.Helpers;
using Logshark.Core.Controller.Metadata.Logset.Mongo;
using Logshark.Core.Helpers;
using Logshark.RequestModel;
using System;
using System.Reflection;

namespace Logshark.Core.Controller.Parsing
{
    public enum LogsetStatus
    {
        NonExistent,
        InFlight,
        Corrupt,
        Incomplete,
        Indeterminable,
        Valid
    }

    internal static class LogsetProcessingStatusChecker
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static LogsetStatus GetStatus(LogsharkRequest request)
        {
            LogsetMetadata logsetMetadata;
            string logsetType;
            try
            {
                LogsetMetadataReader metadataReader = new LogsetMetadataReader(request);

                if (!RemoteLogsetHasData(request))
                {
                    return LogsetStatus.NonExistent;
                }

                logsetMetadata = metadataReader.GetMetadata();
                logsetType = metadataReader.GetLogsetType();
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Unable to retrieve logset metadata from MongoDB: {0}", ex.Message);
                return LogsetStatus.Indeterminable;
            }

            // Lack of metadata is treated as a corrupt state.
            if (logsetMetadata == null || logsetMetadata.CollectionsParsed == null || logsetType.Equals(RequestConstants.UNKNOWN_LOGSET_TYPE))
            {
                return LogsetStatus.Corrupt;
            }

            if (!logsetMetadata.ProcessedSuccessfully)
            {
                if (logsetMetadata.IsHeartbeatExpired())
                {
                    return LogsetStatus.Corrupt;
                }
                else
                {
                    return LogsetStatus.InFlight;
                }
            }

            // Check to make sure the remote logset has all of the collections we need.
            var missingCollections = LogsetDependencyHelper.GetMissingRequiredCollections(request, logsetMetadata.CollectionsParsed);
            if (missingCollections.Count > 0)
            {
                Log.DebugFormat("Remote {0} logset does not contain required collections: {1}", logsetType, String.Join(", ", missingCollections));
                return LogsetStatus.Incomplete;
            }

            return LogsetStatus.Valid;
        }

        private static bool RemoteLogsetHasData(LogsharkRequest request)
        {
            return MongoAdminUtil.DatabaseExists(request.Configuration.MongoConnectionInfo.GetClient(), request.RunContext.MongoDatabaseName);
        }
    }
}