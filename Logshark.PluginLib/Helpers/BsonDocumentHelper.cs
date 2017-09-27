using MongoDB.Bson;
using System;

namespace Logshark.PluginLib.Helpers
{
    //This class shouldn't have to exist, but unfortunately MongoDB will store certain things as ints or longs depending on the value
    //and we end up with a casting exception if we try to apply them to a mismatched type, by toStringing everything and then using
    //the class function to parse we guarantee we will be able to parse it up no matter what it comes in as.
    //
    //Since we already have it, I'm tossing some extra helper functions in here as well.
    public static class BsonDocumentHelper
    {
        public static String GetKeyType(BsonDocument document)
        {
            return document.GetValue("k").AsString;
        }

        public static BsonDocument GetValuesStruct(BsonDocument document)
        {
            return document.GetValue("v").AsBsonDocument;
        }

        public static bool GetBool(string key, BsonDocument document)
        {
            return Boolean.Parse(document.GetValue(key).ToString());
        }

        public static int GetInt(string key, BsonDocument document)
        {
            return Int32.Parse(document.GetValue(key).ToString());
        }

        public static int? GetNullableInt(string key, BsonDocument document)
        {
            try
            {
                return GetInt(key, document);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static long GetLong(string key, BsonDocument document)
        {
            return Int64.Parse(document.GetValue(key).ToString());
        }

        public static bool? GetNullableBool(string key, BsonDocument document)
        {
            try
            {
                return GetBool(key, document);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static long? GetNullableLong(string key, BsonDocument document)
        {
            try
            {
                return GetLong(key, document);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static float GetFloat(string key, BsonDocument document)
        {
            return Single.Parse(document.GetValue(key).ToString());
        }

        public static float? GetNullableFloat(string key, BsonDocument document)
        {
            try
            {
                return GetFloat(key, document);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static double GetDouble(string key, BsonDocument document)
        {
            return Double.Parse(document.GetValue(key).ToString());
        }

        public static double? GetNullableDouble(string key, BsonDocument document)
        {
            try
            {
                return GetDouble(key, document);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string GetString(string key, BsonDocument document)
        {
            try
            {
                return document.GetValue(key).AsString;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static BsonDocument GetBsonDocument(string key, BsonDocument document)
        {
            try
            {
                return document.GetValue(key).AsBsonDocument;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static DateTime GetDateTime(string key, BsonDocument document)
        {
            return document.GetValue(key).ToUniversalTime();
        }

        public static DateTime? GetNullableDateTime(string key, BsonDocument document)
        {
            try
            {
                return GetDateTime(key, document);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string TruncateString(string fullString, int characterLimit)
        {
            if (fullString == null)
            {
                return null;
            }

            int maxStringLength = characterLimit;
            if (fullString.Length > maxStringLength)
            {
                return fullString.Substring(0, maxStringLength);
            }
            else
            {
                return fullString;
            }
        }

        public static BsonValue GetPath(this BsonValue bson, string path)
        {
            if (bson.BsonType != BsonType.Document)
            {
                throw new Exception("Not a document");
            }

            var doc = bson.AsBsonDocument;

            var tokens = path.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length == 0)
            {
                return doc;
            }

            if (!doc.Contains(tokens[0]))
            {
                return BsonNull.Value;
            }

            if (tokens.Length > 1)
            {
                return GetPath(doc[tokens[0]], tokens[1]);
            }

            return doc[tokens[0]];
        }
    }
}