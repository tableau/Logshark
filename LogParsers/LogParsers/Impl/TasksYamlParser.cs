using System.Collections.Generic;
using System.Linq;
using LogParsers.Extensions;
using LogParsers.Helpers;
using Newtonsoft.Json.Linq;

namespace LogParsers
{
    /// <summary>
    /// Parses tasks.yml files into a single JSON document.
    /// </summary>
    public class TasksYamlParser : AbstractYamlParser, IParser
    {
        private static readonly string collectionName = ParserConstants.ConfigCollectionName;
        private static readonly IList<string> indexNames = new List<string>();
        private static readonly CollectionSchema collectionSchema = ParserUtil.CreateCollectionSchema(collectionName, indexNames);

        public override CollectionSchema CollectionSchema
        {
            get { return collectionSchema; }
        }

        public TasksYamlParser() { }
        public TasksYamlParser(LogFileContext fileContext) : base(fileContext) { }

        protected override JObject TransformYamlToJson(IList<object> documents)
        {
            // Sanity check.
            if (!(documents[0] is IList<object>))
            {
                return null;
            }

            var rawList = documents[0] as IList<object>;
            var configObject = new Dictionary<string, object>();
            foreach (var item in rawList)
            {
                if (item is IDictionary<object, object>)
                {
                    var rawDictionary = item as IDictionary<object, object>;
                    var configDictionary = rawDictionary.ToDictionary(k => k.Key.ToString(), k => k.Value);
                    var configHierarchy = configDictionary.PivotToHierarchy();

                    // Use the "name" field's value as the key, if it exists.
                    if (configHierarchy.ContainsKey("name"))
                    {
                        string name = configHierarchy["name"].ToString();
                        configHierarchy.Remove("name");
                        configObject.Add(name, JObject.FromObject(configHierarchy));
                    }
                    else
                    {
                        configObject.Add(rawList.IndexOf(item).ToString(), JObject.FromObject(configHierarchy));
                    }
                }
            }

            FinishedParsing = true;
            return InsertMetadata(new JObject { { "contents", JObject.FromObject(configObject) } });
        }
    }
}
