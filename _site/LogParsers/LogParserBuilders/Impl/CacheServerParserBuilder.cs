﻿using System;
using System.Collections.Generic;

namespace LogParsers
{
    /// <summary>
    /// Contains the mapping context between files within the "cacheserver" directory in the logs and their associated parsers.
    /// </summary>
    internal sealed class CacheServerParserBuilder : BaseParserBuilder, IParserBuilder
    {
        private static readonly IDictionary<string, Type> fileMap =
            new Dictionary<string, Type>
            {
                { @"^redis_.*log", typeof(CacheServerParser) }
            };

        protected override IDictionary<string, Type> FileMap
        {
            get { return fileMap; }
        }
    }
}