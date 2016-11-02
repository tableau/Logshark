using System;
using System.Collections.Generic;

namespace LogParsers
{
    /// <summary>
    /// Contains the mapping context between files within the "desktop" directory in the logs and their associated parsers.
    /// </summary>
    internal sealed class DesktopParserBuilder : BaseParserBuilder, IParserBuilder
    {
        private static readonly IDictionary<string, Type> fileMap =
            new Dictionary<string, Type>
            {
                { @"^log.*txt", typeof(DesktopCppParser) },
                { @"^tabprotosrv.*txt", typeof(ProtocolServerParser) },
                { @"^tdeserver.*", typeof(DataEngineParser)}
            };

        protected override IDictionary<string, Type> FileMap
        {
            get { return fileMap; }
        }
    }
}