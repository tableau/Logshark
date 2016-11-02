using System.Collections.Generic;
using System.Text.RegularExpressions;
using LogParsers.Helpers;

namespace LogParsers
{
    /// <summary>
    /// Parses BuildVersion logs to JSON.
    /// </summary>
    public sealed class BuildVersionParser : AbstractMultiLineRegexParser, IParser
    {
        private static readonly string collectionName = ParserConstants.BuildVersionCollectionName;
        private static readonly IList<string> indexNames = new List<string> { "version" };
        private static readonly CollectionSchema collectionSchema = ParserUtil.CreateCollectionSchema(collectionName, indexNames);

        private readonly IList<Regex> regexes = new List<Regex>
            {
                new Regex(@"^
                            .*\n
                            \#\sVersion\s(?<version>.*?)\s.(?<build_version>.*?).\s(?<architecture>.*?)\n
                            \#\sBuildHost\s(?<build_host>.*?)\n
                            \#\sFilePath\s(?<build_file_path>.*?)\n
                            \#\sBuildTimestamp\s(?<build_timestamp>.*)",
                    RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled)
            };

        private readonly IList<Regex> lineDelimiterRegexes = new List<Regex>();

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
            get
            {
                return collectionSchema;
            }
        }

        protected override bool UseLineNumbers { get { return false; } }

        public BuildVersionParser() { }
        public BuildVersionParser(LogFileContext fileContext) : base(fileContext) { }
    }
}
