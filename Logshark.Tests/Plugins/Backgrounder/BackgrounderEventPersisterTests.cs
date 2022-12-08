using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LogShark.Plugins.Backgrounder;
using LogShark.Plugins.Backgrounder.Model;
using LogShark.Tests.Plugins.Helpers;
using Xunit;

namespace LogShark.Tests.Plugins.Backgrounder
{
    public class BackgrounderEventPersisterTests : InvariantCultureTestsBase
    {
        private const string TestFileName = "testfile.log";

        private readonly TestWriterFactory _writerFactory;
        private readonly IBackgrounderEventPersister _persister;

        public BackgrounderEventPersisterTests()
        {
            _writerFactory = new TestWriterFactory();
            _persister = new BackgrounderEventPersister(_writerFactory);
        }

        [Fact]
        public void ErrorEvent()
        {
            _persister.AddErrorEvent(_errorEvent);
            _persister.Dispose();
            var writer = _writerFactory.GetOneWriterAndVerifyOthersAreEmptyAndDisposed<BackgrounderJobError>("BackgrounderJobErrors", 4);
            writer.ReceivedObjects.Count.Should().Be(1);
            writer.ReceivedObjects[0].Should().BeEquivalentTo(_errorEvent);
        }

        [Fact]
        public void WatermarkTest()
        {
            var startEvent1 = GetStartEvent("1");
            var startEvent2 = GetStartEvent("2");
            startEvent2.StartTime = startEvent2.StartTime.Add(TimeSpan.FromSeconds(10));

            _persister.AddStartEvent(startEvent1);
            _persister.AddStartEvent(startEvent2);
            _persister.DrainEvents();

            var jobWriter = _writerFactory.GetOneWriterAndVerifyOthersAreEmpty<BackgrounderJob>("BackgrounderJobs", 4);
            jobWriter.ReceivedObjects.Count.Should().Be(2);
            startEvent1.MarkAsInvalidEnd();
            jobWriter.ReceivedObjects[0].Should().BeEquivalentTo(startEvent1);
            startEvent2.MarkAsUnknown();
            jobWriter.ReceivedObjects[1].Should().BeEquivalentTo(startEvent2);
        }

        [Fact]
        public void ExtractDetails()
        {
            var startEvent1 = GetStartEvent("1", "refresh_extracts", 600); // Extract refresh with details
            startEvent1.Args = "[TestyTest, blah, Datasource]";
            var startEvent2 = GetStartEvent("2", "refresh_extracts", 605); // Extract refresh without details
            var startEvent3 = GetStartEvent("3", "increment_extracts", 610); // Incremental refresh
            var startEvent4 = GetStartEvent("4", "some_other_job", 620); // Some other job type

            var endEvent1 = GetEndEvent("1");
            var endEvent2 = GetEndEvent("2");
            var endEvent3 = GetEndEvent("3");
            var endEvent4 = GetEndEvent("4");

            var detail1 = GetExtractJobDetail("1", 601);
            var detail2 = new BackgrounderExtractJobDetail()
            {
                BackgrounderJobId = "2",
                ResourceType = "testArgument"
            };
            var detail3 = GetExtractJobDetail("3", 611);
            var detail4 = GetExtractJobDetail("4", 621);

            _persister.AddStartEvent(startEvent1);
            _persister.AddStartEvent(startEvent2);
            _persister.AddStartEvent(startEvent3);
            _persister.AddStartEvent(startEvent4);
            _persister.AddExtractJobDetails(detail1);
            _persister.AddExtractJobDetails(detail3);
            _persister.AddExtractJobDetails(detail4);
            _persister.AddEndEvent(endEvent1);
            _persister.AddEndEvent(endEvent2);
            _persister.AddEndEvent(endEvent3);
            _persister.AddEndEvent(endEvent4);
            _persister.DrainEvents();
            var jobWriter = _writerFactory.GetWriterByName<BackgrounderJob>("BackgrounderJobs");
            jobWriter.ReceivedObjects.Count.Should().Be(4);

            var extractJobDetailWriter = _writerFactory.GetWriterByName<BackgrounderExtractJobDetail>("BackgrounderExtractJobDetails");
            extractJobDetailWriter.ReceivedObjects.Count.Should().Be(3);
            var actualDetail1 = extractJobDetailWriter.ReceivedObjects[0] as BackgrounderExtractJobDetail;
            var actualDetail2 = extractJobDetailWriter.ReceivedObjects[1] as BackgrounderExtractJobDetail;
            var actualDetail3 = extractJobDetailWriter.ReceivedObjects[2] as BackgrounderExtractJobDetail;
            detail1.ResourceName = "TestyTest";
            detail1.ResourceType = "Datasource";
            actualDetail1.Should().BeEquivalentTo(detail1);
            actualDetail2.Should().BeEquivalentTo(detail2);
            actualDetail3.Should().BeEquivalentTo(detail3);
        }

        [Fact]
        public void SubscriptionDetails()
        {
            var startEvent1 = GetStartEvent("1", "single_subscription_notify", 590); // Subscription job with details
            var startEvent2 = GetStartEvent("2", "single_subscription_notify", millisecond: 601); // Subscription job without details
            var startEvent3 = GetStartEvent("3", "some_other_job_type", millisecond: 610); // Non-subscription job with details

            var endEvent1 = GetEndEvent("1", millisecond: 600);
            var endEvent2 = GetEndEvent("2", millisecond: 605);
            var endEvent3 = GetEndEvent("3", millisecond: 615);

            var detail1 = GetSubscriptionDetails("1", millisecond: 591);
            var detail3 = GetSubscriptionDetails("3", millisecond: 611);

            _persister.AddStartEvent(startEvent1);
            _persister.AddStartEvent(startEvent2);
            _persister.AddStartEvent(startEvent3);
            foreach (var detail in detail1)
            {
                _persister.AddSubscriptionJobDetails(detail);
            }
            foreach (var detail in detail3)
            {
                _persister.AddSubscriptionJobDetails(detail);
            }
            _persister.AddEndEvent(endEvent1);
            _persister.AddEndEvent(endEvent2);
            _persister.AddEndEvent(endEvent3);
            _persister.DrainEvents();

            var jobWriter = _writerFactory.GetWriterByName<BackgrounderJob>("BackgrounderJobs");
            jobWriter.ReceivedObjects.Count.Should().Be(3);

            var subscriptionJobDetailWriter = _writerFactory.GetWriterByName<BackgrounderSubscriptionJobDetail>("BackgrounderSubscriptionJobDetails");
            subscriptionJobDetailWriter.ReceivedObjects.Count.Should().Be(1);
            var actualDetail1 = subscriptionJobDetailWriter.ReceivedObjects[0] as BackgrounderSubscriptionJobDetail;
            var twoCombined = CopyValues(detail1[0], detail1[1]);
            var expectedDetail = CopyValues(twoCombined, detail1[2]);
            actualDetail1.Should().BeEquivalentTo(expectedDetail);
        }

        [Fact]
        public void RequeueEvent()
        {
            var startEvent1 = GetStartEvent("1", millisecond: 599);
            _persister.AddStartEvent(startEvent1);

            var endEvent1 = GetEndEvent("1", millisecond: 600);
            _persister.AddEndEvent(endEvent1);

            var startEvent2 = GetStartEvent("1", millisecond: 601);
            _persister.AddStartEvent(startEvent2);

            var endEvent2 = GetEndEvent("1", millisecond: 602);
            _persister.AddEndEvent(endEvent2);

            var startEvent3 = GetStartEvent("1", millisecond: 603);
            _persister.AddStartEvent(startEvent3);

            var endEvent3 = GetEndEvent("1", millisecond: 604);
            _persister.AddEndEvent(endEvent3);

            var jobWriter = _writerFactory.GetOneWriterAndVerifyOthersAreEmpty<BackgrounderJob>("BackgrounderJobs", 4);
            jobWriter.ReceivedObjects.Count.Should().Be(0);

            _persister.DrainEvents();

            jobWriter.ReceivedObjects.Count.Should().Be(3);
        }

        [Fact]
        public void MultiRequeueEvent()
        {
            //events for job 1
            var startEvent1 = GetStartEvent("1", millisecond: 600);
            _persister.AddStartEvent(startEvent1);

            var subdetailsEvent1 = GetSubscriptionDetails("1", millisecond:601);
            _persister.AddSubscriptionJobDetails(subdetailsEvent1[0]);

            var extractEvent1 = GetExtractJobDetail("1", millisecond: 602);
            _persister.AddExtractJobDetails(extractEvent1);

            var endEvent1 = GetEndEvent("1", millisecond: 603);
            _persister.AddEndEvent(endEvent1);

            //events for job 2
            var startEvent2 = GetStartEvent("2", millisecond: 604);
            _persister.AddStartEvent(startEvent2);

            var endEvent2 = GetEndEvent("2", millisecond: 605);
            _persister.AddEndEvent(endEvent2);

            //requeue events for job 1
            var requeueStartEvent1 = GetStartEvent("1", millisecond: 606);
            _persister.AddStartEvent(requeueStartEvent1);

            var requeueSubdetailsEvent1 = GetSubscriptionDetails("1", millisecond: 607);
            _persister.AddSubscriptionJobDetails(requeueSubdetailsEvent1[0]);

            var requeueExtractEvent1 = GetExtractJobDetail("1", millisecond: 608);
            _persister.AddExtractJobDetails(requeueExtractEvent1);

            var requeueEndEvent1 = GetEndEvent("1", millisecond: 609);
            _persister.AddEndEvent(requeueEndEvent1);

            //another requeue events for job 1
            var anotherRequeueStartEvent11 = GetStartEvent("1", millisecond: 606);
            _persister.AddStartEvent(anotherRequeueStartEvent11);

            var anotherRequeueSubdetailsEvent1 = GetSubscriptionDetails("1", millisecond: 607);
            _persister.AddSubscriptionJobDetails(anotherRequeueSubdetailsEvent1[0]);

            var anotherRequeueExtractEvent1 = GetExtractJobDetail("1", millisecond: 608);
            _persister.AddExtractJobDetails(anotherRequeueExtractEvent1);

            var anotherRequeueEndEvent1 = GetEndEvent("1", millisecond: 609);
            _persister.AddEndEvent(anotherRequeueEndEvent1);

            var jobWriter = _writerFactory.GetOneWriterAndVerifyOthersAreEmpty<BackgrounderJob>("BackgrounderJobs", 4);
            jobWriter.ReceivedObjects.Count.Should().Be(0);

            _persister.DrainEvents();

            jobWriter.ReceivedObjects.Count.Should().Be(4);
        }

        [Fact]
        public void MultiRequeueEventDifferentOrder()
        {
            //Same as above, but different event order
            var endEvent2 = GetEndEvent("2", millisecond: 605);
            _persister.AddEndEvent(endEvent2);

            var requeueStartEvent1 = GetStartEvent("1", millisecond: 606);
            _persister.AddStartEvent(requeueStartEvent1);

            var requeueSubdetailsEvent1 = GetSubscriptionDetails("1", millisecond: 607);
            _persister.AddSubscriptionJobDetails(requeueSubdetailsEvent1[0]);

            var startEvent1 = GetStartEvent("1", millisecond: 600);
            _persister.AddStartEvent(startEvent1);

            var subdetailsEvent1 = GetSubscriptionDetails("1", millisecond: 601);
            _persister.AddSubscriptionJobDetails(subdetailsEvent1[0]);

            var anotherRequeueExtractEvent1 = GetExtractJobDetail("1", millisecond: 608);
            _persister.AddExtractJobDetails(anotherRequeueExtractEvent1);

            var anotherRequeueEndEvent1 = GetEndEvent("1", millisecond: 609);

            var startEvent2 = GetStartEvent("2", millisecond: 604);
            _persister.AddStartEvent(startEvent2);

            var requeueExtractEvent1 = GetExtractJobDetail("1", millisecond: 608);
            _persister.AddExtractJobDetails(requeueExtractEvent1);

            var requeueEndEvent1 = GetEndEvent("1", millisecond: 609);
            _persister.AddEndEvent(requeueEndEvent1);

            var extractEvent1 = GetExtractJobDetail("1", millisecond: 602);
            _persister.AddExtractJobDetails(extractEvent1);

            var endEvent1 = GetEndEvent("1", millisecond: 603);
            _persister.AddEndEvent(endEvent1);


            var anotherRequeueStartEvent11 = GetStartEvent("1", millisecond: 606);
            _persister.AddStartEvent(anotherRequeueStartEvent11);

            var anotherRequeueSubdetailsEvent1 = GetSubscriptionDetails("1", millisecond: 607);
            _persister.AddSubscriptionJobDetails(anotherRequeueSubdetailsEvent1[0]);
            _persister.AddEndEvent(anotherRequeueEndEvent1);

            var jobWriter = _writerFactory.GetOneWriterAndVerifyOthersAreEmpty<BackgrounderJob>("BackgrounderJobs", 4);
            jobWriter.ReceivedObjects.Count.Should().Be(0);

            _persister.DrainEvents();

            jobWriter.ReceivedObjects.Count.Should().Be(4);
        }

        private static BackgrounderJob GetStartEvent(string jobId, string jobType = "purge_expired_wgsessions", int millisecond = 491)
        {
            return new BackgrounderJob
            {
                Args = "testArgument",
                BackgrounderId = "1",
                JobId = jobId,
                JobType = jobType,
                Priority = 0,
                StartFile = TestFileName,
                StartLine = 123,
                StartTime = new DateTime(2018, 8, 8, 11, 17, 13, millisecond),
                Timeout = 9000,
                WorkerId = "worker0",
            };
        }

        private static BackgrounderJob GetEndEvent(string jobId, int millisecond = 402)
        {
            return new BackgrounderJob
            {
                EndFile = TestFileName,
                EndLine = 130,
                EndTime = new DateTime(2018, 8, 8, 11, 17, 13, millisecond),
                ErrorMessage = null,
                JobId = jobId,
                Notes = "test notes",
                RunTime = 3,
                Success = true,
                TotalTime = 5,
            };
        }

        private static BackgrounderExtractJobDetail GetExtractJobDetail(string jobId, int millisecond = 600)
        {
            return new BackgrounderExtractJobDetail
            {
                BackgrounderJobId = jobId,
                ExtractGuid = "5EEC2CCA-6F82-4EFF-9DBC-FDB471269B06",
                ExtractId = "bd5c5cc4-1c35-443f-bac7-3a4acac54a4b",
                ExtractSize = 1048641536L,
                ExtractUrl = "MDAPP2018_1_2",
                ResourceName = null,
                ResourceType = null,
                TotalSize = 1048713414L,
                TwbSize = 71878L,
                VizqlSessionId = "D7A2D1F664E5466B87C4637ABBC31D63",
                Timestamp = new DateTime(2018, 8, 8, 11, 17, 13, millisecond)
            };
        }

        private static List<BackgrounderSubscriptionJobDetail> GetSubscriptionDetails(string jobId, int millisecond = 599)
        {
            DateTime timestamp = new DateTime(2018, 8, 8, 11, 17, 13, millisecond);
            DateTime timestamp2 = new DateTime(2018, 8, 8, 11, 17, 13, millisecond + 1);
            DateTime timestamp3 = new DateTime(2018, 8, 8, 11, 17, 13, millisecond + 2);
            return new List<BackgrounderSubscriptionJobDetail>
            {
                new BackgrounderSubscriptionJobDetail
                {
                    BackgrounderJobId = jobId,
                    RecipientEmail = null,
                    SenderEmail = null,
                    SmtpServer = null,
                    SubscriptionName = null,
                    VizqlSessionId = "FA88A9BC626A40A29228ECE09F04A76B",
                    Timestamp = timestamp
                },

                new BackgrounderSubscriptionJobDetail
                {
                    BackgrounderJobId = jobId,
                    RecipientEmail = null,
                    SenderEmail = null,
                    SmtpServer = null,
                    SubscriptionName = "Weekly Report",
                    VizqlSessionId = null,
                    Timestamp = timestamp2
                },

                new BackgrounderSubscriptionJobDetail
                {
                    BackgrounderJobId = jobId,
                    RecipientEmail = "john.doe@test.com",
                    SenderEmail = "tableau@test.com",
                    SmtpServer = "mail.test.com",
                    SubscriptionName = null,
                    VizqlSessionId = null,
                    Timestamp = timestamp3
                }
            };
        }

        private static T CopyValues<T>(T target, T source)
        {
            var type = typeof(T);

            var properties = type.GetProperties().Where(prop => prop.CanRead && prop.CanWrite);

            foreach (var prop in properties)
            {
                var value = prop.GetValue(source, null);
                if (value != null)
                    prop.SetValue(target, value, null);
            }

            return target;
        }

        private readonly BackgrounderJobError _errorEvent = new BackgrounderJobError
        {
            BackgrounderJobId = "1369448",
            Class = "com.tableausoftware.core.configuration.ConfigurationSupportService",
            File = TestFileName,
            Line = 123,
            Message = "unable to convert site id string:  to integer for extract refresh time out overrides list skipping this site, will continue with the remainder.",
            Severity = "ERROR",
            Site = "Default",
            Thread = "pool-4-thread-1",
            Timestamp = new DateTime(2018, 7, 12, 23, 37, 17, 201)
        };
    }
}