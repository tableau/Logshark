using LogParsers.Base;
using LogParsers.Base.Helpers;
using LogParsers.Base.Parsers;
using System.Collections.Generic;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers
{
    /// <summary>
    /// Parses Tabadmin Controller C++ logs from JSON->JSON.
    /// </summary>
    public sealed class TabAdminControllerCppParser : AbstractJsonParser, IParser
    {
        private static readonly string collectionName = ParserConstants.TabAdminControllerCppCollectionName;
        private static readonly IList<string> indexNames = new List<string> { "k", "file", "pid", "sev", "worker" };
        private static readonly CollectionSchema collectionSchema = ParserUtil.CreateCollectionSchema(collectionName, indexNames);

        public override CollectionSchema CollectionSchema
        {
            get
            {
                return collectionSchema;
            }
        }

        public TabAdminControllerCppParser()
        {
        }

        public TabAdminControllerCppParser(LogFileContext fileContext)
            : base(fileContext)
        {
        }
    }
}