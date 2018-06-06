using log4net;
using Logshark.Core.Controller.Parsing;
using Logshark.Core.Exceptions;
using Logshark.RequestModel.Config;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Logshark.Core.Controller.Processing
{
    internal class ArchiveTargetProcessingStrategy : ILogsetProcessingStrategy
    {
        protected readonly Func<LogsetParsingRequest, LogsetParsingResult> parseLogset;
        protected readonly Action<string> dropExistingLogset;
        protected readonly LogsharkConfiguration config;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public ArchiveTargetProcessingStrategy(Func<LogsetParsingRequest, LogsetParsingResult> parseLogset, Action<string> dropExistingLogset, LogsharkConfiguration config)
        {
            this.parseLogset = parseLogset;
            this.dropExistingLogset = dropExistingLogset;
            this.config = config;
        }

        public LogsetParsingResult ProcessLogset(LogsetParsingRequest request, LogsetProcessingStatus existingProcessedLogsetStatus)
        {
            // If the user requested a forced reparsing of this logset, first drop the existing logset.
            if (request.ForceParse && existingProcessedLogsetStatus.State != ProcessedLogsetState.NonExistent)
            {
                Log.InfoFormat("'Force Parse' request issued, dropping existing logset '{0}'..", request.LogsetHash);
                dropExistingLogset(request.LogsetHash);
                return parseLogset(request);
            }

            switch (existingProcessedLogsetStatus.State)
            {
                case ProcessedLogsetState.NonExistent:
                    return parseLogset(request);

                case ProcessedLogsetState.Corrupt:
                    Log.InfoFormat("Logset matching hash '{0}' exists but is corrupted. Dropping it and reprocessing..", request.LogsetHash);
                    dropExistingLogset(request.LogsetHash);
                    return parseLogset(request);

                case ProcessedLogsetState.InFlight:
                    throw new ProcessingUserCollisionException(String.Format("Logset matching hash '{0}' exists but is currently being processed by another user.  Aborting..", request.LogsetHash));

                case ProcessedLogsetState.Incomplete:
                    dropExistingLogset(request.LogsetHash);
                    Log.Info("Found existing logset matching hash, but it is a partial logset that does not contain all of the data required to run specified plugins. Dropping it and reprocessing..");
                    return parseLogset(request);

                case ProcessedLogsetState.Indeterminable:
                    throw new IndeterminableLogsetStatusException("Unable to determine status of logset. Aborting..");

                case ProcessedLogsetState.Valid:
                    Log.Info("Found existing logset matching hash! Skipping extraction and parsing.");
                    return new LogsetParsingResult(new List<string>(), existingProcessedLogsetStatus.ProcessedDataVolumeBytes, utilizedExistingProcessedLogset: true);

                default:
                    throw new ArgumentOutOfRangeException(String.Format("'{0}' is not a valid LogsetProcessingState!", existingProcessedLogsetStatus.State));
            }
        }
    }
}