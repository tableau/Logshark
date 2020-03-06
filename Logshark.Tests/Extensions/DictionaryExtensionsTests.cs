using System.Collections.Generic;
using FluentAssertions;
using LogShark.Extensions;
using Xunit;

namespace LogShark.Tests.Extensions
{
    public class DictionaryExtensionsTests : InvariantCultureTestsBase
    {
        private readonly IDictionary<string, string> _testDictionary = new Dictionary<string, string>
        {
            { "nullKey", null },
            { "emptyKey", string.Empty },
            { "stringKey", "stringValue" },
            { "intKey", "123" },
            { "longKey", "32147483647" }, // int limit is 2,147,483,647
            { "boolKey", "true" },
        };
        
        [Fact]
        public void GetStringValueOrNull()
        {
            _testDictionary.GetStringValueOrNull("nullKey").Should().Be(null);
            _testDictionary.GetStringValueOrNull("emptyKey").Should().Be(string.Empty);
            _testDictionary.GetStringValueOrNull("stringKey").Should().Be("stringValue");
            _testDictionary.GetStringValueOrNull("intKey").Should().Be("123");
            _testDictionary.GetStringValueOrNull("longKey").Should().Be("32147483647");
            _testDictionary.GetStringValueOrNull("boolKey").Should().Be("true");
        }
        
        [Fact]
        public void GetIntValueOrNull()
        {
            _testDictionary.GetIntValueOrNull("nullKey").Should().Be(null);
            _testDictionary.GetIntValueOrNull("emptyKey").Should().Be(null);
            _testDictionary.GetIntValueOrNull("stringKey").Should().Be(null);
            _testDictionary.GetIntValueOrNull("intKey").Should().Be(123);
            _testDictionary.GetIntValueOrNull("longKey").Should().Be(null);
            _testDictionary.GetIntValueOrNull("boolKey").Should().Be(null);
        }
        
        [Fact]
        public void GetLongValueOrNull()
        {
            _testDictionary.GetLongValueOrNull("nullKey").Should().Be(null);
            _testDictionary.GetLongValueOrNull("emptyKey").Should().Be(null);
            _testDictionary.GetLongValueOrNull("stringKey").Should().Be(null);
            _testDictionary.GetLongValueOrNull("intKey").Should().Be(123);
            _testDictionary.GetLongValueOrNull("longKey").Should().Be(32147483647L);
            _testDictionary.GetLongValueOrNull("boolKey").Should().Be(null);
        }
        
        [Fact]
        public void GetBoolValueOrNull()
        {
            _testDictionary.GetBoolValueOrNull("nullKey").Should().Be(null);
            _testDictionary.GetBoolValueOrNull("emptyKey").Should().Be(null);
            _testDictionary.GetBoolValueOrNull("stringKey").Should().Be(null);
            _testDictionary.GetBoolValueOrNull("intKey").Should().Be(null);
            _testDictionary.GetBoolValueOrNull("boolKey").Should().Be(true);
        }
    }
}