using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Logshark.Core.Controller.Parsing.Mongo
{
    /// <summary>
    /// Custom JSON Converter that enforces MongoDB JSON syntax.
    /// </summary>
    internal class MongoCompatibleJsonConverter : JsonConverter
    {
        protected readonly Type[] types;

        protected static readonly JavaScriptDateTimeConverter DateTimeConverter = new JavaScriptDateTimeConverter();

        public MongoCompatibleJsonConverter(params Type[] types)
        {
            this.types = types;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JToken jToken = JToken.FromObject(value);

            if (jToken.Type != JTokenType.Object)
            {
                jToken.WriteTo(writer, DateTimeConverter);
            }
            else
            {
                JObject jObject = (JObject)jToken;

                // Find any properties with illegal field names and replace them with well-formed properties to avoid an insert failure down the road.
                var propertiesWithIllegalNames = FindPropertiesWithIllegalNames(jObject);
                foreach (var propertyWithIllegalName in propertiesWithIllegalNames)
                {
                    JProperty propertyWithLegalName = CreateLegalCopy(propertyWithIllegalName);
                    propertyWithIllegalName.Replace(propertyWithLegalName);
                }

                jObject.WriteTo(writer, DateTimeConverter);
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanConvert(Type objectType)
        {
            return types.Any(t => t == objectType);
        }

        /// <summary>
        /// Indicates whether a field name is illegal according to MongoDB syntax.
        /// </summary>
        /// <param name="name">The name to check.</param>
        /// <returns>True if a field name is illegal according to MongoDB syntax.</returns>
        protected static bool IsIllegalFieldName(string name)
        {
            return name.StartsWith("$") || name.Contains(".");
        }

        /// <summary>
        /// Creates a copy of an existing JProperty, transforming the name to a legal Mongo name if necessary.
        /// </summary>
        /// <param name="jProperty">The property to make a copy of.</param>
        protected static JProperty CreateLegalCopy(JProperty jProperty)
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
        protected static ICollection<JProperty> FindPropertiesWithIllegalNames(JToken node)
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
        protected static void AddIllegalFieldNamesToList(JToken node, ICollection<JProperty> list)
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