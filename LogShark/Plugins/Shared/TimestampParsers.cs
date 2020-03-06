using System;
using System.Globalization;

namespace LogShark.Plugins.Shared
{
    public static class TimestampParsers
    {
        private const string NativeJsonLogsTimestampFormat = @"yyyy-MM-ddTHH:mm:ss.fff";
        private const string JavaLogsTimestampFormat = @"yyyy-MM-dd HH:mm:ss.fff";
        
        public static DateTime ParseApacheLogsTimestamp(string rawTimestamp)
        {
            var normalizedTimestamp = rawTimestamp?.Replace(' ', 'T');
            return TryToParseWithGivenFormat(normalizedTimestamp, NativeJsonLogsTimestampFormat);
        }

        public static DateTime ParseJavaLogsTimestamp(string rawTimestamp)
        {
            return TryToParseWithGivenFormat(rawTimestamp, JavaLogsTimestampFormat);
        }

        private static DateTime TryToParseWithGivenFormat(string rawTimestamp, string format)
        {
            var parseSuccessful = DateTime.TryParseExact(rawTimestamp, format, CultureInfo.InvariantCulture,DateTimeStyles.AssumeLocal, out var parsedTimestamp);

            return parseSuccessful
                ? parsedTimestamp
                : DateTime.MinValue;
        }
    }
}