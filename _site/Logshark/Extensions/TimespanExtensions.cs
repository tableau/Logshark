using System;

namespace Logshark.Extensions
{
    public static class TimespanExtensions
    {
        public static string Print(this TimeSpan timeSpan)
        {
            string timespanFormatString = "";

            if (timeSpan.Hours != 0)
            {
                timespanFormatString += @"hh\:mm\:";
            } 
            else if (timeSpan.Minutes != 0)
            {
                timespanFormatString += @"mm\:";
            }

            timespanFormatString += @"ss\.ff";

            return timeSpan.ToString(timespanFormatString);
        }
    }
}
