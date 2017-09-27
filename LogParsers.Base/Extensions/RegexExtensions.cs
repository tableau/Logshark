using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LogParsers.Base.Extensions
{
    public static class RegexExtensions
    {
        /// <summary>
        /// Apply a regex to a text input and map any named captures to a dictionary.
        /// </summary>
        /// <param name="regex">The regex to apply.  Must contain named captures.</param>
        /// <param name="input">The input to apply the regex to.</param>
        /// <returns>Dictionary containg k,v pairs of capturename,value.</returns>
        public static IDictionary<string, object> MatchNamedCaptures(this Regex regex, string input)
        {
            var namedCaptureDictionary = new Dictionary<string, object>();
            GroupCollection groups = regex.Match(input).Groups;
            string[] groupNames = regex.GetGroupNames();

            foreach (var groupName in groupNames)
            {
                if (groups[groupName].Captures.Count > 0 && groupName != "0")
                {
                    namedCaptureDictionary.Add(groupName, groups[groupName].Value);
                }
            }

            return namedCaptureDictionary;
        }
    }
}