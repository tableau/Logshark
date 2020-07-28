using System;
using System.Collections.Generic;

namespace LogShark.Extensions
{
    public static class DictionaryExtensions
    {
        public static string GetStringValueOrNull(this IDictionary<string, string> dictionary, string key)
        {
            return dictionary.ContainsKey(key)
                ? dictionary[key]
                : null;
        }

        public static int? GetIntValueOrNull(this IDictionary<string, string> dictionary, string key)
        {
            var strValue = GetStringValueOrNull(dictionary, key);
            var parsed = int.TryParse(strValue, out var result);
            return parsed
                ? (int?)result
                : null;
        }
        
        public static long? GetLongValueOrNull(this IDictionary<string, string> dictionary, string key)
        {
            var strValue = GetStringValueOrNull(dictionary, key);
            var parsed = long.TryParse(strValue, out var result);
            return parsed
                ? (long?)result
                : null;
        }

        public static bool? GetBoolValueOrNull(this IDictionary<string, string> dictionary, string key)
        {
            var strValue = GetStringValueOrNull(dictionary, key);
            var parsed = bool.TryParse(strValue, out var result);
            return parsed
                ? (bool?)result
                : null;
        }

        public static IDictionary<string, List<T>> AddToDictionaryListOrCreate<T>(this IDictionary<string, List<T>> dict, string key, T value)
        {
            if (key != null)
            {
                dict.TryAdd(key, new List<T>());
                dict[key].Add(value);
            }
            return dict;
        }
    }
}