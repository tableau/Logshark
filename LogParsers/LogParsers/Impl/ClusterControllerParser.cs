using System.Collections.Generic;
using System.Text.RegularExpressions;
using LogParsers.Helpers;

namespace LogParsers
{
    /// <summary>
    /// Parses ClusterController logs to JSON.
    /// </summary>
    public sealed class ClusterControllerParser : AbstractMultiLineRegexParser, IParser
    {
        private static readonly string collectionName = ParserConstants.ClusterControllerCollectionName;
        private static readonly IList<string> indexNames = new List<string> { "class", "sev" };
        private static readonly CollectionSchema collectionSchema = ParserUtil.CreateCollectionSchema(collectionName, indexNames);

        private readonly IList<Regex> regexes = new List<Regex>
            {
                new Regex(@"^
                            (?<ts>\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}.\d{3})\s
                            (?<ts_offset>.+?)\s
                            (?<thread>.*?)\s
                            (?<sev>[A-Z]+)(\s+)
                            :\s
                            (?<class>.*?)\s-\s
                            (?<message>(.|\n)*)",
                    RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled),
            };

        private readonly IList<Regex> lineDelimiterRegexes = new List<Regex>
            {
                new Regex(@"^\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}.\d{3}\s") // DateTime string
            };

        protected override IList<Regex> Regexes
        {
            get { return regexes; }
        }

        protected override IList<Regex> LineDelimiterRegexes
        {
            get { return lineDelimiterRegexes; }
        }

        public override CollectionSchema CollectionSchema
        {
            get { return collectionSchema; }
        }

        public ClusterControllerParser() { }
        public ClusterControllerParser(LogFileContext fileContext) : base(fileContext) { }
    }
}
