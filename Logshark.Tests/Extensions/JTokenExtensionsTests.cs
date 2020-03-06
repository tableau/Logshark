using FluentAssertions;
using LogShark.Extensions;
using Newtonsoft.Json.Linq;
using System.Globalization;
using Xunit;

namespace LogShark.Tests.Extensions
{
    public class JTokenExtensionsTests : InvariantCultureTestsBase
    {
        private readonly JToken _testToken;

        public JTokenExtensionsTests()
        {
            var testObject = new
            {
                StringValue = "line1\nline2",
                IntValue = 10,
                DoubleValue = 12.34,
            };
            
            _testToken = JToken.FromObject(testObject);
        }

        [Fact]
        public void Tests()
        {
            _testToken.GetStringFromPath("StringValue").Should().Be("line1\nline2");
            _testToken.GetStringFromPath("IntValue").Should().Be("10");

            _testToken.GetDoubleFromPath("StringValue").Should().BeNull();
            _testToken.GetDoubleFromPath("DoubleValue").Should().Be(12.34);
            _testToken.GetDoubleFromPath("IntValue").Should().Be(10);

            _testToken.GetIntFromPath("StringValue").Should().BeNull();
            _testToken.GetIntFromPath("IntValue").Should().Be(10);
            
            _testToken.GetLongFromPath("StringValue").Should().BeNull();
            _testToken.GetLongFromPath("IntValue").Should().Be(10);
        }
    }
}