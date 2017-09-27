using LogParsers.Base.ParserBuilders;
using LogParsers.Base.Parsers.Shared;
using System;
using System.Collections.Generic;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsing.ParserBuilders
{
    /// <summary>
    /// Contains the mapping context between files within the "dataengine" directory in the logs and their associated parsers.
    /// </summary>
    internal sealed class DataEngineParserBuilder : BaseParserBuilder, IParserBuilder
    {
        private static readonly IDictionary<string, Type> fileMap =
            new Dictionary<string, Type>
            {
                { @"^tdeserver\d.*", typeof(DataEngineParser) }
            };

        protected override IDictionary<string, Type> FileMap
        {
            get { return fileMap; }
        }
    }
}