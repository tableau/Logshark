using LogParsers.Base.ParserBuilders;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using System;
using System.Collections.Generic;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.ParserMapping.Tsm.ParserBuilders
{
    /// <summary>
    /// Contains the mapping context between files within the root directory in the logs and their associated parsers.
    /// </summary>
    internal sealed class RootParserBuilder : BaseParserBuilder, IParserBuilder
    {
        private static readonly IDictionary<string, Type> FileMapStatic =
            new Dictionary<string, Type>
            {
                { @"^manifest.json", typeof(ServiceManifestParser) }
            };

        protected override IDictionary<string, Type> FileMap => FileMapStatic;
    }
}