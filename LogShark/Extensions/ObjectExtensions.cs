using System.Text.RegularExpressions;
using LogShark.Containers;
using LogShark.Plugins.Shared;
using LogShark.Shared.Extensions;

namespace LogShark.Extensions
{
    public static class ObjectExtensions
    {
        public static JavaLineMatchResult MatchJavaLine(this object rawJavaLine, Regex regexToUse)
        {
            return MatchJavaLineAndPopulateCommonFields(rawJavaLine, regexToUse).MatchResult;
        }

        public static JavaLineMatchResult MatchJavaLineWithSessionInfo(this object rawJavaLine, Regex regexToUse)
        {
            var (matchResult, match) = MatchJavaLineAndPopulateCommonFields(rawJavaLine, regexToUse);

            if (match != null)
            {
                matchResult.RequestId = match.GetNullableString("req");
                matchResult.SessionId = match.GetNullableString("sess");
                matchResult.Site = match.GetNullableString("site");
                matchResult.User = match.GetNullableString("user");
            }

            return matchResult;
        }

        private static (JavaLineMatchResult MatchResult, Match Match) MatchJavaLineAndPopulateCommonFields(object rawJavaLine, Regex regexToUse)
        {
            var match = rawJavaLine?.CastToStringAndRegexMatch(regexToUse);
            if (match == null || !match.Success)
            {
                return (JavaLineMatchResult.FailedMatch(), match);
            }

            var timestamp = TimestampParsers.ParseJavaLogsTimestamp(match.GetString("ts"));
            var result = new JavaLineMatchResult(true)
            {
                Class = match.GetNullableString("class"),
                Message = match.GetNullableString("message"),
                ProcessId = match.GetNullableInt("pid"),
                Severity = match.GetNullableString("sev"),
                Thread = match.GetNullableString("thread"),
                Timestamp = timestamp
            };

            return (result, match);
        }
        
        private static Match CastToStringAndRegexMatch(this object stringAsObject, Regex regex)
        {
            return stringAsObject is string str
                ? regex.Match(str)
                : null;
        }
    }
}