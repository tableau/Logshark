using System;
using System.Collections.Generic;
using FluentAssertions;
using LogShark.Extensions;
using Newtonsoft.Json.Linq;
using System.Linq;
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
                IntAsString = "15",
                IntWithThousandsSeparatorAsString = "65,123",
                NullValue = (string) null
            };
            
            _testToken = JToken.FromObject(testObject);
        }

        [Fact]
        public void Tests()
        {
            _testToken.GetStringFromPath("StringValue").Should().Be("line1\nline2");
            _testToken.GetStringFromPath("IntValue").Should().Be("10");
            _testToken.GetStringFromPath("NullValue").Should().BeNull();

            _testToken.GetDoubleFromPath("StringValue").Should().BeNull();
            _testToken.GetDoubleFromPath("DoubleValue").Should().Be(12.34);
            _testToken.GetDoubleFromPath("IntValue").Should().Be(10);
            _testToken.GetDoubleFromPath("IntAsString").Should().Be(15);
            _testToken.GetDoubleFromPath("IntWithThousandsSeparatorAsString").Should().Be(65123);
            _testToken.GetDoubleFromPath("NullValue").Should().BeNull();
            
            _testToken.GetIntFromPath("StringValue").Should().BeNull();
            _testToken.GetIntFromPath("IntValue").Should().Be(10);
            _testToken.GetIntFromPath("IntAsString").Should().Be(15);
            _testToken.GetIntFromPath("IntWithThousandsSeparatorAsString").Should().BeNull();
            _testToken.GetIntFromPath("NullValue").Should().BeNull();

            _testToken.GetLongFromPath("StringValue").Should().BeNull();
            _testToken.GetLongFromPath("IntValue").Should().Be(10);
            _testToken.GetLongFromPath("IntAsString").Should().Be(15);
            _testToken.GetLongFromPath("IntWithThousandsSeparatorAsString").Should().BeNull();
            _testToken.GetLongFromPath("NullValue").Should().BeNull();
            
            _testToken.GetLongFromPathAnyNumberStyle("StringValue").Should().BeNull();
            _testToken.GetLongFromPathAnyNumberStyle("IntValue").Should().Be(10);
            _testToken.GetLongFromPathAnyNumberStyle("IntAsString").Should().Be(15);
            _testToken.GetLongFromPathAnyNumberStyle("IntWithThousandsSeparatorAsString").Should().Be(65123);
            _testToken.GetLongFromPathAnyNumberStyle("NullValue").Should().BeNull();
            
            Action nullMultiplicationTest = () =>
            {
                var multiplicationResult = _testToken.GetDoubleFromPath("NullValue") * 1000;
                multiplicationResult.Should().BeNull();
            };
            nullMultiplicationTest.Should().NotThrow();
        }
        
        [Fact]
        public void TestGetStringFromPathsEdgeCase()
        {
            var jToken = JToken.FromObject(GetStringFromPathsTestCases.ToList()[0][0]);
            jToken.GetStringFromPaths().Should().BeNull();
        }

        [Theory]
        [MemberData(nameof(GetStringFromPathsTestCases))]
        public void TestGetStringFromPaths(object testObject, string expected)
        {
            var jToken = JToken.FromObject(testObject);
            jToken.GetStringFromPaths("String1", "String2", "SomeOtherPath").Should().Be(expected);
        }
        
        public static IEnumerable<object[]> GetStringFromPathsTestCases => new List<object[]>
        {
            new object[] { new {}, null},
            new object[] { new { AnotherPath = "string"}, null},
            new object[] { new { String1 = "string1" }, "string1"},
            new object[] { new { String1 = (string) null }, null},
            new object[] { new { String1 = 3 }, "3"},
            new object[] { new { String2 = "string2" }, "string2"},
            new object[] { new { String1 = "string1", String2 = "string2" }, "string1"},
            new object[] { new { String2 = "string2", String1 = "string1" }, "string1"},
            new object[] { new { String1 = (string) null, String2 = "string2" }, "string2"},
            new object[] { new { String1 = (string) null, String2 = (string) null }, null},
            new object[] { new { NotAString1 = "string1", String2 = "string2" }, "string2"},
        };
        
        [Fact]
        public void TestGetDoubleFromPathsEdgeCase()
        {
            var jToken = JToken.FromObject(GetDoubleFromPathsTestCases.ToList()[0][0]);
            jToken.GetDoubleFromPaths().Should().BeNull();
        }

        [Theory]
        [MemberData(nameof(GetDoubleFromPathsTestCases))]
        public void TestGetDoubleFromPaths(object testObject, double? expected)
        {
            var jToken = JToken.FromObject(testObject);
            jToken.GetDoubleFromPaths("Double1", "Double2", "SomeOtherPath").Should().Be(expected);
        }
        
        public static IEnumerable<object[]> GetDoubleFromPathsTestCases => new List<object[]>
        {
            new object[] { new {}, null},
            new object[] { new { AnotherPath = 1}, null},
            new object[] { new { Double1 = 1 }, 1D},
            new object[] { new { Double1 = (double?) null }, null},
            new object[] { new { Double1 = "string" }, null},
            new object[] { new { Double1 = "1" }, 1D},
            new object[] { new { Double2 = 2 }, 2D},
            new object[] { new { Double1 = 1, Double2 = 2 }, 1D},
            new object[] { new { Double2 = 2, Double1 = 1 }, 1D},
            new object[] { new { Double1 = (double?) null, Double2 = 2 }, 2D},
            new object[] { new { Double1 = (double?) null, Double2 = (double?) null }, null},
            new object[] { new { NotADouble1 = 1, Double2 = 2 }, 2D},
        };
    }
}