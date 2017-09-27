using System;

namespace Logshark.Common.Extensions
{
    /// <summary>
    /// Extensions for manipulating file sizes.
    /// </summary>
    public static class FileSizeExtensions
    {
        private const long OneKb = 1024;
        private const long OneMb = OneKb * 1024;
        private const long OneGb = OneMb * 1024;
        private const long OneTb = OneGb * 1024;

        /// <summary>
        /// Converts a large number of bytes to a reduced & prettier format, e.g. Mb or Kb.
        /// </summary>
        /// <param name="value">The number of bytes.</param>
        /// <param name="decimalPlaces">The precision to use.</param>
        /// <returns>String of the bytes converted to their largest-possible representation.</returns>
        public static string ToPrettySize(this int value, int decimalPlaces = 0)
        {
            return ToPrettySize(Convert.ToUInt64(value), decimalPlaces);
        }

        /// <summary>
        /// Converts a large number of bytes to a reduced & prettier format, e.g. Mb or Kb.
        /// </summary>
        /// <param name="value">The number of bytes.</param>
        /// <param name="decimalPlaces">The precision to use.</param>
        /// <returns>String of the bytes converted to their largest-possible representation.</returns>
        public static string ToPrettySize(this long value, int decimalPlaces = 0)
        {
            return ToPrettySize(Convert.ToUInt64(value), decimalPlaces);
        }

        /// <summary>
        /// Converts a large number of bytes to a reduced & prettier format, e.g. Mb or Kb.
        /// </summary>
        /// <param name="value">The number of bytes.</param>
        /// <param name="decimalPlaces">The precision to use.</param>
        /// <returns>String of the bytes converted to their largest-possible representation.</returns>
        public static string ToPrettySize(this ulong value, int decimalPlaces = 0)
        {
            var asTb = Math.Round((double)value / OneTb, decimalPlaces);
            var asGb = Math.Round((double)value / OneGb, decimalPlaces);
            var asMb = Math.Round((double)value / OneMb, decimalPlaces);
            var asKb = Math.Round((double)value / OneKb, decimalPlaces);
            string chosenValue = asTb > 1 ? string.Format("{0}Tb", asTb)
                : asGb > 1 ? string.Format("{0}Gb", asGb)
                : asMb > 1 ? string.Format("{0}Mb", asMb)
                : asKb > 1 ? string.Format("{0}Kb", asKb)
                : string.Format("{0}B", Math.Round((double)value, decimalPlaces));
            return chosenValue;
        }
    }
}