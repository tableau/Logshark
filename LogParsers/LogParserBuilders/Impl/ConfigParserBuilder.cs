using System;
using System.Collections.Generic;

namespace LogParsers
{
    /// <summary>
    /// Contains the mapping context between files within the "config" directory in the logs and their associated parsers.
    /// </summary>
    internal sealed class ConfigParserBuilder : BaseParserBuilder, IParserBuilder
    {
        private static readonly IDictionary<string, Type> fileMap =
            new Dictionary<string, Type>
            {
                { @"^connections.*properties", typeof(ConnectionsConfigParser) },
                { @"^connections.*yml", typeof(ConfigYamlParser) },
                { @"^customization.*yml", typeof(ConfigYamlParser) },
                { @"^pg_hba.conf", typeof(PostgresHostConfigParser) },
                { @"^tasks.*yml", typeof(TasksYamlParser) },
                { @"^workgroup.*yml", typeof(ConfigYamlParser) }
            };

        protected override IDictionary<string, Type> FileMap
        {
            get { return fileMap; }
        }
    }
}