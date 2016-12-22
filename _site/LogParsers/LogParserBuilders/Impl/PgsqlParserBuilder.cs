using System;
using System.Collections.Generic;
using LogParsers.LogParsers.Impl;

namespace LogParsers
{
    /// <summary>
    /// Contains the mapping context between files within the "pgsql" directory in the logs and their associated parsers.
    /// </summary>
    internal sealed class PgsqlParserBuilder : BaseParserBuilder, IParserBuilder
    {
        private static readonly IDictionary<string, Type> fileMap =
            new Dictionary<string, Type>
            {
                { @"^postgresql-.*csv", typeof(PostgresParser) }, // 9.3+
                { @"^postgresql-.*log", typeof(PostgresLegacyParser) }  // Pre-9.3
            };

        protected override IDictionary<string, Type> FileMap
        {
            get { return fileMap; }
        }
    }
}