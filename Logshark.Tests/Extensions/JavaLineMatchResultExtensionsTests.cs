using System;
using FluentAssertions;
using LogShark.Containers;
using LogShark.Extensions;
using Xunit;

namespace LogShark.Tests.Extensions
{
    public class JavaLineMatchResultExtensionsTests
    {
        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("  ", false)]
        [InlineData("something else", false)]
        [InlineData("info", false)]
        [InlineData("INFO", false)]
        [InlineData("WARN", true)]
        [InlineData("warn", true)]
        [InlineData("Warn", true)]
        [InlineData("ERROR", true)]
        [InlineData("error", true)]
        [InlineData("Error", true)]
        [InlineData("ErRoR", true)]
        [InlineData("Fatal", true)]
        [InlineData("FATAL", true)]
        public void IsWarningPriorityOrHigher(string severityToUse, bool expectedResult)
        {
            var javaLineMatchResult = new JavaLineMatchResult(true)
            {
                Severity = severityToUse
            };
            
            javaLineMatchResult.IsWarningPriorityOrHigher().Should().Be(expectedResult);
        }
        
        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("  ", false)]
        [InlineData("something else", false)]
        [InlineData("info", false)]
        [InlineData("INFO", false)]
        [InlineData("WARN", false)]
        [InlineData("warn", false)]
        [InlineData("Warn", false)]
        [InlineData("ERROR", true)]
        [InlineData("error", true)]
        [InlineData("Error", true)]
        [InlineData("ErRoR", true)]
        [InlineData("Fatal", true)]
        [InlineData("FATAL", true)]
        public void IsErrorPriorityOrHigher(string severityToUse, bool expectedResult)
        {
            var javaLineMatchResult = new JavaLineMatchResult(true)
            {
                Severity = severityToUse
            };
            javaLineMatchResult.IsErrorPriorityOrHigher().Should().Be(expectedResult);
        }
    }
}