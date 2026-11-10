using System.Collections.Generic;
using System.Threading.Tasks;
using AwesomeAssertions;
using LogShark.Shared.HostExtensions;
using Xunit;

namespace LogShark.Tests.HostExtensions
{
    public class NullArtifactPublisherTests
    {
        [Fact]
        public void IsDisabled()
        {
            new NullArtifactPublisher().IsEnabled.Should().BeFalse();
        }

        [Fact]
        public async Task PublishReturnsNullAndDoesNotTouchWorkbooks()
        {
            var workbooks = new List<ArtifactWorkbook>
            {
                new ArtifactWorkbook("Apache.twbx", "does/not/exist.twbx", true)
            };

            var result = await new NullArtifactPublisher().PublishLargeWorkbooks("project", workbooks);

            result.Should().BeNull();
        }
    }
}
