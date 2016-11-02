using System.Collections.Generic;
using System.Text.RegularExpressions;
using LogParsers.Helpers;

namespace LogParsers
{
    /// <summary>
    /// Parses CacheServer logs to JSON.
    /// </summary>
    public sealed class CacheServerParser : AbstractRegexParser, IParser
    {
        private static readonly string collectionName = ParserConstants.CacheServerCollectionName;
        private static readonly IList<string> indexNames = new List<string>();
        private static readonly CollectionSchema collectionSchema = ParserUtil.CreateCollectionSchema(collectionName, indexNames);

        private readonly IList<Regex> regexes = new List<Regex>
            {
                new Regex(@"^
                            \[(?<pid>.+?)\]\s
                            (?<ts>\d{2}\s[A-Z][a-z]{2}\s.+?)\s
                            .\s
                            (?<message>.*)",
                    RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled)
            };

        protected override IList<Regex> Regexes
        {
            get { return regexes; }
        }

        public override CollectionSchema CollectionSchema
        {
            get
            {
                return collectionSchema;
            }
        }

        public CacheServerParser() { }
        public CacheServerParser(LogFileContext fileContext) : base(fileContext) { }
    }
}
