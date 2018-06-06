using LogParsers.Base;
using LogParsers.Base.ParserBuilders;
using Logshark.ArtifactProcessors.TableauDesktopLogProcessor.ParserMapping.ParserBuilders;
using System;
using System.Collections.Generic;

namespace Logshark.ArtifactProcessors.TableauDesktopLogProcessor.ParserMapping
{
    internal class ParserFactory : BaseParserFactory
    {
        private static readonly IDictionary<string, Type> DesktopDirectoryMap = new Dictionary<string, Type>
        {
            { @"logs", typeof(DesktopParserBuilder) }
        };

        public ParserFactory(string rootLogLocation) : base(rootLogLocation)
        {
        }

        protected override IDictionary<string, Type> DirectoryMap { get { return DesktopDirectoryMap; } }

        protected override IParserBuilder GetRootParserBuilder()
        {
            return new DesktopParserBuilder();
        }
    }
}