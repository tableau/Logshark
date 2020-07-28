using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace LogShark.Extensions
{
    public static class JTokenExtensions
    {
        public static string GetStringFromPath(this JToken token, string path)
        {
            return token.SelectToken(path, false)?.Value<string>();
        }

        public static string GetStringFromPaths(this JToken token, params string[] paths)
        {
            return paths.Select(token.GetStringFromPath).FirstOrDefault(value => value != null);
        }

        public static double? GetDoubleFromPath(this JToken token, string path)
        {
            var str = GetStringFromPath(token, path);

            return str != null && double.TryParse(str, out var result)
                ? result
                : (double?) null;
        }
        
        public static double? GetDoubleFromPaths(this JToken token, params string[] paths)
        {
            return paths.Select(token.GetDoubleFromPath).FirstOrDefault(value => value != null);
        }
        
        public static int? GetIntFromPath(this JToken token, string path)
        {
            var str = GetStringFromPath(token, path);

            return str != null && int.TryParse(str, out var result)
                ? result
                : (int?) null;
        }
        
        public static long? GetLongFromPath(this JToken token, string path)
        {
            var str = GetStringFromPath(token, path);

            return str != null && long.TryParse(str, out var result)
                ? result
                : (long?) null;
        }
        
        /// <summary>
        /// This method allows to capture more numeric styles (i.e. "65,535"), but number parsing twice as slow (399 vs 212 seconds for 1B conversions)
        /// </summary>
        public static long? GetLongFromPathAnyNumberStyle(this JToken token, string path)
        {
            var str = GetStringFromPath(token, path);

            return str != null && long.TryParse(str, NumberStyles.Any, null, out var result)
                ? result
                : (long?) null;
        }
    }
}