using log4net;
using Logshark.Core.Controller.Parsing;
using Logshark.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Logshark.Core.Controller.Processing
{
    internal class HashTargetProcessingStrategy : ILogsetProcessingStrategy
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public LogsetParsingResult ProcessLogset(LogsetParsingRequest request, LogsetProcessingStatus existingProcessedLogsetStatus)
        {
            switch (existingProcessedLogsetStatus.State)
            {
                case ProcessedLogsetState.NonExistent:
                    throw new InvalidTargetHashException(String.Format("No logset exists that matches logset hash '{0}'. Aborting..", request.LogsetHash));

                case ProcessedLogsetState.Corrupt:
                    throw new InvalidTargetHashException(String.Format("Mongo database matching logset hash '{0}' exists but is corrupted. Aborting..", request.LogsetHash));

                case ProcessedLogsetState.InFlight:
                    throw new ProcessingUserCollisionException(String.Format("Logset matching hash '{0}' exists but is currently being processed by another user.  Aborting..", request.LogsetHash));

                case ProcessedLogsetState.Incomplete:
                    throw new InvalidTargetHashException("Found existing logset matching hash, but it is a partial logset that does not contain all of the data required to run specified plugins. Aborting..");

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