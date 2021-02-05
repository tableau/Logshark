using System;
using System.Collections.Generic;
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

        [Theory]
        [MemberData(nameof(GetBuildTestCases))]
        public void GetBuildTests(bool expectResult, DateTime dateTime)
        {
            var build = _buildTracker.GetBuild(dateTime);
            build.Should().Be(expectResult ? TestBuild : null);
            _processingNotificationsCollectorMock.VerifyNoOtherCalls();
        }

        public static IEnumerable<object[]> GetBuildTestCases => new List<object[]>
        {
            new object[] { false, new DateTime(2020, 10, 20 , 9, 15, 20) },
            new object[] { false, new DateTime(2020, 10, 20 , 10, 13, 59) },
            new object[] { true, new DateTime(2020, 10, 20 , 10, 14, 00) },
            new object[] { true, new DateTime(2020, 10, 20 , 10, 14, 01) },
            new object[] { true, new DateTime(2020, 10, 20 , 10, 14, 59) },
            new object[] { true, new DateTime(2020, 10, 20 , 10, 15, 00) },
            new object[] { true, new DateTime(2020, 10, 20 , 10, 15, 20) },
            new object[] { true, new DateTime(2020, 10, 20 , 10, 15, 59) },
            new object[] { true, new DateTime(2020, 10, 20 , 10, 16, 00) },
            new object[] { true, new DateTime(2020, 10, 20 , 10, 16, 59) },
            new object[] { false, new DateTime(2020, 10, 20 , 10, 17, 00) },
            new object[] { false, new DateTime(2020, 10, 20 , 10, 17, 20) },
            new object[] { false, new DateTime(2020, 10, 20 , 11, 15, 20) },
        };

        [Fact]
        public void AddingOverlappingRecordsForTheSameBuild()
        {
            _buildTracker.AddBuild(TestTimestamp, TestBuild); // Same timestamp
            _buildTracker.AddBuild(TestTimestamp.AddSeconds(1), TestBuild);

            _buildTracker.GetBuild(TestTimestamp).Should().Be(TestBuild);
            _buildTracker.GetBuild(TestTimestamp.AddMinutes(2)).Should().Be(null);
            
            // Subtest 1 - adding a record that is 1 minute later. This should extend window by 1 minute ahead
            _buildTracker.AddBuild(TestTimestamp.AddMinutes(1), TestBuild);

            void AssertSubtest1Results()
            {
                _buildTracker.GetBuild(TestTimestamp.Subtract(TimeSpan.FromMinutes(2))).Should().Be(null);
                _buildTracker.GetBuild(TestTimestamp.Subtract(TimeSpan.FromMinutes(1))).Should().Be(TestBuild);
                _buildTracker.GetBuild(TestTimestamp).Should().Be(TestBuild);
                _buildTracker.GetBuild(TestTimestamp.AddMinutes(1)).Should().Be(TestBuild);
                _buildTracker.GetBuild(TestTimestamp.AddMinutes(2)).Should().Be(TestBuild);
                _buildTracker.GetBuild(TestTimestamp.AddMinutes(3)).Should().Be(null);
            }
            AssertSubtest1Results();

            // Subtest 2 - adding a record that is far from the original record, so we should now have 2 disjoined time frames
            const string build2 = "someOtherBuild";
            var timestamp2 = TestTimestamp.AddMinutes(10);
            _buildTracker.AddBuild(timestamp2, build2);
            AssertSubtest1Results();
            _buildTracker.GetBuild(timestamp2.AddMinutes(-2)).Should().Be(null);
            _buildTracker.GetBuild(timestamp2.AddMinutes(-1)).Should().Be(build2);
            _buildTracker.GetBuild(timestamp2).Should().Be(build2);
            _buildTracker.GetBuild(timestamp2.AddMinutes(1)).Should().Be(build2);
            _buildTracker.GetBuild(timestamp2.AddMinutes(2)).Should().Be(null);
        }

        [Theory]
        [MemberData(nameof(CollisionTestCases))]
        public void CollisionOnAdd(
            int numberOfOverlappingMinutes, 
            (bool NewTimestampMinusOne, bool NewTimestamp, bool NewTimestampPlusOne) expectNewBuildReturned,
            DateTime timestampToUse)
        {
            const string otherBuild = "otherBuild";
            _buildTracker.AddBuild(timestampToUse, otherBuild);

            _buildTracker.GetBuild(TestTimestamp.AddMinutes(-1)).Should().Be(TestBuild);
            _buildTracker.GetBuild(TestTimestamp).Should().Be(TestBuild);
            _buildTracker.GetBuild(TestTimestamp.AddMinutes(1)).Should().Be(TestBuild);
            
            _buildTracker.GetBuild(timestampToUse.AddMinutes(-1)).Should().Be(expectNewBuildReturned.NewTimestampMinusOne ? otherBuild : TestBuild);
            _buildTracker.GetBuild(timestampToUse).Should().Be(expectNewBuildReturned.NewTimestamp ? otherBuild : TestBuild);
            _buildTracker.GetBuild(timestampToUse.AddMinutes(1)).Should().Be(expectNewBuildReturned.NewTimestampPlusOne ? otherBuild : TestBuild);
            
            _processingNotificationsCollectorMock.Verify(m => m.ReportError(
                It.IsAny<string>(), nameof(BuildTracker)), 
                Times.Exactly(numberOfOverlappingMinutes));
            _processingNotificationsCollectorMock.VerifyNoOtherCalls();
        }

        public static IEnumerable<object[]> CollisionTestCases => new List<object[]>
        {
            new object[] { 1, (true, true, false), TestTimestamp.AddMinutes(-2) },
            new object[] { 2, (true, false, false), TestTimestamp.AddMinutes(-1) },
            new object[] { 3, (false, false, false), TestTimestamp },
            new object[] { 2, (false, false, true), TestTimestamp.AddMinutes(1) },
            new object[] { 1, (false, true, true), TestTimestamp.AddMinutes(2) },
        };
    }
}