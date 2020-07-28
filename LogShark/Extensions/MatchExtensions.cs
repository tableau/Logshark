using System;
using System.Text.RegularExpressions;

namespace LogShark.Extensions
{
    public static class MatchExtensions
    {
        public static string GetString(this Match match, string groupName)
        {
            return match.Groups[groupName].Value;
        }

        public static string GetNullableString(this Match match, string groupName)
        {
            var value = match.GetString(groupName);
            return string.IsNullOrEmpty(value) ? null : value;
        }
        
        public static long? GetNullableLong(this Match match, string groupName)
        {
            var str = match.Groups[groupName].Value;

            var success = long.TryParse(str, out var res);

            return success 
                ? res 
                : (long?) null;
        }

        public static int? GetNullableInt(this Match match, string groupName)
        {
            var str = match.Groups[groupName].Value;
            
            var success = int.TryParse(str, out var res);

            return success
                ? res
                : (int?) null;
        }
        
        public static double? GetNullableDoubleWithDelimiterNormalization(this Match match, string groupName)
        {
            var str = match.Groups[groupName].Value;
            var normalizedStr = str?.Replace(",", ".");
            
            var success = double.TryParse(normalizedStr, out var res);

            return success 
                ? res
                : (double?) null;
        }
    }
}