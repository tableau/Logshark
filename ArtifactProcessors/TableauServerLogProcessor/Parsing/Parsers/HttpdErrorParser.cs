using LogParsers.Base.Helpers;
using LogParsers.Base.Parsers;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsing.Parsers
{
    /// <summary>
    /// Parses HttpdError logs to JSON.
    /// </summary>
    public sealed class HttpdErrorParser : AbstractRegexParser, IParser
    {
        private static readonly string collectionName = ParserConstants.HttpdCollectionName;
        private static readonly IList<string> indexNames = new List<string> { "sev" };
        private static readonly CollectionSchema collectionSchema = ParserUtil.CreateCollectionSchema(collectionName, indexNames);

        private readonly IList<Regex> regexes = new List<Regex>
            {
                new Regex(@"^
                            \[(?<ts>.*?)\]\s
                            \[(?<module>.*?):(?<sev>.*?)\]\s
                            \[pid\s(?<pid>\d+):tid\s(?<tid>\d+)\]\s
                            (\[client\s(?<client_ip>.*?):(?<client_port>\d+)\]\s)?
                            (\((?<error_code>.+?)\))?
                            ((?<error_code>[A-Z]{2}\d+):\s)?
                            (?<message>.*)",
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

        public HttpdErrorParser()
        {
        }

        public HttpdErrorParser(LogFileContext fileContext) : base(fileContext)
        {
        }
    }
}