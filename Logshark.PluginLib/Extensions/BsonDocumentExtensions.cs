using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
using System;

namespace Logshark.PluginLib.Extensions
{
    public static class BsonDocumentExtensions
    {
        public static bool GetBool(this BsonDocument document, string key)
        {
            return BsonDocumentHelper.GetBool(key, document);
        }

        public static DateTime GetDateTime(this BsonDocument document, string key)
        {
            return BsonDocumentHelper.GetDateTime(key, document);
        }

        public static double GetDouble(this BsonDocument document, string key)
        {
            return BsonDocumentHelper.GetDouble(key, document);
        }

        public static float GetFloat(this BsonDocument document, string key)
        {
            return BsonDocumentHelper.GetFloat(key, document);
        }

        public static int GetInt(this BsonDocument document, string key)
        {
            return BsonDocumentHelper.GetInt(key, document);
        }

        public static long GetLong(this BsonDocument document, string key)
        {
            return BsonDocumentHelper.GetLong(key, document);
        }

        public static bool? GetNullableBool(this BsonDocument document, string key)
        {
            return BsonDocumentHelper.GetNullableBool(key, document);
        }

        public static DateTime? GetNullableDateTime(this BsonDocument document, string key)
        {
            return BsonDocumentHelper.GetNullableDateTime(key, document);
        }

        public static double? GetNullableDouble(this BsonDocument document, string key)
        {
            return BsonDocumentHelper.GetNullableDouble(key, document);
        }

        public static float? GetNullableFloat(this BsonDocument document, string key)
        {
            return BsonDocumentHelper.GetNullableFloat(key, document);
        }

        public static int? GetNullableInt(this BsonDocument document, string key)
        {
            return BsonDocumentHelper.GetNullableInt(key, document);
        }

        public static long? GetNullableLong(this BsonDocument document, string key)
        {
            return BsonDocumentHelper.GetNullableLong(key, document);
        }

        public static string GetString(this BsonDocument document, string key)
        {
            return BsonDocumentHelper.GetString(key, document);
        }
    }
}