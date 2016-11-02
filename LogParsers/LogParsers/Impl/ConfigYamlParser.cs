using System;
using System.Collections.Generic;
using System.Linq;
using LogParsers.Extensions;
using LogParsers.Helpers;
using Newtonsoft.Json.Linq;

namespace LogParsers
{
    /// <summary>
    /// Parses config yaml files into a single JSON document.
    /// </summary>
    public class ConfigYamlParser : AbstractYamlParser, IParser
    {
        private static readonly string collectionName = ParserConstants.ConfigCollectionName;
        private static readonly IList<string> indexNames = new List<string> { "file", "worker" };
        private static readonly CollectionSchema collectionSchema = ParserUtil.CreateCollectionSchema(collectionName, indexNames);
        private readonly DateTime? lastModifiedTimestamp;

        public override CollectionSchema CollectionSchema
        {
            get { return collectionSchema; }
        }

        public ConfigYamlParser() { }
        public ConfigYamlParser(LogFileContext fileContext) : base(fileContext)
        {
            lastModifiedTimestamp = fileContext.LastWriteTime;
        }

        protected override JObject TransformYamlToJson(IList<object> documents)
        {
            // Sanity check.
            if (!(documents[0] is IDictionary<object, object>))
            {
                return null;
            }

            var rawDictionary = documents[0] as IDictionary<object, object>;
            var configDictionary = rawDictionary.ToDictionary(k => k.Key.ToString(), k => k.Value);
            var configHierarchy = configDictionary.PivotToHierarchy();

            FinishedParsing = true;
            return InsertMetadata(new JObject { { "contents", JObject.FromObject(configHierarchy) }, { "last_modified_at", lastModifiedTimestamp } });
        }
    }
}
