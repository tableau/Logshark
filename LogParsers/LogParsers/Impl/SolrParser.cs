using System.Collections.Generic;
using System.Text.RegularExpressions;
using LogParsers.Helpers;

namespace LogParsers
{
    /// <summary>
    /// Parses Solr logs to JSON.
    /// </summary>
    public sealed class SolrParser : AbstractRegexParser, IParser
    {
        private static readonly string collectionName = ParserConstants.SolrCollectionName;
        private static readonly IList<string> indexNames = new List<string>() { "status_code" };
        private static readonly CollectionSchema collectionSchema = ParserUtil.CreateCollectionSchema(collectionName, indexNames);

        private readonly IList<Regex> regexes = new List<Regex> {
                new Regex(@"^
                            (?<request_ip>.+?)\s-\s-\s\[
                            (?<ts>.*?)\s
                            (?<ts_offset>.*?)\]\s""
                            (?<request_method>.*?)\s
                            (?<resource>.*?)\s
                            (?<http_version>.*?)""\s
                            (?<status_code>.*?)\s
                            (?<response_size>.*)",
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

        public SolrParser() { }
        public SolrParser(LogFileContext fileContext) : base(fileContext) { }
    }
}
