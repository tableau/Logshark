using System;
using System.Data.Entity.Design.PluralizationServices;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Logshark.Common.Extensions
{
    /// <summary>
    /// Extension methods for the System.String class.
    /// </summary>
    public static class StringExtensions
    {
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

        /// <summary>
        /// Indicates whether a string matches the pattern of an MD5.
        /// </summary>
        /// <param name="str">The source string to check the pattern of.</param>
        /// <returns>True if the source string matches the pattern of an MD5 (is length 32 and composed of only alphanumeric characters).</returns>
        public static bool IsValidMD5(this string str)
        {
            Regex rgx = new Regex(@"[a-fA-F0-9]{32}");
            return rgx.IsMatch(str);
        }

        /// <summary>
        /// Returns a string containing a specified number of characters from the left side of a string.
        /// </summary>
        /// <param name="str">String expression from which the leftmost characters are returned.</param>
        /// <param name="length">The number of characters to return.  If greater than or equal to the number of characters in str, the entire string is returned.</param>
        /// <returns>Returns a string containing a specified number of characters from the left side of a string.</returns>
        public static string Left(this string str, int length)
        {
            return str.Substring(0, Math.Min(length, str.Length));
        }

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
        /// Strips all non-alphanumeric characters from a string.
        /// </summary>
        /// <param name="str">The string to remove special characters from.</param>
        /// <returns>The source string with all non-alphanumeric characters removed.</returns>
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

        /// <summary>
        /// Returns a string containing a specified number of characters from the right side of a string.
        /// </summary>
        /// <param name="str">String expression from which the rightmost characters are returned.</param>
        /// <param name="length">The number of characters to return.  If greater than or equal to the number of characters in str, the entire string is returned.</param>
        /// <returns>Returns a string containing a specified number of characters from the right side of a string.</returns>
        public static string Right(this string str, int length)
        {
            return str.Substring(str.Length - Math.Min(length, str.Length));
        }
    }
}