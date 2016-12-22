using System;
using System.Data.Entity.Design.PluralizationServices;
using System.Globalization;
using System.Text;

namespace Logshark.Extensions
{
    /// <summary>
    /// Extension methods for the System.String class.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Pluralizes a string.
        /// </summary>
        /// <param name="str">The string to pluralize.</param>
        /// <param name="count">The quantity used to pluralize.</param>
        /// <returns>Pluralized form of string.</returns>
        public static string Pluralize(this string str, int count)
        {
            if (count == 1)
            {
                return str;
            }
            return PluralizationService
                .CreateService(new CultureInfo("en-US"))
                .Pluralize(str);
        }

        /// <summary>
        /// Replace the last occurrence of a substring within a string.
        /// </summary>
        /// <param name="source">The source string to replace within.</param>
        /// <param name="searchString">The string to replace the last occurrence of.</param>
        /// <param name="replaceString">The string to replace the last occurrence with.</param>
        /// <param name="comparisonMethod">The StringComparison method to use to match.</param>
        /// <returns></returns>
        public static string ReplaceLastOccurrence(this string source, string searchString, string replaceString, StringComparison comparisonMethod)
        {
            int positionOfLastOccurrence = source.LastIndexOf(searchString, comparisonMethod);

            if (positionOfLastOccurrence == -1)
            {
                return source;               
            }

            string result = source.Remove(positionOfLastOccurrence, searchString.Length).Insert(positionOfLastOccurrence, replaceString);
            return result;
        }

        /// <summary>
        /// Aligns the string within a larger region.
        /// </summary>
        /// <param name="stringToCenter">The string to center.</param>
        /// <param name="totalLength">The total size of the region.</param>
        /// <returns>String centered in region of given size.</returns>
        public static string AlignCenter(this string stringToCenter, int totalLength)
        {
            return stringToCenter.PadLeft((totalLength - stringToCenter.Length) / 2 + stringToCenter.Length)
                                 .PadRight(totalLength);
        }

        public static string RemoveSpecialCharacters(this string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char ch in str)
            {
                if ((ch >= '0' && ch <= '9')
                    || (ch >= 'A' && ch <= 'Z')
                    || (ch >= 'a' && ch <= 'z'))
                {
                    sb.Append(ch);
                }
            }

            return sb.ToString();
        }
    }
}