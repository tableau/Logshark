using System;

namespace LogShark.Containers
{
    public class JavaLineMatchResult
    {
        public bool SuccessfulMatch { get; }
        
        // Common fields
        public string Class { get; set; }
        public string Message { get; set; }
        public int? ProcessId { get; set; }
        public string Severity { get; set; }
        public string Thread { get; set; }
        public DateTime Timestamp { get; set; }
        
        // "WithSessionInfo" fields
        public string RequestId { get; set; }
        public string SessionId { get; set; }
        public string Site { get; set; }
        public string User { get; set; }

        public JavaLineMatchResult(bool successfulMatch)
        {
            SuccessfulMatch = successfulMatch;
        }

        public static JavaLineMatchResult FailedMatch()
        {
            return new JavaLineMatchResult(false)
            {
                Timestamp = DateTime.MinValue
            };
        }
    }
}