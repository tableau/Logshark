using LogParsers.Base;
using LogParsers.Base.Helpers;
using LogParsers.Base.Parsers;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers
{
    /// <summary>
    /// Parses SearchServerLocalhost logs to JSON.
    /// </summary>
    public sealed class SearchServerLocalhostParser : AbstractRegexParser, IParser
    {
        private static readonly string collectionName = ParserConstants.SearchServerCollectionName;
        private static readonly IList<string> indexNames = new List<string> { "status_code" };
        private static readonly CollectionSchema collectionSchema = ParserUtil.CreateCollectionSchema(collectionName, indexNames);

        private readonly IList<Regex> regexes = new List<Regex>
            {
                new Regex(@"^
                            (?<request_ip>.+?)\s-\s-\s\[
                            (?<ts>.*?)\s
                            (?<ts_offset>.*?)\]\s""
                            (?<request_method>.*?)\s
                            (?<resource>.*?)\s
                            (?<http_version>.*?)""\s
                            (?<status_code>.*?)\s
                            (?<response_size>.*)",
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

        public SearchServerLocalhostParser()
        {
        }

        public SearchServerLocalhostParser(LogFileContext fileContext)
            : base(fileContext)
        {
        }
    }
}