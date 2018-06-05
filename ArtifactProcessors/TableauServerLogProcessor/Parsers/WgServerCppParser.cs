using LogParsers.Base;
using LogParsers.Base.Helpers;
using LogParsers.Base.Parsers;
using System.Collections.Generic;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers
{
    /// <summary>
    /// Parses WgServer C++ logs to JSON.
    /// </summary>
    public sealed class WgServerCppParser : AbstractJsonParser, IParser
    {
        private static readonly string collectionName = ParserConstants.WgServerCppCollectionName;
        private static readonly IList<string> indexNames = new List<string> { "k", "file", "pid", "req", "sess", "sev", "worker" };
        private static readonly CollectionSchema collectionSchema = ParserUtil.CreateCollectionSchema(collectionName, indexNames);

        public override CollectionSchema CollectionSchema
        {
            get
            {
                return collectionSchema;
            }
        }

        public WgServerCppParser()
        {
        }

        public WgServerCppParser(LogFileContext fileContext)
            : base(fileContext)
        {
        }
    }
}