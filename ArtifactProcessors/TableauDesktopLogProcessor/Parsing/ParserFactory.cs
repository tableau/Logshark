using LogParsers.Base;
using LogParsers.Base.ParserBuilders;
using System;
using System.Collections.Generic;
using Tableau.DesktopLogProcessor.Parsing.ParserBuilders;

namespace Tableau.DesktopLogProcessor.Parsing
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