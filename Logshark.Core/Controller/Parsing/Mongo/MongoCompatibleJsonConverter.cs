using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Logshark.Core.Controller.Parsing.Mongo
{
    /// <summary>
    /// Custom JSON Converter that enforces MongoDB JSON syntax.
    /// </summary>
    internal class MongoCompatibleJsonConverter : JsonConverter
    {
        private readonly Type[] _types;

        private static readonly JavaScriptDateTimeConverter DateTimeConverter = new JavaScriptDateTimeConverter();

        public MongoCompatibleJsonConverter(params Type[] types)
        {
            _types = types;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var jToken = JToken.FromObject(value);

            if (jToken.Type != JTokenType.Object)
            {
                jToken.WriteTo(writer, DateTimeConverter);
            }
            else
            {
                var jObject = (JObject)jToken;

                CheckAndFixPropertiesRecursively(jObject);
                
                jObject.WriteTo(writer, DateTimeConverter);
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
        }

        public override bool CanRead => false;

        public override bool CanConvert(Type objectType)
        {
            return _types.Any(t => t == objectType);
        }
        
        /// <summary>
        /// Recursively descends through a JSON tree and fixes any properties with illegal field names in place
        /// </summary>
        private static void CheckAndFixPropertiesRecursively(JToken node)
        {
            switch (node.Type)
            {
                case JTokenType.Object:
                    var needFixing = node.Children<JProperty>().Any(prop => IsIllegalFieldNameForMongo(prop.Name));
                    if (needFixing)
                    {
                        CheckAndFixDirectChildren(node);
                    }
                    
                    foreach (var child in node.Children<JProperty>())
                    {
                        CheckAndFixPropertiesRecursively(child.Value);
                    }
                    break;

                case JTokenType.Array:
                    foreach (var child in node.Children())
                    {
                        CheckAndFixPropertiesRecursively(child);
                    }
                    break;
            }
        }
        
        private static bool IsIllegalFieldNameForMongo(string name)
        {
            return name.StartsWith("$") || name.Contains(".");
        }

        private static void CheckAndFixDirectChildren(JToken tokenToLookThrough)
        {
            var childrenAsList = tokenToLookThrough.Children <JProperty>().ToList();
            for (var i = 0; i < childrenAsList.Count; ++i) // Can't do foreach, because .Replace method below modifies currentChild
            {
                var currentChild = childrenAsList[i];
                if (IsIllegalFieldNameForMongo(currentChild.Name))
                {
                    var fixedChild = CreateLegalCopy(currentChild);
                    currentChild.Replace(fixedChild);
                }
            }     
        }
        
        private static JProperty CreateLegalCopy(JProperty jProperty)
        {
            var name = jProperty.Name
                .TrimStart('$')
                .Replace('.', '_');
            return new JProperty(name, jProperty.Value);
        }
    }
}