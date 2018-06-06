using LogParsers.Base;
using LogParsers.Base.Helpers;
using LogParsers.Base.Parsers;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers
{
    /// <summary>
    /// Parses DataEngine logs to JSON.
    /// </summary>
    public sealed class DataEngineParser : AbstractRegexParser, IParser
    {
        private static readonly string collectionName = ParserConstants.DataengineCollectionName;
        private static readonly IList<string> indexNames = new List<string> { "tid", "ts", "line", "file", "worker" };
        private static readonly CollectionSchema collectionSchema = ParserUtil.CreateCollectionSchema(collectionName, indexNames);

        private readonly IList<Regex> regexes = new List<Regex>
            {
                new Regex(@"^
                            (?<ts>\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}.\d{3})\s
                            \((?<tid>.*?)\):\s
                            (?<message>.*)",
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

        public DataEngineParser()
        {
        }

        public DataEngineParser(LogFileContext fileContext)
            : base(fileContext)
        {
        }
    }
}