using System;
using System.Collections.Generic;
using LogParsers.Base.ParserBuilders;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.ParserMapping.Tsm.ParserBuilders
{
    public sealed class NetstatLinuxParserBuilder : BaseParserBuilder
    {
        private static readonly IDictionary<string, Type> FileMapStatic = new Dictionary<string, Type>
        {
            { @"^netstat-anp.txt", typeof(NetstatLinuxParser) }
        };

        protected override IDictionary<string, Type> FileMap => FileMapStatic;
    }
}