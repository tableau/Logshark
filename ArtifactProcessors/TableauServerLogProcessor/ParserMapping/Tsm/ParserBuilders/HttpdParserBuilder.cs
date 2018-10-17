using LogParsers.Base.ParserBuilders;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using System;
using System.Collections.Generic;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.ParserMapping.Tsm.ParserBuilders
{
    /// <summary>
    /// Contains the mapping context between files within the "httpd" directory in the logs and their associated parsers.
    /// </summary>
    internal sealed class HttpdParserBuilder : BaseParserBuilder, IParserBuilder
    {
        private static readonly IDictionary<string, Type> fileMap =
            new Dictionary<string, Type>
            {
                { @"^access.*log", typeof(HttpdParser) },
                { @"^control.gateway.*log.*", typeof(ServiceControlParser) },
                { @"^gateway.*log.*", typeof(HttpdConfigurationParser) },
                { @"^error.*log", typeof(HttpdErrorParser) }
            };

        protected override IDictionary<string, Type> FileMap
        {
            get { return fileMap; }
        }
    }
}