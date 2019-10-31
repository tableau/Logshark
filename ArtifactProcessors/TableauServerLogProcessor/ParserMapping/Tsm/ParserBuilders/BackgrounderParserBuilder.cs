using LogParsers.Base.ParserBuilders;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using System;
using System.Collections.Generic;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.ParserMapping.Tsm.ParserBuilders
{
    /// <summary>
    /// Contains the mapping context between files within the "backgrounder" directory in the logs and their associated parsers.
    /// </summary>
    internal sealed class BackgrounderParserBuilder : BaseParserBuilder, IParserBuilder
    {
        private static readonly IDictionary<string, Type> fileMap =
            new Dictionary<string, Type>
            {
                { @"^backgrounder.*log.*", typeof(BackgrounderJavaParser) },
                { @"^control.backgrounder.*log.*", typeof(ServiceControlParser) },
                { @"^nativeapi_backgrounder_.*txt.*", typeof(BackgrounderCppParser) },
                { @"^tabprotosrv_backgrounder_.*txt.*", typeof(ProtocolServerParser) }
            };

        protected override IDictionary<string, Type> FileMap
        {
            get { return fileMap; }
        }
    }
}