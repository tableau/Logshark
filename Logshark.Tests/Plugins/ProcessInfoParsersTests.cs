using FluentAssertions;
using LogShark.Plugins.Shared;
using Xunit;

namespace LogShark.Tests.Plugins
{
    public class ProcessInfoParsersTests : InvariantCultureTestsBase
    {
        [Theory]
        [InlineData("backgrounder_3-1_2017_12_19_03_24_29.txt", 1)]
        [InlineData("tabprotosrv_dataserver_1-0_1.txt", 0)]
        [InlineData("tabprotosrv_dataserver_1.txt", null)]
        public void ParseProcessIndex(string input, int? expected)
        {
            var result = ProcessInfoParser.ParseProcessIndex(input);
            result.Should().Be(expected);
        }
    }
}