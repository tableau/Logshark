using LogParsers.Base.ParserBuilders;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using System;
using System.Collections.Generic;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.ParserMapping.Tsm.ParserBuilders
{
    /// <summary>
    /// Contains the mapping context between files within the "vizportal" directory in the logs and their associated parsers.
    /// </summary>
    internal sealed class VizPortalParserBuilder : BaseParserBuilder, IParserBuilder
    {
        private static readonly IDictionary<string, Type> fileMap =
            new Dictionary<string, Type>
            {
                { @"^control.vizportal.*log.*", typeof(ServiceControlParser) },
                { @"^nativeapi_vizportal_.*txt.*", typeof(VizportalCppParser) },
                { @"^tabprotosrv_vizportal_.*txt.*", typeof(ProtocolServerParser) },
                { @"^vizportal.*log.*", typeof(VizportalJavaParser) }
            };

        protected override IDictionary<string, Type> FileMap
        {
            get { return fileMap; }
        }
    }
}