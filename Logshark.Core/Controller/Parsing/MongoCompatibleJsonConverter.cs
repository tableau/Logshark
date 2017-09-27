using Logshark.Core.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace Logshark.Core.Controller.Parsing
{
    /// <summary>
    /// Custom JSON Converter that enforces MongoDB JSON syntax.
    /// </summary>
    internal class MongoCompatibleJsonConverter : JsonConverter
    {
        private readonly Type[] types;
        private static readonly JavaScriptDateTimeConverter dateTimeConverter = new JavaScriptDateTimeConverter();

        public MongoCompatibleJsonConverter(params Type[] types)
        {
            this.types = types;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JToken jToken = JToken.FromObject(value);

            if (jToken.Type != JTokenType.Object)
            {
                jToken.WriteTo(writer, dateTimeConverter);
            }
            else
            {
                JObject jObject = (JObject)jToken;

                // Find any properties with illegal field names and replace them with well-formed properties to avoid an insert failure down the road.
                var propertiesWithIllegalNames = MongoJsonHelper.FindPropertiesWithIllegalNames(jObject);
                foreach (var propertyWithIllegalName in propertiesWithIllegalNames)
                {
                    JProperty propertyWithLegalName = MongoJsonHelper.CreateLegalCopy(propertyWithIllegalName);
                    propertyWithIllegalName.Replace(propertyWithLegalName);
                }

                jObject.WriteTo(writer, dateTimeConverter);
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
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
    }
}