using LogParsers.Base.Helpers;
using LogParsers.Base.Parsers;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsing.Parsers
{
    /// <summary>
    /// Handles parsing pg_hba.conf files to JSON.
    /// </summary>
    public sealed class PostgresHostConfigParser : AbstractSingleDocumentRegexParser, IParser
    {
        private static readonly string collectionName = ParserConstants.ConfigCollectionName;
        private static readonly IList<string> indexNames = new List<string>();
        private static readonly CollectionSchema collectionSchema = ParserUtil.CreateCollectionSchema(collectionName, indexNames);

        private readonly IList<Regex> regexes = new List<Regex>
            {
                new Regex(@"^
                            (?<connection>local|host|hostssl|hostnossl)\s+
                            (?<database>.+?)\s+
                            (?<user>.+?)\s+
                            ((?<address>.+?)/(?<mask_length>\d+)\s+)?
                            (?<auth_method>.+?)
                            (\s+?<auth_options>.+?)?
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

        public PostgresHostConfigParser()
        {
        }

        public PostgresHostConfigParser(LogFileContext fileContext) : base(fileContext)
        {
        }
    }
}