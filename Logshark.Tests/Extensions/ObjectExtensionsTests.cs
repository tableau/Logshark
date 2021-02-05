using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using LogShark.Extensions;
using LogShark.Tests.Plugins.Helpers;
using Xunit;

namespace LogShark.Tests.Extensions
{
    public class ObjectExtensionsTests : InvariantCultureTestsBase
    {
        [Theory]
        [MemberData(nameof(TestCases))]
        public void MatchJavaLineSharedTests(string testName, string regexPattern, object input, IDictionary<string, object> expectedOutputProps)
        {
            var regex = new Regex(regexPattern);
            
            var resultForCommonLine = input.MatchJavaLine(regex);
            AssertMethods.AssertThatAllClassOwnPropsAreAtDefaultExpectFor(resultForCommonLine, expectedOutputProps, testName);
            
            var resultForExtendedLine = input.MatchJavaLineWithSessionInfo(regex);
            AssertMethods.AssertThatAllClassOwnPropsAreAtDefaultExpectFor(resultForExtendedLine, expectedOutputProps, testName);
        }
        
        [Fact]
        public void MatchJavaLineExtendedEventTest()
        {
            const string regexPattern = @"(?<ts>[^;]+); (?<pid>[^\s]+) (?<thread>[^\s]+) (?<class>[^\s]+) (?<sev>[^\s]+) (?<message>[^\s]+) " +
                                        @"(?<req>[^\s]+) (?<sess>[^\s]+) (?<site>[^\s]+) (?<user>[^\s]+)";

            const string input = "2020-09-28 17:44:18.273; 123 thread1 class1 info testMessage testRequest testSession testSite testUser";
            var expectedCommonOutput = new Dictionary<string, object>
            {
                {"Class", "class1"},
                {"Message", "testMessage"},
                {"ProcessId", 123},
                {"Severity", "info"},
                {"SuccessfulMatch", true},
                {"Thread", "thread1"},
                {"Timestamp", new DateTime(2020, 9, 28, 17, 44, 18, 273)},
            };
            var expectedExtendedOutput = new Dictionary<string, object>(expectedCommonOutput)
            {
                {"RequestId", "testRequest"},
                {"SessionId", "testSession"},
                {"Site", "testSite"},
                {"User", "testUser"}
            };

            var regex = new Regex(regexPattern);
            
            var resultForCommonLine = input.MatchJavaLine(regex);
            AssertMethods.AssertThatAllClassOwnPropsAreAtDefaultExpectFor(resultForCommonLine, expectedCommonOutput, "Base method with extra fields");
            
            var resultForExtendedLine = input.MatchJavaLineWithSessionInfo(regex);
            AssertMethods.AssertThatAllClassOwnPropsAreAtDefaultExpectFor(resultForExtendedLine, expectedExtendedOutput, "Extended method with extra fields");
        }

        public static IEnumerable<object[]> TestCases => new List<object[]>
        {
            new object[]
            {
                "Object is not string",
                @"abc",
                123,
                new Dictionary<string, object>
                {
                    { "Timestamp", DateTime.MinValue },
                    { "SuccessfulMatch", false },
                }
            },
            
            new object[]
            {
                "Input doesn't match regex",
                @"abc",
                "I am not gonna match",
                new Dictionary<string, object>
                {
                    { "Timestamp", DateTime.MinValue },
                    { "SuccessfulMatch", false },
                }
            },
            
            new object[]
            {
                "Regex only has some parameters",
                @"PID: (?<pid>\d+), Message: (?<message>.+)$",
                "PID: 123, Message: blah",
                new Dictionary<string, object>
                {
                    { "Message", "blah" },
                    { "ProcessId", 123 },
                    { "SuccessfulMatch", true },
                    { "Timestamp", DateTime.MinValue },
                }
            },
            
            new object[]
            {
                "Bad timestamp",
                @"Timestamp: (?<ts>.+)$",
                "Timestamp: I am not a timestamp!",
                new Dictionary<string, object>
                {
                    { "SuccessfulMatch", true },
                    { "Timestamp", DateTime.MinValue },
                }
            },
            
            new object[]
            {
                "All fields match (at least until someone adds more fields)",
                @"(?<ts>[^;]+); (?<pid>[^\s]+) (?<thread>[^\s]+) (?<class>[^\s]+) (?<sev>[^\s]+) (?<message>[^\s]+)",
                "2020-09-28 17:44:18.273; 123 thread1 class1 info testMessage",
                new Dictionary<string, object>
                {
                    { "Class", "class1" },
                    { "Message", "testMessage" },
                    { "ProcessId", 123 },
                    { "Severity", "info" },
                    { "SuccessfulMatch", true },
                    { "Thread", "thread1" },
                    { "Timestamp", new DateTime(2020, 9, 28, 17, 44, 18, 273) },
                }
            },
        };
    }
}