using System.Collections.Generic;
using LogParsers.Helpers;

namespace LogParsers
{
    /// <summary>
    /// Parses Backgrounder C++ logs from JSON->JSON.
    /// </summary>
    public sealed class BackgrounderCppParser : AbstractJsonParser, IParser
    {
        private static readonly string collectionName = ParserConstants.BackgrounderCppCollectionName;
        private static readonly IList<string> indexNames = new List<string> { "k", "file", "pid", "req", "sess", "sev", "worker" };
        private static readonly CollectionSchema collectionSchema = ParserUtil.CreateCollectionSchema(collectionName, indexNames);

        public override CollectionSchema CollectionSchema
        {
            get
            {
                return collectionSchema;
            }
        }

        public BackgrounderCppParser() { }
        public BackgrounderCppParser(LogFileContext fileContext) : base(fileContext) { }
    }
}
