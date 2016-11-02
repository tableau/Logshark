using System.Collections.Generic;
using System.Text.RegularExpressions;
using LogParsers.Helpers;

namespace LogParsers
{
    /// <summary>
    /// Parses licensing logs to JSON.
    /// </summary>
    public sealed class LicensingParser : AbstractRegexParser, IParser
    {
        private static readonly string collectionName = ParserConstants.LicensingCollectionName;
        private static readonly IList<string> indexNames = new List<string> { "sev" };
        private static readonly CollectionSchema collectionSchema = ParserUtil.CreateCollectionSchema(collectionName, indexNames);

        private readonly IList<Regex> regexes = new List<Regex>
            {
                new Regex(@"^
                            \[(?<tid>.*?)\]\s
                            (?<sev>.*?)\s+
                            (?<ts>\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2})\s
                            (?<ts_offset>.*?)\s+:
                            (?<message>.*)",
                    RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled),
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

        public LicensingParser() { }
        public LicensingParser(LogFileContext fileContext) : base(fileContext) { }
    }
}
