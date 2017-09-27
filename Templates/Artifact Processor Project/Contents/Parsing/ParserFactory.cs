using LogParsers.Base;
using LogParsers.Base.ParserBuilders;
using Logshark.ArtifactProcessors.$safeprojectname$.Parsing.ParserBuilders;
using System;
using System.Collections.Generic;

namespace Logshark.ArtifactProcessors.$safeprojectname$.Parsing
{
    internal class ParserFactory : BaseParserFactory, IParserFactory
    {
        private static readonly IDictionary<string, Type> directoryMap = new Dictionary<string, Type>
        {
            // TODO: Map subdirectories within the artifact payload to ParserBuilders here.
            // For example: { @"logs", typeof(LogDirectoryParserBuilder) }
        };

        public ParserFactory(string rootLogLocation) : base(rootLogLocation) { }

        protected override IDictionary<string, Type> DirectoryMap { get { return directoryMap; } }

        protected override IParserBuilder GetRootParserBuilder()
        {
            return new RootParserBuilder();
        }
    }
}
