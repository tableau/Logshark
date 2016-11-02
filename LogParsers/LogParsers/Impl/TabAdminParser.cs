using System.Collections.Generic;
using System.Text.RegularExpressions;
using LogParsers.Helpers;

namespace LogParsers
{
    /// <summary>
    /// Parses TabAdmin logs to JSON.
    /// </summary>
    public sealed class TabAdminParser : AbstractMultiLineRegexParser, IParser
    {
        private static readonly string collectionName = ParserConstants.TabAdminCollectionName;
        private static readonly IList<string> indexNames = new List<string>() { "sev" };
        private static readonly CollectionSchema collectionSchema = ParserUtil.CreateCollectionSchema(collectionName, indexNames);

        private readonly IList<Regex> regexes = new List<Regex>
            {
                // logs\ style.
                new Regex(@"^
                            (?<ts>\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}.\d{3})\s
                            (?<ts_offset>.*?)_
                            (?<sev>[A-Z]+)_
                            (?<address>.*?):
                            (?<hostname>.*?)_:_
                            pid=(?<pid>\d*)_
                            (.*?)__
                            user=(?<user>.*?)__
                            request=(?<req>.*?)__\s
                            (?<message>(.|\n)*)",
                    RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled),
                // tabadmin\ style.
                new Regex(@"^
                            (?<ts>\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}.\d{3})\s
                            (?<ts_offset>.*?)\s
                            (?<tid>.*?)\s+
                            (?<sev>[A-Z]+)\s+:\s+
                            (?<class>.*?)\s-\s
                            (?<message>(.|\n)*)",
                    RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled)
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

        public TabAdminParser() { }
        public TabAdminParser(LogFileContext fileContext) : base(fileContext) { }
    }
}
