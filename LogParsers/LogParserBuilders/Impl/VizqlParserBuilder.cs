using System;
using System.Collections.Generic;

namespace LogParsers
{
    /// <summary>
    /// Contains the mapping context between files within the "vizqlserver" directory in the logs and their associated parsers.
    /// </summary>
    internal sealed class VizqlParserBuilder : BaseParserBuilder, IParserBuilder
    {
        private static readonly IDictionary<string, Type> fileMap =
            new Dictionary<string, Type>
            {
                { @"^vizql-.*log", typeof(VizqlServerJavaParser) },
                { @"^backgrounder.*txt", typeof(BackgrounderCppParser) },
                { @"^dataserver.*txt", typeof(DataServerCppParser) },
                { @"^tabprotosrv.*txt", typeof(ProtocolServerParser) },
                { @"^vizportal.*txt", typeof(VizportalCppParser) },
                { @"^vizqlserver.*txt", typeof(VizqlServerCppParser) },
                { @"^wgserver.*txt", typeof(WgServerCppParser) },
                { @"^tdeserver.*", typeof(DataEngineParser)}
            };

        protected override IDictionary<string, Type> FileMap
        {
            get { return fileMap; }
        }
    }
}