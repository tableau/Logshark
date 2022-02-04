using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LogShark.Plugins.TabadminController;
using LogShark.Shared;
using Moq;
using Xunit;

namespace LogShark.Tests.Plugins.TabadminControllerPlugin
{
    public class BuildTrackerTests
    {
        private readonly Mock<IProcessingNotificationsCollector> _processingNotificationsCollectorMock;
        private readonly IBuildTracker _buildTracker;
        
        private static readonly DateTime TestTimestamp = new DateTime(2020, 10, 20, 10, 15, 20);
        private const string TestBuild = "20204.20.1020.1234";

        public BuildTrackerTests()
        {
            _processingNotificationsCollectorMock = new Mock<IProcessingNotificationsCollector>();
            _buildTracker = new BuildTracker(_processingNotificationsCollectorMock.Object);
            _buildTracker.AddBuild(TestTimestamp, TestBuild);
        }

        [Fact]
        public void GetBuildRecordsTest()
        {
            var result = _buildTracker.GetBuildRecords();
            result.Should().BeEquivalentTo(new object[]
            {
                new { Build = TestBuild, RoundedTimestamp = new DateTimeOffset(TestTimestamp.Year, TestTimestamp.Month, TestTimestamp.Day, TestTimestamp.Hour, TestTimestamp.Minute - 1, 0, TimeSpan.Zero)},
                new { Build = TestBuild, RoundedTimestamp = new DateTimeOffset(TestTimestamp.Year, TestTimestamp.Month, TestTimestamp.Day, TestTimestamp.Hour, TestTimestamp.Minute + 1, 0, TimeSpan.Zero)},
                new { Build = TestBuild, RoundedTimestamp = new DateTimeOffset(TestTimestamp.Year, TestTimestamp.Month, TestTimestamp.Day, TestTimestamp.Hour, TestTimestamp.Minute , 0, TimeSpan.Zero)},
            });
            _processingNotificationsCollectorMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void AddingOverlappingRecordsForTheSameBuild()
        {
            _buildTracker.AddBuild(TestTimestamp, TestBuild); // Same timestamp
            _buildTracker.GetBuildRecords().Count().Should().Be(3);
            
            _buildTracker.AddBuild(TestTimestamp.AddSeconds(1), TestBuild); // This should have no effect
            _buildTracker.GetBuildRecords().Count().Should().Be(3);
            
            _buildTracker.AddBuild(TestTimestamp.AddMinutes(1), TestBuild); // Adding a record that is 1 minute later. This should extend window by 1 minute ahead
            _buildTracker.GetBuildRecords().Count().Should().Be(4);
            
            // Adding a record that is far from the original record, so we should now have 2 disjoined time frames
            const string build2 = "someOtherBuild";
            var timestamp2 = TestTimestamp.AddMinutes(10); 
            _buildTracker.AddBuild(timestamp2, build2);
            _buildTracker.GetBuildRecords().Count().Should().Be(7);
            _buildTracker.GetBuildRecords().Should().BeEquivalentTo(new object[]
            {
                new { Build = TestBuild, RoundedTimestamp = new DateTimeOffset(TestTimestamp.Year, TestTimestamp.Month, TestTimestamp.Day, TestTimestamp.Hour, TestTimestamp.Minute - 1, 0, TimeSpan.Zero)},
                new { Build = TestBuild, RoundedTimestamp = new DateTimeOffset(TestTimestamp.Year, TestTimestamp.Month, TestTimestamp.Day, TestTimestamp.Hour, TestTimestamp.Minute + 1, 0, TimeSpan.Zero)},
                new { Build = TestBuild, RoundedTimestamp = new DateTimeOffset(TestTimestamp.Year, TestTimestamp.Month, TestTimestamp.Day, TestTimestamp.Hour, TestTimestamp.Minute , 0, TimeSpan.Zero)},
                new { Build = TestBuild, RoundedTimestamp = new DateTimeOffset(TestTimestamp.Year, TestTimestamp.Month, TestTimestamp.Day, TestTimestamp.Hour, TestTimestamp.Minute + 2, 0, TimeSpan.Zero)},
                new { Build = build2, RoundedTimestamp = new DateTimeOffset(TestTimestamp.Year, TestTimestamp.Month, TestTimestamp.Day, TestTimestamp.Hour, timestamp2.Minute - 1, 0, TimeSpan.Zero)},
                new { Build = build2, RoundedTimestamp = new DateTimeOffset(TestTimestamp.Year, TestTimestamp.Month, TestTimestamp.Day, TestTimestamp.Hour, timestamp2.Minute + 1, 0, TimeSpan.Zero)},
                new { Build = build2, RoundedTimestamp = new DateTimeOffset(TestTimestamp.Year, TestTimestamp.Month, TestTimestamp.Day, TestTimestamp.Hour, timestamp2.Minute , 0, TimeSpan.Zero)},
            });
            
            _processingNotificationsCollectorMock.VerifyNoOtherCalls();
        }
        
        [Theory]
        [MemberData(nameof(CollisionTestCases))]
        public void CollisionOnAdd(int numberOfOverlappingMinutes, DateTime timestampToUse)
        {
            const string otherBuild = "otherBuild";
            _buildTracker.AddBuild(timestampToUse, otherBuild);

            var result = _buildTracker.GetBuildRecords().ToList();
            result.Count.Should().Be(3 + (3 - numberOfOverlappingMinutes));
            result.Count(res => res.Build == TestBuild).Should().Be(3);
            result.Count(res => res.Build == otherBuild).Should().Be(3 - numberOfOverlappingMinutes);
            
            _processingNotificationsCollectorMock.Verify(m => m.ReportError(
                It.IsAny<string>(), nameof(BuildTracker)), 
                Times.Exactly(numberOfOverlappingMinutes));
            _processingNotificationsCollectorMock.VerifyNoOtherCalls();
        }

        public static IEnumerable<object[]> CollisionTestCases => new List<object[]>
        {
            new object[] { 1, TestTimestamp.AddMinutes(-2) },
            new object[] { 2, TestTimestamp.AddMinutes(-1) },
            new object[] { 3, TestTimestamp },
            new object[] { 2, TestTimestamp.AddMinutes(1) },
            new object[] { 1, TestTimestamp.AddMinutes(2) },
        };
    }
}