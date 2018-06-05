using Logshark.Core.Controller.Parsing;

namespace Logshark.Core.Controller.Processing
{
    internal interface ILogsetProcessingStrategy
    {
        LogsetParsingResult ProcessLogset(LogsetParsingRequest request, LogsetProcessingStatus existingProcessedLogsetStatus);
    }
}