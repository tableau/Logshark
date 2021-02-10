using FluentAssertions;
using LogShark.Shared.LogReading.Containers;
using Xunit;

namespace LogShark.Tests.LogParser
{
    public class FileIsZipWithLogsResultTests
    {
        [Fact]
        public void ConstructorTests()
        {
            const string errorMessage = "testError";

            var success = FileIsZipWithLogsResult.Success();
            success.ContainsLogFiles.Should().BeTrue();
            success.ErrorMessage.Should().BeNull();
            success.ValidZip.Should().BeTrue();

            var invalidZip = FileIsZipWithLogsResult.InvalidZip(errorMessage);
            invalidZip.ContainsLogFiles.Should().BeFalse();
            invalidZip.ErrorMessage.Should().Be(errorMessage);
            invalidZip.ValidZip.Should().BeFalse();

            var noLogs = FileIsZipWithLogsResult.NoLogsFound();
            noLogs.ContainsLogFiles.Should().BeFalse();
            noLogs.ErrorMessage.Should().BeNull();
            noLogs.ValidZip.Should().BeTrue();
        }
    }
}