using Logshark.Controller.Parsing;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace Logshark.Helpers
{
    /// <summary>
    /// Helper class for dealing with Mongo-specific JSON manipulation.
    /// </summary>
    internal static class MongoJsonHelper
    {
        public static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            Converters = new List<JsonConverter>
            {
                new MongoCompatibleJsonConverter(typeof(JObject))
            }
        };

        /// <summary>
        /// Converts a JObject to a BsonDocument using a MongoDB-friendly JSON converter.
        /// </summary>
        /// <param name="jObject">The JSON object to convert.</param>
        /// <returns>The JObject as a BsonDocument</returns>
        public static BsonDocument GetBsonDocument(JObject jObject)
        {
            var json = JsonConvert.SerializeObject(jObject, JsonSerializerSettings);
            return BsonSerializer.Deserialize<BsonDocument>(json);
        }

        /// <summary>
        /// Indicates whether a field name is illegal according to MongoDB syntax.
        /// </summary>
        /// <param name="name">The name to check.</param>
        /// <returns>True if a field name is illegal according to MongoDB syntax.</returns>
        public static bool IsIllegalFieldName(string name)
        {
            return name.StartsWith("$") || name.Contains(".");
        }

        /// <summary>
        /// Creates a copy of an existing JProperty, transforming the name to a legal Mongo name if necessary.
        /// </summary>
        /// <param name="jProperty">The property to make a copy of.</param>
        public static JProperty CreateLegalCopy(JProperty jProperty)
        {
            string name = jProperty.Name;
            if (jProperty.Name.StartsWith("$"))
            {
                name = name.TrimStart('$');
            }
            if (jProperty.Name.Contains('.'))
            {
                name = jProperty.Name.Replace('.', '_');
            }
            return new JProperty(name, jProperty.Value);
        }

        /// <summary>
        /// Recursively descends through a JSON tree and finds any properties with illegal field names.
        /// </summary>
        /// <param name="node">The root node of the tree to traverse.</param>
        /// <returns>List of properties with illegal field names.</returns>
        public static ICollection<JProperty> FindPropertiesWithIllegalNames(JToken node)
        {
            ICollection<JProperty> propertiesWithIllegalFieldNames = new List<JProperty>();
            AddIllegalFieldNamesToList(node, propertiesWithIllegalFieldNames);
            return propertiesWithIllegalFieldNames;
        }

        /// <summary>
        /// The recursive step for FindPropertiesWithIllegalNames.
        /// </summary>
        /// <param name="node">The node to search.</param>
        /// <param name="list">List to add any properties containing an illegal field name to.</param>
        private static void AddIllegalFieldNamesToList(JToken node, ICollection<JProperty> list)
        {
            switch (node.Type)
            {
                case JTokenType.Object:
                    foreach (JProperty child in node.Children<JProperty>())
                    {
                        if (IsIllegalFieldName(child.Name))
                        {
                            list.Add(child);
                        }
                        AddIllegalFieldNamesToList(child.Value, list);
                    }
                    break;

                case JTokenType.Array:
                    foreach (JToken child in node.Children())
                    {
                        AddIllegalFieldNamesToList(child, list);
                    }
                    break;
            }
        }
    }
}