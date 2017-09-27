using LogParsers.Base.Helpers;
using LogParsers.Base.Parsers;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsing.Parsers
{
    /// <summary>
    /// Parses connections.properties into a single JSON document.
    /// </summary>
    public sealed class ConnectionsConfigParser : AbstractSingleDocumentRegexParser, IParser
    {
        private static readonly string collectionName = ParserConstants.ConfigCollectionName;
        private static readonly IList<string> indexNames = new List<string>();
        private static readonly CollectionSchema collectionSchema = ParserUtil.CreateCollectionSchema(collectionName, indexNames);

        private readonly IList<Regex> regexes = new List<Regex>
            {
                new Regex(@"^
                            (?<key>.+?)
                            =
                            (?<value>.+?)
                            $",
                    RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled)
            };

        public override CollectionSchema CollectionSchema
        {
            get
            {
                return collectionSchema;
            }
        }

        protected override IList<Regex> Regexes
        {
            get { return regexes; }
        }

        public ConnectionsConfigParser()
        {
        }

        public ConnectionsConfigParser(LogFileContext fileContext) : base(fileContext)
        {
        }
    }
}