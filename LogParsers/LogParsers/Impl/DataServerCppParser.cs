using System.Collections.Generic;
using LogParsers.Helpers;

namespace LogParsers
{
    /// <summary>
    /// Parses DataServer C++ logs to JSON.
    /// </summary>
    public sealed class DataServerCppParser : AbstractJsonParser, IParser
    {
        private static readonly string collectionName = ParserConstants.DataserverCppCollectionName;
        private static readonly IList<string> indexNames = new List<string> { "k", "file", "pid", "req", "sess", "sev", "worker" };
        private static readonly CollectionSchema collectionSchema = ParserUtil.CreateCollectionSchema(collectionName, indexNames);

        public override CollectionSchema CollectionSchema
        {
            get
            {
                return collectionSchema;
            }
        }

        public DataServerCppParser() { }
        public DataServerCppParser(LogFileContext fileContext) : base(fileContext) { }
    }
}
