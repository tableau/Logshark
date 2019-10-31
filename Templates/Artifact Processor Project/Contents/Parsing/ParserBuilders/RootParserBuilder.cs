using LogParsers.Base.ParserBuilders;
using Logshark.ArtifactProcessors.$safeprojectname$.Parsing.Parsers;
using System;
using System.Collections.Generic;

namespace Logshark.ArtifactProcessors.$safeprojectname$.Parsing.ParserBuilders
{
    public class RootParserBuilder : BaseParserBuilder, IParserBuilder
    {
        private static readonly IDictionary<string, Type> fileMap = new Dictionary<string, Type>
        {
            // TODO: Map file patterns in the root to their parsers here.
            { @"^.*\.json", typeof(SampleJsonParser) }
        };

        protected override IDictionary<string, Type> FileMap
        {
            get { return fileMap; }
        }
    }
}
