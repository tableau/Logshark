using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LogParsers.Helpers
{
    /// <summary>
    /// Provides functionality for standardizing the various DateTime formats we encounter into a single format.
    /// </summary>
    public static class TimestampStandardizer
    {
        private static readonly IList<string> KnownDateTimeFormats = new List<string>
            {
                "dd/MMM/yyyy:HH:mm:ss",
                "ddd MMM dd HH:mm:ss.FFFFFF yyyy"
            };

        /// <summary>
        /// For the sake of data consistency, any timestamps in logs should be converted to a common DateTime format.  If for some reason we can't parse it, we leave it alone.
        /// </summary>
        /// <param name="rawTimestamp">The raw DateTime string to convert.</param>
        /// <returns>Standardized DateTime string.</returns>
        public static object Standardize(string rawTimestamp)
        {
            DateTime parsedTimestamp;

            // First try to just use the built-in DateTime parser.
            if (DateTime.TryParse(rawTimestamp, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out parsedTimestamp))
            {
                return parsedTimestamp;
            }

            // Check each known format for a match.
            if (KnownDateTimeFormats.Any(format =>
                DateTime.TryParseExact(rawTimestamp, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out parsedTimestamp)))
            {
                return parsedTimestamp;
            }

            // No match found; give up and just use the raw value.
            return rawTimestamp;
        }
    }
}