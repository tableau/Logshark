using LogParsers.Base.Helpers;
using LogParsers.Base.Parsers;
using System.Collections.Generic;

namespace Logshark.ArtifactProcessors.$safeprojectname$.Parsing.Parsers
{
    /// <summary>
    /// Sample parser that parses a log file to JSON.
    /// </summary>
    public sealed class SampleJsonParser : AbstractJsonParser, IParser
    {
        private static readonly string collectionName = ParserConstants.SampleCollectionName;
        private static readonly IList<string> indexNames = new List<string> { /* Fields which should be indexed go here */ };
        private static readonly CollectionSchema collectionSchema = ParserUtil.CreateCollectionSchema(collectionName, indexNames);

        public override CollectionSchema CollectionSchema
        {
            get
            {
                return collectionSchema;
            }
        }

        public SampleJsonParser()
        {
        }

        public SampleJsonParser(LogFileContext fileContext) : base(fileContext)
        {
        }
    }
}
