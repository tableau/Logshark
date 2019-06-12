using LogParsers.Base;
using LogParsers.Base.Helpers;
using LogParsers.Base.Parsers;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers
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
                // 2018.2+ format
                new Regex(@"^
                            (?<request_ip>.+?)\s
                            (?<requester>.+?)\s
                            (?<remote_user>.+?)\s
                            (?<ts>\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3})\s
                            ""(?<ts_offset>.*?)""\s
                            (?<port>\d{1,5})\s
                            ""((?<request_method>[A-Z]+)\s)?(?<resource>.+?)(\sHTTP\/(?<http_version>.+?))?""\s
                            ""(?<xforwarded_for>.+?)""\s
                            (?<status_code>\d{3})\s
                            (?<response_size>.+?)\s
                            ""((?<content_length>-?[0-9]*?)|(.*?))""\s
                            (?<request_time>\d+)\s
                            (?<request_id>.*)",
                    RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled),
                // 9.x - 2018.1 format
                new Regex(@"^
                            (?<request_ip>.+?)\s
                            -\s
                            (?<requester>.+?)\s
                            (?<ts>\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}\.\d{3})\s
                            (?<ts_offset>.*?)\s
                            (?<port>\d{1,5})\s
                            ""((?<request_method>[A-Z]+)\s)?(?<resource>.+?)(\sHTTP\/(?<http_version>.+?))?""\s
                            ""(?<xforwarded_for>.+?)""\s
                            (?<status_code>\d{3})\s
                            (?<response_size>.+?)\s
                            ""((?<content_length>-?[0-9]*?)|(.*?))""\s
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
                            ""((?<request_method>[A-Z]+)\s)?(?<resource>.+?)(\sHTTP\/(?<http_version>.+?))?""\s
                            ""(?<xforwarded_for>.+?)""\s
                            (?<status_code>\d{3})\s
                            (?<response_size>.+?)\s""
                            ((?<content_length>-?[0-9]*?)|(.*?))""\s
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

        public HttpdParser(LogFileContext fileContext)
            : base(fileContext)
        {
        }
    }
}