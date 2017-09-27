using LogParsers.Base.ParserBuilders;
using LogParsers.Base.Parsers.Shared;
using System;
using System.Collections.Generic;
using Tableau.DesktopLogProcessor.Parsing.Parsers;

namespace Tableau.DesktopLogProcessor.Parsing.ParserBuilders
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