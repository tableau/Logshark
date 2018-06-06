using System.Collections.Generic;

namespace Logshark.Core.Controller.Parsing
{
    public class LogsetParsingResult
    {
        // The absolute paths of any parsed files that failed to yield any data.
        public ISet<string> FailedFileParses { get; protected set; }

        public long ParsedDataVolumeBytes { get; protected set; }

        // Indicates whether the Logshark run utilized an existing processed logset.
        public bool UtilizedExistingProcessedLogset { get; protected set; }

        public LogsetParsingResult(IEnumerable<string> failedFileParses, long? parsedDataVolumeBytes, bool utilizedExistingProcessedLogset = false)
        {
            FailedFileParses = new SortedSet<string>(failedFileParses);
            if (parsedDataVolumeBytes.HasValue)
            {
                ParsedDataVolumeBytes = parsedDataVolumeBytes.Value;
            }
            UtilizedExistingProcessedLogset = utilizedExistingProcessedLogset;
        }
    }
}