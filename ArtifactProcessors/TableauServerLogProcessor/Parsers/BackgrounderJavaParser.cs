using LogParsers.Base;
using LogParsers.Base.Helpers;
using LogParsers.Base.Parsers;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers
{
    /// <summary>
    /// Parses Backgrounder Java logs to JSON.
    /// </summary>
    public sealed class BackgrounderJavaParser : AbstractMultiLineRegexParser, IParser
    {
        private static readonly string collectionName = ParserConstants.BackgrounderJavaCollectionName;
        private static readonly IList<string> indexNames = new List<string> { "class", "file", "sev", "ts", "worker" };
        private static readonly CollectionSchema collectionSchema = ParserUtil.CreateCollectionSchema(collectionName, indexNames);

        private readonly IList<Regex> regexes = new List<Regex>
            {
                // 10.4+
                // 10.4 added "job type" and 10.5 added "local request id", either of which may be empty and thus are marked optional here
                new Regex(@"^
                            (?<ts>\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}.\d{3})\s
                            (?<ts_offset>.+?)\s
                            \((?<site>.*?), (?<user>.*?), (?<data_sess_id>.*?), (?<vql_sess_id>.*?), (?<job_id>.*?), (:(?<job_type>.*?))? (,(?<local_req_id>.*?))?\)\s
                            (?<thread>.*?)\s
                            (?<service>.*?)?:\s
                            (?<sev>[A-Z]+)(\s+)
                            (?<class>.*?)\s-\s
                            (?<message>(.|\n)*)",
                    RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled),
                // 9.0 - 10.3
                new Regex(@"^
                            (?<ts>\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}.\d{3})\s
                            (?<ts_offset>.+?)\s
                            \((?<site>.*?), (?<user>.*?), (?<data_sess_id>.*?), (?<req>.*?)\)\s
                            (?<thread>.*?)\s
                            :\s
                            (?<sev>[A-Z]+)(\s+)
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

        public BackgrounderJavaParser()
        {
        }

        public BackgrounderJavaParser(LogFileContext fileContext)
            : base(fileContext)
        {
        }
    }
}