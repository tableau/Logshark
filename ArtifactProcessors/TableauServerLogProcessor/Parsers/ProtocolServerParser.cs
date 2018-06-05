using LogParsers.Base;
using LogParsers.Base.Helpers;
using LogParsers.Base.Parsers;
using System.Collections.Generic;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers
{
    /// <summary>
    /// Parses TabProtoSrv logs to JSON.
    /// </summary>
    public sealed class ProtocolServerParser : AbstractJsonParser, IParser
    {
        private static readonly string collectionName = ParserConstants.ProtocolServerCollectionName;
        private static readonly IList<string> indexNames = new List<string> { "k", "file", "pid", "req", "sess", "sev", "worker" };
        private static readonly CollectionSchema collectionSchema = ParserUtil.CreateCollectionSchema(collectionName, indexNames);

        public override CollectionSchema CollectionSchema
        {
            get
            {
                return collectionSchema;
            }
        }

        public ProtocolServerParser()
        {
        }

        public ProtocolServerParser(LogFileContext fileContext)
            : base(fileContext)
        {
        }
    }
}