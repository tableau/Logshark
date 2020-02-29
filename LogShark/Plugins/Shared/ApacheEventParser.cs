using LogShark.Containers;
using LogShark.Extensions;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LogShark.Plugins.Shared
{
    public static class ApacheEventParser
    {
        private static readonly IList<Regex> _regexList = new List<Regex>
            {
                // 2020.1+ format
                new Regex(@"^
                            (?<request_ip>[^\s]+)\s
                            (?<requester>[^\s]+)\s
                            (?<remote_user>[^\s]+)\s
                            (?<ts>\d{4}-\d{2}-\d{2}[T\s]\d{2}:\d{2}:\d{2}\.\d{3})\s
                            ""?(?<ts_offset>[^""]*)""?\s
                            (?<port>\d{1,5})\s
                            ""(((?<request_method>[A-Z]+)\s)?(?<resource>.+?)(\sHTTP\/(?<http_version>.+?))?)?""\s
                            ""(?<x_forwarded_for>.+?)""\s
                            (?<status_code>\d{3})\s
                            (?<response_size>[^\s]+)\s
                            ""((?<content_length>-?[0-9]*?)|(.*?))""\s
                            (?<request_time>\d+)\s
                            (?<request_id>[^\s]+)\s
                            (?<tableau_error_source>[^\s]+)\s
                            (?<tableau_status_code>[\d-]+)\s
                            (?<tableau_error_code>[^\s]+)\s
                            (?<tableau_service_name>[^\s]+)\s*$",
                    RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled),
                // 2018.2+ format
                new Regex(@"^
                            (?<request_ip>[^\s]+)\s
                            (?<requester>[^\s]+)\s
                            (?<remote_user>[^\s]+)\s
                            (?<ts>\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3})\s
                            ""(?<ts_offset>[^""]*)""\s
                            (?<port>\d{1,5})\s
                            ""(((?<request_method>[A-Z]+)\s)?(?<resource>.+?)(\sHTTP\/(?<http_version>.+?))?)?""\s
                            ""(?<x_forwarded_for>.+?)""\s
                            (?<status_code>\d{3})\s
                            (?<response_size>[^\s]+)\s
                            ""((?<content_length>-?[0-9]*?)|(.*?))""\s
                            (?<request_time>\d+)\s
                            (?<request_id>[^\s]+)\s*$",
                    RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled),
                // 9.x - 2018.1 format
                new Regex(@"^
                            (?<request_ip>[^\s]+)\s
                            -\s
                            (?<requester>[^\s]+)\s
                            (?<ts>\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}\.\d{3})\s
                            (?<ts_offset>.*?)\s
                            (?<port>\d{1,5})\s
                            ""(((?<request_method>[A-Z]+)\s)?(?<resource>.+?)(\sHTTP\/(?<http_version>.+?))?)?""\s
                            ""(?<x_forwarded_for>.+?)""\s
                            (?<status_code>\d{3})\s
                            (?<response_size>[^\s]+)\s
                            ""((?<content_length>-?[0-9]*?)|(.*?))""\s
                            (?<request_time>\d+)\s
                            (?<request_id>[^\s]+)\s*$",
                    RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled),
            };

        public static Apache.ApacheEvent ParseEvent(LogLine logLine)
        {
            var logLineString = logLine.LineContents as string;
            var match = logLineString?.GetRegexMatchAndMoveCorrectRegexUpFront(_regexList);
            return match == null
                ? null
                : FormEvent(match, logLine);
        }

        private static Apache.ApacheEvent FormEvent(Match match, LogLine logLine)
        {
            return new Apache.ApacheEvent(
                logLine: logLine,
                timestamp: TimestampParsers.ParseApacheLogsTimestamp(match.GetString("ts")),
                contentLength: match.GetNullableLong("content_length"),
                port: match.GetNullableInt("port"),
                requestBody: match.GetString("resource"),
                requester: match.GetString("requester"),
                requestId: match.GetString("request_id"),
                requestIp: match.GetString("request_ip"),
                requestMethod: match.GetString("request_method"),
                requestTimeMs: match.GetNullableLong("request_time"),
                statusCode: match.GetNullableInt("status_code"),
                timestampOffset: match.GetString("ts_offset"),
                xForwardedFor: match.GetString("x_forwarded_for"),
                tableauErrorSource: match.GetString("tableau_error_source"),
                tableauStatusCode: match.GetNullableInt("tableau_status_code"),
                tableauErrorCode: match.GetString("tableau_error_code"),
                tableauServiceName: match.GetString("tableau_service_name")
            );
        }
    }

}