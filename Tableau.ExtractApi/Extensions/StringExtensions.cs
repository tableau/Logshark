using System;
using System.Text.RegularExpressions;

namespace Tableau.ExtractApi.Extensions
{
    public static class StringExtensions
    {
        private static readonly Regex LeadingUnderscoresRegex = new Regex("^_+", RegexOptions.Compiled);
        private static readonly Regex SnakeCaseRegex = new Regex("([a-z0-9])([A-Z])", RegexOptions.Compiled);

        public static string ToSnakeCase(this string input)
        {
            if (String.IsNullOrEmpty(input))
            {
                return input;
            }

            var leadingUnderscores = LeadingUnderscoresRegex.Match(input);

            return leadingUnderscores + SnakeCaseRegex.Replace(input, "$1_$2").ToLower();
        }
    }
}