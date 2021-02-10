using System;
using LogShark.Containers;

namespace LogShark.Extensions
{
    public static class JavaLineMatchResultExtensions
    {
        public static bool IsWarningPriorityOrHigher(this JavaLineMatchResult javaLineMatchResult)
        {
            if (string.IsNullOrWhiteSpace(javaLineMatchResult.Severity))
            {
                return false;
            }
            
            return javaLineMatchResult.Severity.Equals("Warn", StringComparison.InvariantCultureIgnoreCase) ||
                   javaLineMatchResult.Severity.Equals("Error", StringComparison.InvariantCultureIgnoreCase) ||
                   javaLineMatchResult.Severity.Equals("Fatal", StringComparison.InvariantCultureIgnoreCase);
        }
        
        public static bool IsErrorPriorityOrHigher(this JavaLineMatchResult javaLineMatchResult)
        {
            if (string.IsNullOrWhiteSpace(javaLineMatchResult.Severity))
            {
                return false;
            }
            
            return javaLineMatchResult.Severity.Equals("Error", StringComparison.InvariantCultureIgnoreCase) ||
                   javaLineMatchResult.Severity.Equals("Fatal", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}