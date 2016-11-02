using System;

namespace Logshark.Plugins.Tabadmin.Helpers
{
    internal abstract class DateTimeConversionHelper
    {
        /// <summary>
        /// Shift a DateTime by an offset value. Most likely useful to convert a local time to GMT.
        /// </summary>
        /// <param name="dateTime">DateTime to convert.</param>
        /// <param name="offset">Offset to apply to dateTime. Expected format is "+200" or "-1000" for +2 and -10 GMT, respectively.</param>
        /// <returns></returns>
        public static DateTime? ConvertDateTime(DateTime? dateTime, string offset)
        {
            if (dateTime == null)
            {
                return null;
            }
            else
            {
                return ((DateTime)dateTime).AddHours(Double.Parse(offset) / 100);
            }
        }
    }
}