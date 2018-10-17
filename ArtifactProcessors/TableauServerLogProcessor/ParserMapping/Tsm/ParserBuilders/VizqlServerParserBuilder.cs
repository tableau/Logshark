using LogParsers.Base.ParserBuilders;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using System;
using System.Collections.Generic;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.ParserMapping.Tsm.ParserBuilders
{
    /// <summary>
    /// Contains the mapping context between files within the "vizqlserver" directory in the logs and their associated parsers.
    /// </summary>
    internal sealed class VizqlServerParserBuilder : BaseParserBuilder, IParserBuilder
    {
        private static readonly IDictionary<string, Type> fileMap =
            new Dictionary<string, Type>
            {
                { @"^control.vizqlserver.*log.*", typeof(ServiceControlParser) },
                { @"^nativeapi_vizqlserver_.*txt.*", typeof(VizqlServerCppParser) },
                { @"^tabprotosrv_vizqlserver_.*txt.*", typeof(ProtocolServerParser) },
                { @"^vizqlserver.*log.*", typeof(VizqlServerJavaParser) }
            };

        protected override IDictionary<string, Type> FileMap
        {
            get { return fileMap; }
        }
    }
}