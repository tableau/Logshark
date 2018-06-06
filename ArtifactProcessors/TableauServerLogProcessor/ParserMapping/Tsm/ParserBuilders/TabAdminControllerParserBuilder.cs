using LogParsers.Base.ParserBuilders;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using System;
using System.Collections.Generic;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.ParserMapping.Tsm.ParserBuilders
{
    /// <summary>
    /// Contains the mapping context between files within the "tabadmincontroller" directory in the logs and their associated parsers.
    /// </summary>
    internal sealed class TabAdminControllerParserBuilder : BaseParserBuilder, IParserBuilder
    {
        private static readonly IDictionary<string, Type> fileMap =
            new Dictionary<string, Type>
            {
                { @"^control-tabadmincontroller-.*log.*", typeof(ServiceControlParser) },
                { @"^nativeapi_tabadmincontroller_.*txt.*", typeof(TabAdminControllerCppParser) },
                { @"^tabadmincontroller.*log.*", typeof(TabAdminControllerJavaParser) }
            };

        protected override IDictionary<string, Type> FileMap
        {
            get { return fileMap; }
        }
    }
}