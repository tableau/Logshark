using LogParsers.Base.Helpers;
using LogParsers.Base.Parsers;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsing.Parsers
{
    /// <summary>
    /// Parses httpd logs to JSON.
    /// </summary>
    public sealed class HttpdParser : AbstractRegexParser, IParser
    {
        private static readonly string collectionName = ParserConstants.HttpdCollectionName;
        private static readonly IList<string> indexNames = new List<string> { "file", "status_code", "request_id" };
        private static readonly CollectionSchema collectionSchema = ParserUtil.CreateCollectionSchema(collectionName, indexNames);

        private readonly IList<Regex> regexes = new List<Regex>
            {
                // 9.x format
                new Regex(@"^
                            (?<request_ip>.+?)\s
                            -\s
                            (?<requester>.+?)\s
                            (?<ts>\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}\.\d{3})\s
                            (?<ts_offset>.*?)\s
                            (?<port>\d{1,5})\s
                            ""(?<request_method>[A-Z]+)\s(?<resource>.+?)(\sHTTP/(?<http_version>.+?))?""\s
                            ""(?<xforwarded_for>.+?)""\s
                            (?<status_code>\d{3})\s
                            (?<response_size>.+?)\s
                            ""(?<content_length>.*?)""\s
                            (?<request_time>\d+)\s
                            (?<request_id>.*)",
                    RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled),
                // 8.x format
                new Regex(@"^
                            (?<request_ip>.+?)\s
                            -\s
                            (?<requester>.+?)\s
                            \[(?<ts>\d+.*?)\s(?<ts_offset>.*?)\]\s
                            (?<port>\d{1,5})\s
                            ""(?<request_method>[A-Z]+)\s(?<resource>.+?)(\sHTTP/(?<http_version>.+?))?""\s
                            ""(?<xforwarded_for>.+?)""\s
                            (?<status_code>\d{3})\s
                            (?<response_size>.+?)\s""
                            (?<content_length>.*?)""\s
                            (?<request_time>\d+)\s
                            (?<request_id>.*)",
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

        public HttpdParser()
        {
        }

        public HttpdParser(LogFileContext fileContext) : base(fileContext)
        {
        }
    }
}