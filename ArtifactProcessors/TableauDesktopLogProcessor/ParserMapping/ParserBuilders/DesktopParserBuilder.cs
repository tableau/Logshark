using LogParsers.Base.ParserBuilders;
using Logshark.ArtifactProcessors.TableauDesktopLogProcessor.Parsers;
using System;
using System.Collections.Generic;

namespace Logshark.ArtifactProcessors.TableauDesktopLogProcessor.ParserMapping.ParserBuilders
{
    public class DesktopParserBuilder : BaseParserBuilder, IParserBuilder
    {
        private static readonly IDictionary<string, Type> fileMap =
            new Dictionary<string, Type>
            {
                { @"^log.*txt", typeof(DesktopCppParser) },
                { @"^tabprotosrv.*txt", typeof(ProtocolServerParser) },
                { @"^tdeserver.*", typeof(DataEngineParser)}
            };

        protected override IDictionary<string, Type> FileMap
        {
            get { return fileMap; }
        }
    }
}