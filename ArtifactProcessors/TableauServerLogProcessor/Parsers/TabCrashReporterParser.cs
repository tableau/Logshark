using LogParsers.Base;
using LogParsers.Base.Helpers;
using LogParsers.Base.Parsers;
using System.Collections.Generic;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers
{
    /// <summary>
    /// Parses TabCrashReporter C++ logs to JSON.
    /// </summary>
    public sealed class TabCrashReporterParser : AbstractJsonParser, IParser
    {
        private static readonly string collectionName = ParserConstants.TabCrashReporterCollectionName;
        private static readonly IList<string> indexNames = new List<string> { "k", "file", "pid", "req", "sess", "sev", "worker" };
        private static readonly CollectionSchema collectionSchema = ParserUtil.CreateCollectionSchema(collectionName, indexNames);

        public override CollectionSchema CollectionSchema
        {
            get
            {
                return collectionSchema;
            }
        }

        public TabCrashReporterParser()
        {
        }

        public TabCrashReporterParser(LogFileContext fileContext)
            : base(fileContext)
        {
        }
    }
}