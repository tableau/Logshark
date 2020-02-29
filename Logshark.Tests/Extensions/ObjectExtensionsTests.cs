using System.Text.RegularExpressions;
using FluentAssertions;
using LogShark.Extensions;
using Xunit;

namespace LogShark.Tests.Extensions
{
    public class ObjectExtensionsTests : InvariantCultureTestsBase
    {
        [Theory]
        [InlineData("abc", (string) null, true, false)]
        [InlineData("abc", 123, true, false)]
        [InlineData("abc", "", false, false)]
        [InlineData("abc", "def", false, false)]
        [InlineData("bc", "abc", false, true)]
        public void GetRegexMatch(string regexPattern, object input, bool isNull, bool isSuccess)
        {
            var regex = new Regex(regexPattern);
            var match = input.CastToStringAndRegexMatch(regex);
            if (isNull)
            {
                match.Should().BeNull();
            }
            else
            {
                match.Success.Should().Be(isSuccess);
            }
        }
    }
}