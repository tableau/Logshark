using System;
using System.Collections.Generic;
using System.Text;

namespace Logshark.PluginLib.Extensions
{
    public static class DictionaryExtensions
    {
        private const string DefaultDictionaryFormatString = "{0}='{1}'";

        /// <summary>
        /// Prints the elements of a dictionary using a specified format string.  If the given format string is invalid, a default will be used.
        /// </summary>
        /// <param name="dictionary">The dictionary to print.</param>
        /// <param name="format">A format string. Must contain both "{0}" and "{1}" tokens. "{0}" will map to the key, and "{1}" will map to the value.</param>
        /// <param name="insertNewlinesBetweenElements">If true, an environment-appropriate newline will be printed after every element.</param>
        /// <returns>Formatted string representing all dictionary elements.</returns>
        public static string PrintFormatted<TKey,TValue>(this IDictionary<TKey,TValue> dictionary, string format, bool insertNewlinesBetweenElements = true)
        {
            if (format == null || !format.Contains("{0}") || !format.Contains("{1}"))
            {
                format = DefaultDictionaryFormatString;
            }

            var sb = new StringBuilder();
            foreach (var kvp in dictionary)
            {
                sb.AppendFormat(format, kvp.Key, kvp.Value);

                if (insertNewlinesBetweenElements)
                {
                    sb.Append(Environment.NewLine);
                }
            }

            return sb.ToString();
        }
    }
}