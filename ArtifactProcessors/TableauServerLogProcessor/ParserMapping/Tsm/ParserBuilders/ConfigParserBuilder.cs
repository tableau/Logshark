using LogParsers.Base.ParserBuilders;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using System;
using System.Collections.Generic;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.ParserMapping.Tsm.ParserBuilders
{
    /// <summary>
    /// Contains the mapping context between files within the "config" directory in the logs and their associated parsers.
    /// </summary>
    internal sealed class ConfigParserBuilder : BaseParserBuilder, IParserBuilder
    {
        private static readonly IDictionary<string, Type> fileMap =
            new Dictionary<string, Type>
            {
                { @"^pg_hba.conf", typeof(PostgresHostConfigParser) },
                { @"^ports.*yml", typeof(ConfigYamlParser) },
                { @"^tabsvc.*yml", typeof(ConfigYamlParser) },
                { @"^topology.*yml", typeof(ConfigYamlParser) },
                { @"^workgroup.*yml", typeof(ConfigYamlParser) }
            };

        protected override IDictionary<string, Type> FileMap
        {
            get { return fileMap; }
        }
    }
}