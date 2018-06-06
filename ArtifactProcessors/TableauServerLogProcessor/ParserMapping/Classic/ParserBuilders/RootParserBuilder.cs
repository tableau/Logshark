using LogParsers.Base.ParserBuilders;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using System;
using System.Collections.Generic;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.ParserMapping.Classic.ParserBuilders
{
    /// <summary>
    /// Contains the mapping context between files within the root directory in the logs and their associated parsers.
    /// </summary>
    internal sealed class RootParserBuilder : BaseParserBuilder, IParserBuilder
    {
        private static readonly IDictionary<string, Type> fileMap =
            new Dictionary<string, Type>
            {
                { @"^buildversion.txt", typeof(BuildVersionParser) },
                { @"^netstat-info.txt", typeof(NetstatWindowsParser) },
                { @"^tabsvc.*yml", typeof(ConfigYamlParser) }
            };

        protected override IDictionary<string, Type> FileMap
        {
            get { return fileMap; }
        }
    }
}