using LogParsers.Base;
using LogParsers.Base.Helpers;
using LogParsers.Base.Parsers;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers
{
    /// <summary>
    /// Parses legacy (prior to Tableau 9.3) space-delimited Postgres logs to JSON.
    /// </summary>
    public sealed class PostgresLegacyParser : AbstractMultiLineRegexParser, IParser
    {
        private static readonly string collectionName = ParserConstants.PgSqlCollectionName;
        private static readonly IList<string> indexNames = new List<string> { "file", "sev" };
        private static readonly CollectionSchema collectionSchema = ParserUtil.CreateCollectionSchema(collectionName, indexNames);

        private readonly IList<Regex> regexes = new List<Regex>
            {
                // Pre-9.3 Format
                new Regex(@"^
                            (?<ts>\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}.\d{3})\s
                            (?<ts_offset>.*?)\s
                            (?<pid>[0-9]+)\s
                            (?<sev>[A-Z]+?):\s\s
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

        public PostgresLegacyParser()
        {
        }

        public PostgresLegacyParser(LogFileContext fileContext)
            : base(fileContext)
        {
        }
    }
}