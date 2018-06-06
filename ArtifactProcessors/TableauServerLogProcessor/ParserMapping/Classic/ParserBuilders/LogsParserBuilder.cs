using LogParsers.Base.ParserBuilders;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using System;
using System.Collections.Generic;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.ParserMapping.Classic.ParserBuilders
{
    /// <summary>
    /// Contains the mapping context between files within the "logs" directory in the logs and their associated parsers.
    /// </summary>
    internal sealed class LogsParserBuilder : BaseParserBuilder, IParserBuilder
    {
        private static readonly IDictionary<string, Type> fileMap =
            new Dictionary<string, Type>
            {
                { @"^tabadmin.log", typeof(TabAdminParser) },
                { @"^tablicsrv.log", typeof(TabLicSrvParser) },
                { @"^tabsrvlic.log", typeof(TabLicSrvParser) }
            };

        protected override IDictionary<string, Type> FileMap
        {
            get { return fileMap; }
        }
    }
}