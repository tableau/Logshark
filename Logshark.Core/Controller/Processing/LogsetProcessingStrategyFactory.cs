using Logshark.Core.Controller.Parsing;
using Logshark.RequestModel;
using Logshark.RequestModel.Config;
using System;

namespace Logshark.Core.Controller.Processing
{
    internal class LogsetProcessingStrategyFactory
    {
        public static ILogsetProcessingStrategy GetLogsetProcessingStrategy(LogsharkRequestTarget target, Func<LogsetParsingRequest, LogsetParsingResult> parseLogset, Action<string> dropExistingLogset, LogsharkConfiguration config)
        {
            switch (target.Type)
            {
                case LogsetTarget.File:
                case LogsetTarget.Directory:
                    return new ArchiveTargetProcessingStrategy(parseLogset, dropExistingLogset, config);

                case LogsetTarget.Hash:
                    return new HashTargetProcessingStrategy();

                default:
                    throw new ArgumentException(String.Format("Cannot get run processing strategy for unknown target type '{0}'", target.Type), "target");
            }
        }
    }
}