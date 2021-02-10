using System.Text.RegularExpressions;
using FluentAssertions;
using LogShark.Shared.Extensions;
using Xunit;

namespace LogShark.Tests.Extensions
{
    public class MatchExtensionsTests
    {
        [Theory]
        [InlineData("", false, "")]
        [InlineData("''", true, "")]
        [InlineData("'  '", true, "  ")]
        [InlineData("aabbcc", false, "")]
        [InlineData("'aabbcc'", true, "aabbcc")]
        [InlineData("'full match'", true, "full match")]
        public void GetString(string input, bool matchExpected, string expectedOutput)
        {
            var testRegex = new Regex(@"\'(?<test_group>[\w\s]*)\'");
            var testMatch = testRegex.Match(input);
            testMatch.Success.Should().Be(matchExpected);
            testMatch.GetString("test_group").Should().Be(expectedOutput);
            testMatch.GetString("some_other_group").Should().Be("");
        }
        
        [Theory]
        [InlineData("", false, null)]
        [InlineData("''", true, null)]
        [InlineData("'  '", true, "  ")]
        [InlineData("aabbcc", false, null)]
        [InlineData("'aabbcc'", true, "aabbcc")]
        [InlineData("'full match'", true, "full match")]
        public void GetNullableString(string input, bool matchExpected, string expectedOutput)
        {
            var testRegex = new Regex(@"\'(?<test_group>[\w\s]*)\'");
            var testMatch = testRegex.Match(input);
            testMatch.Success.Should().Be(matchExpected);
            testMatch.GetNullableString("test_group").Should().Be(expectedOutput);
            testMatch.GetNullableString("some_other_group").Should().BeNull();
        }
    }
}