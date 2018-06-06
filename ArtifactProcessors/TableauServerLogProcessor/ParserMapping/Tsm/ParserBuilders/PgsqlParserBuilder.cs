using LogParsers.Base.ParserBuilders;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using System;
using System.Collections.Generic;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.ParserMapping.Tsm.ParserBuilders
{
    /// <summary>
    /// Contains the mapping context between files within the "pgsql" directory in the logs and their associated parsers.
    /// </summary>
    internal sealed class PgsqlParserBuilder : BaseParserBuilder, IParserBuilder
    {
        private static readonly IDictionary<string, Type> fileMap =
            new Dictionary<string, Type>
            {
                { @"^control-pgsql-.*log.*", typeof(ServiceControlParser) },
                { @"^postgresql-.*csv", typeof(PostgresParser) },
                { @"^postgresql-.*log", typeof(PostgresLegacyParser) }
            };

        protected override IDictionary<string, Type> FileMap
        {
            get { return fileMap; }
        }
    }
}