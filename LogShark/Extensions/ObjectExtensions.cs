using System.Text.RegularExpressions;

namespace LogShark.Extensions
{
    public static class ObjectExtensions
    {
        public static Match CastToStringAndRegexMatch(this object stringAsObject, Regex regex)
        {
            return stringAsObject is string str
                ? regex.Match(str)
                : null;
        }
    }
}