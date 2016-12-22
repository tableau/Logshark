using System;

namespace LogParsers.Extensions
{
    internal static class TimespanExtensions
    {
        /// <summary>
        /// Converts a TimeSpan offset to a short string version, i.e. "-07:00:00" is transformed to "-0700".
        /// </summary>
        /// <param name="offset">The TimeSpan value containing the offset.</param>
        /// <returns>Short string version of the offset.</returns>
        public static string ToShortString(this TimeSpan offset)
        {
            return offset.ToString().Remove(6).Replace(":", "");
        }
    }
}
