using LogParsers.Base.ParserBuilders;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using System;
using System.Collections.Generic;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.ParserMapping.Tsm.ParserBuilders
{
    /// <summary>
    /// Contains the mapping context between files within the "databasemaintenance" directory in the logs and their associated parsers.
    /// </summary>
    internal sealed class DatabaseMaintenanceParserBuilder : BaseParserBuilder, IParserBuilder
    {
        private static readonly IDictionary<string, Type> fileMap =
            new Dictionary<string, Type>
            {
                { @"^control-databasemaintenance-.*log.*", typeof(ServiceControlParser) },
                { @"^databasemaintenance-.*log.*", typeof(DatabaseMaintenanceParser) }
            };

        protected override IDictionary<string, Type> FileMap
        {
            get { return fileMap; }
        }
    }
}