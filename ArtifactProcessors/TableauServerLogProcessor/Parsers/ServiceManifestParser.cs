using LogParsers.Base;
using LogParsers.Base.Helpers;
using LogParsers.Base.Parsers;
using System.Collections.Generic;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers
{
    /// <summary>
    /// Parses service manifest logs to JSON.
    /// </summary>
    public sealed class ServiceManifestParser : AbstractJsonParser, IParser
    {
        private static readonly string collectionName = ParserConstants.ServiceManifestCollectionName;
        private static readonly IList<string> indexNames = new List<string> { "baseName", "file" };
        private static readonly CollectionSchema collectionSchema = ParserUtil.CreateCollectionSchema(collectionName, indexNames);

        public override CollectionSchema CollectionSchema
        {
            get
            {
                return collectionSchema;
            }
        }

        public ServiceManifestParser()
        {
        }

        public ServiceManifestParser(LogFileContext fileContext)
            : base(fileContext)
        {
        }
    }
}