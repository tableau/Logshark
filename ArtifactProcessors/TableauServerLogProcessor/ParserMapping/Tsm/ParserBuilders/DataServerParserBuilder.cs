using LogParsers.Base.ParserBuilders;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using System;
using System.Collections.Generic;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.ParserMapping.Tsm.ParserBuilders
{
    /// <summary>
    /// Contains the mapping context between files within the "dataserver" directory in the logs and their associated parsers.
    /// </summary>
    internal sealed class DataServerParserBuilder : BaseParserBuilder, IParserBuilder
    {
        private static readonly IDictionary<string, Type> fileMap =
            new Dictionary<string, Type>
            {
                { @"^control-dataserver-.*log.*", typeof(ServiceControlParser) },
                { @"^dataserver-.*log.*", typeof(DataServerJavaParser) },
                { @"^nativeapi_dataserver_.*txt.*", typeof(DataServerCppParser) },
                { @"^tabprotosrv_dataserver_.*txt.*", typeof(ProtocolServerParser) }
            };

        protected override IDictionary<string, Type> FileMap
        {
            get { return fileMap; }
        }
    }
}