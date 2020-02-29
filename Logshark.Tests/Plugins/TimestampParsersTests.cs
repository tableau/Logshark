using System;
using System.Collections.Generic;
using FluentAssertions;
using LogShark.Plugins.Shared;
using Xunit;

namespace LogShark.Tests.Plugins
{
    public class TimestampParsersTests : InvariantCultureTestsBase
    {
        [Fact]
        public void ApacheTimestamps()
        {
            foreach (var pair in _apacheTimestampTests)
            {
                var (input, expectedOutput) = pair;
                var output = TimestampParsers.ParseApacheLogsTimestamp(input);
                output.Should().Be(expectedOutput);
            }
        }
        
        [Fact]
        public void ApacheTimestampsEdgeCases()
        {
            TimestampParsers.ParseApacheLogsTimestamp(null).Should().Be(DateTime.MinValue);
            TimestampParsers.ParseApacheLogsTimestamp(string.Empty).Should().Be(DateTime.MinValue);
            TimestampParsers.ParseApacheLogsTimestamp(" ").Should().Be(DateTime.MinValue);
        }
        
        [Fact]
        public void JavaTimestamps()
        {
            foreach (var pair in _javaTimestampTests)
            {
                var (input, expectedOutput) = pair;
                var output = TimestampParsers.ParseJavaLogsTimestamp(input);
                output.Should().Be(expectedOutput);
            }
        }
        
        [Fact]
        public void JavaTimestampsEdgeCases()
        {
            TimestampParsers.ParseJavaLogsTimestamp(null).Should().Be(DateTime.MinValue);
            TimestampParsers.ParseJavaLogsTimestamp(string.Empty).Should().Be(DateTime.MinValue);
            TimestampParsers.ParseJavaLogsTimestamp(" ").Should().Be(DateTime.MinValue);
        }
        
        private readonly IDictionary<string, DateTime> _nativeJsonTimestampTests = new Dictionary<string,DateTime>
        {
            {"2015-05-18T10:30:26.429", new DateTime(2015, 05, 18, 10, 30, 26, 429) },
            {"2015-05-18T23:30:26.000", new DateTime(2015, 05, 18, 23, 30, 26, 0) },
            {"2015-01-31T00:00:00.000", new DateTime(2015, 01, 31, 00, 00, 0, 0) },
            {"2015-05-18T10:30:26", DateTime.MinValue }, // Missing milliseconds
            {"I am not a timestamp", DateTime.MinValue },
        };
        
        private readonly IDictionary<string, DateTime> _apacheTimestampTests = new Dictionary<string,DateTime>
        {
            // pre 2018.2
            {"2015-05-18 10:12:58.574", new DateTime(2015, 05, 18, 10, 12, 58, 574) },
            {"2015-05-18 10:12:58", DateTime.MinValue },
            
            // 2018.2 +
            {"2015-05-18T10:12:58.574", new DateTime(2015, 05, 18, 10, 12, 58, 574) },
            {"2015-05-18T10:12:58", DateTime.MinValue },
            
            // Other
            {"I am not a timestamp", DateTime.MinValue },
        };
        
        private readonly IDictionary<string, DateTime> _javaTimestampTests = new Dictionary<string,DateTime>
        {
            {"2015-05-18 10:12:58.574", new DateTime(2015, 05, 18, 10, 12, 58, 574) },
            {"2015-05-18 10:12:58.000", new DateTime(2015, 05, 18, 10, 12, 58, 0) },
            {"2015-05-18 10:12:58", DateTime.MinValue },
            
            // Other
            {"I am not a timestamp", DateTime.MinValue },
        };
    }
}