using Newtonsoft.Json.Linq;

namespace LogShark.Extensions
{
    public static class JTokenExtensions
    {
        public static string GetStringFromPath(this JToken token, string path)
        {
            return token.SelectToken(path, false)?.Value<string>();
        }

        public static double? GetDoubleFromPath(this JToken token, string path)
        {
            var str = GetStringFromPath(token, path);

            return str != null && double.TryParse(str, out var result)
                ? result
                : (double?) null;
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
    }
}