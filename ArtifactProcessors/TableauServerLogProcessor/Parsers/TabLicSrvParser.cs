using LogParsers.Base;
using LogParsers.Base.Helpers;
using LogParsers.Base.Parsers;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers
{
    /// <summary>
    /// Parses TabLicSrv logs to JSON.
    /// </summary>
    public sealed class TabLicSrvParser : AbstractRegexParser, IParser
    {
        private static readonly string collectionName = ParserConstants.TabLicSrvCollectionName;
        private static readonly IList<string> indexNames = new List<string>();
        private static readonly CollectionSchema collectionSchema = ParserUtil.CreateCollectionSchema(collectionName, indexNames);

        private readonly IList<Regex> regexes = new List<Regex>
            {
                new Regex(@"^
                            (?<ts>.*?)\s\(
                            (?<process>.*?)\)
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

        public TabLicSrvParser()
        {
        }

        public TabLicSrvParser(LogFileContext fileContext)
            : base(fileContext)
        {
        }
    }
}