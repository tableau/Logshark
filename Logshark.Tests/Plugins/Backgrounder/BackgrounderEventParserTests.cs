using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LogShark.Containers;
using LogShark.Plugins.Backgrounder;
using LogShark.Plugins.Backgrounder.Model;
using LogShark.Tests.Plugins.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LogShark.Tests.Plugins.Backgrounder
{
    public class BackgrounderEventParserTests : InvariantCultureTestsBase
    {
        private static readonly LogFileInfo TestLogFileInfo = new LogFileInfo("backgrounder-1.log.2018-07-11",
            @"folder1/backgrounder-1.log.2018-07-11", "worker0", DateTime.MinValue);

        private readonly Mock<IBackgrounderEventPersister> _persisterMock;

        public BackgrounderEventParserTests()
        {
            _persisterMock = new Mock<IBackgrounderEventPersister>();
        }

        [Fact]
        public void BadOrNoOpInput()
        {
            var processingNotificationsCollector = new ProcessingNotificationsCollector(10);
            var parser = new BackgrounderEventParser(_persisterMock.Object, processingNotificationsCollector);

            var logLine = _startTestCases[0].GetLogLine();
            parser.ParseAndPersistLine(logLine, null); // Bad input
            parser.ParseAndPersistLine(logLine, "I am not a backgrounder line!"); // Good input, but doesn't match regex
            parser.ParseAndPersistLine(logLine, "2018-08-08 11:17:13.491 +1000 (,,,,,:purge_expired_wgsessions,-) scheduled-background-job-runner-1 backgrounder: INFO  com.tableausoftware.backgrounder.runner.BackgroundJobRunner - Running job of type PurgeExpiredWgSessions; no timeout; priority: 0; id: 9; args: null"); // Good input, but not error nor has ID

            _persisterMock.VerifyNoOtherCalls();
            processingNotificationsCollector.TotalErrorsReported.Should().Be(2);
        }


        [Theory]
        //valid 2020.1 lines that should not have error messages
        [InlineData("2020-04-03 02:00:55.731 -0400 (,,,,3752759,:viz_recommendations_trainer,2680d163-b7cd-496c-a720-2264720faee8) pool-111-thread-1 backgrounder: INFO  com.tableausoftware.recommendations.service.VizRecommendationsDataValidator { method=validateViewsStats, site=1 } - Start validating viewsStats. 9 records.")]
        [InlineData("2020-04-03 03:01:02.166 -0400 (CareBI,,,,3752821,:refresh_extracts,25fb3ebd-8aa0-481b-8a9e-affb480d469d) ActiveMQ Task-1 backgrounder: INFO  org.apache.activemq.transport.failover.FailoverTransport - Successfully connected to ssl")]
        public void ValidLinesThatAreNotPersisted(string input)
        {
            var processingNotificationsCollector = new ProcessingNotificationsCollector(10);
            var parser = new BackgrounderEventParser(_persisterMock.Object, processingNotificationsCollector);

            var logLine = _startTestCases[0].GetLogLine();

            parser.ParseAndPersistLine(logLine, input);

            _persisterMock.VerifyNoOtherCalls();
            processingNotificationsCollector.TotalErrorsReported.Should().Be(0);
        }

        [Fact]
        public void ErrorEvents()
        {
            var output = new List<BackgrounderJobError>();
            _persisterMock.Setup(m => m.AddErrorEvent(It.IsAny<BackgrounderJobError>())).Callback<BackgrounderJobError>(job => output.Add(job));
            
            RunTestCasesAndAssertOutput(_errorTestCases, output, _persisterMock.Object);

            _persisterMock.Verify(m => m.AddErrorEvent(It.IsAny<BackgrounderJobError>()), Times.Exactly(_errorTestCases.Count));
            _persisterMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void StartEvents()
        {
            var output = new List<BackgrounderJob>();
            _persisterMock.Setup(m => m.AddStartEvent(It.IsAny<BackgrounderJob>())).Callback<BackgrounderJob>(job => output.Add(job));
            
            RunTestCasesAndAssertOutput(_startTestCases, output, _persisterMock.Object);

            _persisterMock.Verify(m => m.AddStartEvent(It.IsAny<BackgrounderJob>()), Times.Exactly(_startTestCases.Count));
            _persisterMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void EndEvents()
        {
            var output = new List<BackgrounderJob>();
            _persisterMock.Setup(m => m.AddEndEvent(It.IsAny<BackgrounderJob>())).Callback<BackgrounderJob>(job => output.Add(job));
            
            RunTestCasesAndAssertOutput(_endTestCases, output, _persisterMock.Object);

            _persisterMock.Verify(m => m.AddEndEvent(It.IsAny<BackgrounderJob>()), Times.Exactly(_endTestCases.Count));
            _persisterMock.VerifyNoOtherCalls();
        }
        
        [Fact]
        public void ExtractJobDetailsEvents()
        {
            var output = new List<BackgrounderExtractJobDetail>();
            _persisterMock.Setup(m => m.AddExtractJobDetails(It.IsAny<BackgrounderExtractJobDetail>())).Callback<BackgrounderExtractJobDetail>(job => output.Add(job));
            
            RunTestCasesAndAssertOutput(_extractJobDetailTestCases, output, _persisterMock.Object);

            _persisterMock.Verify(m => m.AddExtractJobDetails(It.IsAny<BackgrounderExtractJobDetail>()), Times.Exactly(_extractJobDetailTestCases.Count));
            _persisterMock.VerifyNoOtherCalls();
        }
        
        [Fact]
        public void SubscriptionJobDetailsEvents()
        {
            var output = new List<BackgrounderSubscriptionJobDetail>();
            _persisterMock.Setup(m => m.AddSubscriptionJobDetails(It.IsAny<BackgrounderSubscriptionJobDetail>())).Callback<BackgrounderSubscriptionJobDetail>(job => output.Add(job));
            
            RunTestCasesAndAssertOutput(_subscriptionJobDetailTestCases, output, _persisterMock.Object);

            _persisterMock.Verify(m => m.AddSubscriptionJobDetails(It.IsAny<BackgrounderSubscriptionJobDetail>()), Times.Exactly(_subscriptionJobDetailTestCases.Count));
            _persisterMock.VerifyNoOtherCalls();
        }

        private static void RunTestCasesAndAssertOutput<T>(List<PluginTestCase> testCases, List<T> outputList, IBackgrounderEventPersister persister)
        {
            var parser = new BackgrounderEventParser(persister, null);

            foreach (var testCase in testCases)
            {
                parser.ParseAndPersistLine(testCase.GetLogLine(), testCase.LogContents.ToString());
            }
            
            var expectedOutput = testCases.Select(testCase => testCase.ExpectedOutput).ToList();
            outputList.Should().BeEquivalentTo(expectedOutput);
        }

        private readonly List<PluginTestCase> _errorTestCases = new List<PluginTestCase>
        {
            new PluginTestCase
            {
                LogContents = "2018-07-12 23:37:17.201 -0700 (Default,,,,1369448,:refresh_extracts,-) pool-4-thread-1 backgrounder: ERROR com.tableausoftware.core.configuration.ConfigurationSupportService - unable to convert site id string:  to integer for extract refresh time out overrides list skipping this site, will continue with the remainder.",
                LogType = LogType.BackgrounderJava,
                LogFileInfo = TestLogFileInfo,
                LineNumber = 123,
                ExpectedOutput = new
                {
                    BackgrounderJobId = 1369448,
                    Class = "com.tableausoftware.core.configuration.ConfigurationSupportService",
                    File = TestLogFileInfo.FileName,
                    Line = 123,
                    Message = "unable to convert site id string:  to integer for extract refresh time out overrides list skipping this site, will continue with the remainder.",
                    Severity = "ERROR",
                    Site = "Default",
                    Thread = "pool-4-thread-1",
                    Timestamp = new DateTime(2018, 7, 12, 23, 37, 17, 201)
                }},
            
            new PluginTestCase
            {
                LogContents = "2018-07-12 23:37:17.201 -0700 (Default,,,,1369448,:refresh_extracts,-) pool-4-thread-1 backgrounder: FATAL com.tableausoftware.core.configuration.ConfigurationSupportService - unable to convert site id string:  to integer for extract refresh time out overrides list skipping this site, will continue with the remainder.",
                LogType = LogType.BackgrounderJava,
                LogFileInfo = TestLogFileInfo,
                LineNumber = 124,
                ExpectedOutput = new
                {
                    BackgrounderJobId = 1369448,
                    Class = "com.tableausoftware.core.configuration.ConfigurationSupportService",
                    File = TestLogFileInfo.FileName,
                    Line = 124,
                    Message = "unable to convert site id string:  to integer for extract refresh time out overrides list skipping this site, will continue with the remainder.",
                    Severity = "FATAL",
                    Site = "Default",
                    Thread = "pool-4-thread-1",
                    Timestamp = new DateTime(2018, 7, 12, 23, 37, 17, 201)
                }},
            
            new PluginTestCase
            {
                LogContents = "2018-07-12 23:37:17.201 -0700 (Default,,,,,:refresh_extracts,-) pool-4-thread-1 backgrounder: FATAL com.tableausoftware.core.configuration.ConfigurationSupportService - unable to convert site id string:  to integer for extract refresh time out overrides list skipping this site, will continue with the remainder.",
                LogType = LogType.BackgrounderJava,
                LogFileInfo = TestLogFileInfo,
                LineNumber = 125,
                ExpectedOutput = new
                {
                    BackgrounderJobId = (int?) null,
                    Class = "com.tableausoftware.core.configuration.ConfigurationSupportService",
                    File = TestLogFileInfo.FileName,
                    Line = 125,
                    Message = "unable to convert site id string:  to integer for extract refresh time out overrides list skipping this site, will continue with the remainder.",
                    Severity = "FATAL",
                    Site = "Default",
                    Thread = "pool-4-thread-1",
                    Timestamp = new DateTime(2018, 7, 12, 23, 37, 17, 201)
                }},
        };

        private readonly List<PluginTestCase> _startTestCases = new List<PluginTestCase>
        {
            new PluginTestCase
            {
                LogContents =
                    "2018-08-08 11:17:13.491 +1000 (,,,,9,:purge_expired_wgsessions,-) scheduled-background-job-runner-1 backgrounder: INFO  com.tableausoftware.backgrounder.runner.BackgroundJobRunner - Running job of type PurgeExpiredWgSessions; no timeout; priority: 0; id: 9; args: null",
                LogType = LogType.BackgrounderJava,
                LogFileInfo = TestLogFileInfo,
                LineNumber = 123,
                ExpectedOutput = new
                {
                    Args = (string) null,
                    BackgrounderId = 1,
                    EndFile = (string) null,
                    EndLine = (int?) null,
                    EndTime = (DateTime?) null,
                    ErrorMessage = (string) null,
                    JobId = 9,
                    JobType = "purge_expired_wgsessions",
                    Notes = (string) null,
                    Priority = 0,
                    RunTime = (int?) null,
                    StartFile = TestLogFileInfo.FileName,
                    StartLine = 123,
                    StartTime = new DateTime(2018, 8, 8, 11, 17, 13, 491),
                    Success = (bool?) null,
                    Timeout = (int?) null,
                    TotalTime = (int?) null,
                    WorkerId = "worker0",
                }
            },

            new PluginTestCase
            {
                LogContents =
                    "2018-08-08 14:45:17.152 +1000 (,,,,326,:enqueue_data_alerts,-) scheduled-background-job-runner-1 backgrounder: INFO  com.tableausoftware.backgrounder.runner.BackgroundJobRunner - Running job of type EnqueueDataAlerts; no timeout; priority: 10; id: 326; args: null",
                LogType = LogType.BackgrounderJava,
                LogFileInfo = TestLogFileInfo,
                LineNumber = 124,
                ExpectedOutput = new
                {
                    Args = (string) null,
                    BackgrounderId = 1,
                    EndFile = (string) null,
                    EndLine = (int?) null,
                    EndTime = (DateTime?) null,
                    ErrorMessage = (string) null,
                    JobId = 326,
                    JobType = "enqueue_data_alerts",
                    Notes = (string) null,
                    Priority = 10,
                    RunTime = (int?) null,
                    StartFile = TestLogFileInfo.FileName,
                    StartLine = 124,
                    StartTime = new DateTime(2018, 8, 8, 14, 45, 17, 152),
                    Success = (bool?) null,
                    Timeout = (int?) null,
                    TotalTime = (int?) null,
                    WorkerId = "worker0",
                }
            },

            new PluginTestCase
            {
                LogContents =
                    "2018-08-08 14:46:17.152 +1000 (,,,,327,:enqueue_data_alerts,-) scheduled-background-job-runner-1 backgrounder: INFO  com.tableausoftware.backgrounder.runner.BackgroundJobRunner - Running job of type EnqueueDataAlerts; timeout: 9000; priority: 10; id: 327; args: test1 test2",
                LogType = LogType.BackgrounderJava,
                LogFileInfo = TestLogFileInfo,
                LineNumber = 125,
                ExpectedOutput = new
                {
                    Args = "test1 test2",
                    BackgrounderId = 1,
                    EndFile = (string) null,
                    EndLine = (int?) null,
                    EndTime = (DateTime?) null,
                    ErrorMessage = (string) null,
                    JobId = 327,
                    JobType = "enqueue_data_alerts",
                    Notes = (string) null,
                    Priority = 10,
                    RunTime = (int?) null,
                    StartFile = TestLogFileInfo.FileName,
                    StartLine = 125,
                    StartTime = new DateTime(2018, 8, 8, 14, 46, 17, 152),
                    Success = (bool?) null,
                    Timeout = 9000,
                    TotalTime = (int?) null,
                    WorkerId = "worker0",
                }
            },

            //2020.1
            new PluginTestCase
            {
                LogContents =
                    "2020-04-05 23:00:59.601 -0500 (kpi,,,,1968280,:refresh_extracts,6ee0e44f-0e70-4d31-bb34-57a6e09a6d72) scheduled-background-job-runner-1 backgrounder: INFO  com.tableausoftware.backgrounder.runner.BackgroundJobRunner - activity=backgrounder-job-start job_id=1968280 job_type=RefreshExtracts request_id=6ee0e44f-0e70-4d31-bb34-57a6e09a6d72 args=\"[Workbook, 9, Test COSMOS, 243, null]\" site=kpi site_id=3 timeout=9000",
                LogType = LogType.BackgrounderJava,
                LogFileInfo = TestLogFileInfo,
                LineNumber = 125,
                ExpectedOutput = new
                {
                    Args = "Workbook, 9, Test COSMOS, 243, null",
                    BackgrounderId = 1,
                    EndFile = (string) null,
                    EndLine = (int?) null,
                    EndTime = (DateTime?) null,
                    ErrorMessage = (string) null,
                    JobId = 1968280,
                    JobType = "refresh_extracts",
                    Notes = (string) null,
                    Priority = 0,
                    RunTime = (int?) null,
                    StartFile = TestLogFileInfo.FileName,
                    StartLine = 125,
                    StartTime = new DateTime(2020, 4, 5, 23, 00, 59, 601),
                    Success = (bool?) null,
                    Timeout = 9000,
                    TotalTime = (int?) null,
                    WorkerId = "worker0",
                }
            }
        };

        private readonly List<PluginTestCase> _endTestCases = new List<PluginTestCase>
        {
            new PluginTestCase
            {
                LogContents =
                    "2018-08-08 11:17:13.402 +1000 (,,,,7,:reap_auto_saves,-) scheduled-background-job-runner-1 backgrounder: INFO  com.tableausoftware.backgrounder.runner.BackgroundJobRunner - Job finished: SUCCESS; name: Reap Auto Saves; type :reap_auto_saves; id: 7; notes: null; total time: 1 sec; run time: 0 sec",
                LogType = LogType.BackgrounderJava,
                LogFileInfo = TestLogFileInfo,
                LineNumber = 123,
                ExpectedOutput = new
                {
                    Args = (string) null,
                    BackgrounderId = (int?) null,
                    EndFile = TestLogFileInfo.FileName,
                    EndLine = 123,
                    EndTime = new DateTime(2018, 8, 8, 11, 17, 13, 402),
                    ErrorMessage = (string) null,
                    JobId = 7,
                    JobType = (string) null,
                    Notes = (string) null,
                    Priority = 0,
                    RunTime = 0,
                    StartFile = (string) null,
                    StartLine = 0,
                    StartTime = default(DateTime),
                    Success = true,
                    Timeout = (int?) null,
                    TotalTime = 1,
                    WorkerId = (string) null,
                }
            },

            new PluginTestCase
            {
                LogContents =
                    "2018-08-08 11:17:13.402 +1000 (,,,,7,:reap_auto_saves,-) scheduled-background-job-runner-1 backgrounder: INFO  com.tableausoftware.backgrounder.runner.BackgroundJobRunner - Job finished: SUCCESS; name: Reap Auto Saves; type :reap_auto_saves; id: 7; notes: test note here; total time: 1 sec; run time: 0 sec",
                LogType = LogType.BackgrounderJava,
                LogFileInfo = TestLogFileInfo,
                LineNumber = 124,
                ExpectedOutput = new
                {
                    Args = (string) null,
                    BackgrounderId = (int?) null,
                    EndFile = TestLogFileInfo.FileName,
                    EndLine = 124,
                    EndTime = new DateTime(2018, 8, 8, 11, 17, 13, 402),
                    ErrorMessage = (string) null,
                    JobId = 7,
                    JobType = (string) null,
                    Notes = "test note here",
                    Priority = 0,
                    RunTime = 0,
                    StartFile = (string) null,
                    StartLine = 0,
                    StartTime = default(DateTime),
                    Success = true,
                    Timeout = (int?) null,
                    TotalTime = 1,
                    WorkerId = (string) null,
                }
            },

            new PluginTestCase
            {
                LogContents =
                    "2018-08-08 11:16:32.386 +1000 (,,,,2,:sanitize_dataserver_workbooks,-) scheduled-background-job-runner-1 backgrounder: INFO  com.tableausoftware.backgrounder.runner.BackgroundJobRunner - Job finished: ERROR; name: Sanitize Data Server Workbooks; type :sanitize_dataserver_workbooks; id: 2; notes: null; total time: 598 sec; run time: 0 sec",
                LogType = LogType.BackgrounderJava,
                LogFileInfo = TestLogFileInfo,
                LineNumber = 125,
                ExpectedOutput = new
                {
                    Args = (string) null,
                    BackgrounderId = (int?) null,
                    EndFile = TestLogFileInfo.FileName,
                    EndLine = 125,
                    EndTime = new DateTime(2018, 8, 8, 11, 16, 32, 386),
                    ErrorMessage =
                        "Job finished: ERROR; name: Sanitize Data Server Workbooks; type :sanitize_dataserver_workbooks; id: 2; notes: null; total time: 598 sec; run time: 0 sec",
                    JobId = 2,
                    JobType = (string) null,
                    Notes = (string) null,
                    Priority = 0,
                    RunTime = (int?) null,
                    StartFile = (string) null,
                    StartLine = 0,
                    StartTime = default(DateTime),
                    Success = false,
                    Timeout = (int?) null,
                    TotalTime = (int?) null,
                    WorkerId = (string) null,
                }
            },

            new PluginTestCase
            {
                LogContents =
                    "2020-05-13 19:00:46.479 -0500 (,,,,5470253,:sos_reconcile,bded9cdd-acfd-4378-acf3-3f65760a6706) scheduled-background-job-runner-1 backgrounder: INFO  com.tableausoftware.backgrounder.runner.BackgroundJobRunner - Job finished: SUCCESS; name: Simple Object Storage Reconcile; type :sos_reconcile; id: 5470253; total time: 7 sec; run time: 0 sec",
                LogType = LogType.BackgrounderJava,
                LogFileInfo = TestLogFileInfo,
                LineNumber = 123,
                ExpectedOutput = new
                {
                    Args = (string) null,
                    BackgrounderId = (int?) null,
                    EndFile = TestLogFileInfo.FileName,
                    EndLine = 123,
                    EndTime = new DateTime(2020, 05, 13, 19, 00, 46, 479),
                    ErrorMessage = (string) null,
                    JobId = 5470253L,
                    JobType = (string) null,
                    Notes = (string) null,
                    Priority = 0,
                    RunTime = 0,
                    StartFile = (string) null,
                    StartLine = 0,
                    StartTime = default(DateTime),
                    Success = true,
                    Timeout = (int?) null,
                    TotalTime = 7,
                    WorkerId = (string) null,
                }
            },

        };
        
        private readonly List<PluginTestCase> _extractJobDetailTestCases = new List<PluginTestCase>
        {
            new PluginTestCase // old format
            {
                LogContents = "2018-07-13 02:05:24.969 -0700 (Default,,,D7A2D1F664E5466B87C4637ABBC31D63,1369448,:refresh_extracts,-) pool-4-thread-1 backgrounder: INFO  com.tableausoftware.model.workgroup.service.VqlSessionService - Storing to SOS: MDAPP2018_1_2/extract reducedDataId:bd5c5cc4-1c35-443f-bac7-3a4acac54a4b size:71878 (twb) + 1048641536 (guid={5EEC2CCA-6F82-4EFF-9DBC-FDB471269B06}) = 1048713414",
                LogType = LogType.BackgrounderJava,
                LogFileInfo = TestLogFileInfo,
                LineNumber = 123,
                ExpectedOutput = new
                {
                    BackgrounderJobId = 1369448,
                    ExtractGuid = "5EEC2CCA-6F82-4EFF-9DBC-FDB471269B06",
                    ExtractId =  "bd5c5cc4-1c35-443f-bac7-3a4acac54a4b",
                    ExtractSize = 1048641536L,
                    ExtractUrl = "MDAPP2018_1_2",
                    JobNotes = (string) null,
                    ResourceName = (string) null,
                    ResourceType = (string) null,
                    ScheduleName = (string) null,
                    Site = (string) null,
                    TotalSize = 1048713414L,
                    TwbSize = 71878L,
                    VizqlSessionId = "D7A2D1F664E5466B87C4637ABBC31D63",
                }},
            
            new PluginTestCase // new format
            {
                LogContents = "2019-08-09 21:50:17.641 +0000 (Default,,,,201,:refresh_extracts,ee6dd62e-f472-4252-a931-caf4dfb0009f) pool-12-thread-1 backgrounder: INFO  com.tableausoftware.model.workgroup.workers.RefreshExtractsWorker - |status=ExtractTimingSuccess|jobId=201|jobLuid=ee6dd62e-f472-4252-a931-caf4dfb0009f|siteName=\"Default\"|workbookName=\"Large1\"|refreshedAt=\"2019-08-09T21:50:17.638Z\"|sessionId=F7162DFF82CB48D386850188BD5B190A-1:1|scheduleName=\"Weekday early mornings\"|scheduleType=\"FullRefresh\"|jobName=\"Refresh Extracts\"|jobType=\"RefreshExtracts\"|totalTimeSeconds=48|runTimeSeconds=46|queuedTime=\"2019-08-09T21:49:29.076Z\"|startedTime=\"2019-08-09T21:49:31.262Z\"|endTime=\"2019-08-09T21:50:17.638Z\"|correlationId=65|priority=0|serialId=null|extractsSizeBytes=57016320|jobNotes=\"Finished refresh of extracts (new extract id:{78C1FCC2-E70E-4B25-BFFE-7B7F0096A4FE}) for Workbook 'Large1' \"",
                LogType = LogType.BackgrounderJava,
                LogFileInfo = TestLogFileInfo,
                LineNumber = 123,
                ExpectedOutput = new
                {
                    BackgrounderJobId = 201,
                    ExtractGuid = (string) null,
                    ExtractId =  "78C1FCC2-E70E-4B25-BFFE-7B7F0096A4FE",
                    ExtractSize = 57016320,
                    ExtractUrl = "Large1",
                    JobNotes = "Finished refresh of extracts (new extract id:{78C1FCC2-E70E-4B25-BFFE-7B7F0096A4FE}) for Workbook 'Large1' ",
                    ResourceName = (string) null,
                    ResourceType = (string) null,
                    ScheduleName = "Weekday early mornings",
                    Site = "Default",
                    TotalSize = (long?) null,
                    TwbSize = (long?) null,
                    VizqlSessionId = "F7162DFF82CB48D386850188BD5B190A-1:1",
                }},
            
            new PluginTestCase // new format for data source
            {
                LogContents = "2019-08-09 21:50:17.641 +0000 (Default,,,,201,:refresh_extracts,ee6dd62e-f472-4252-a931-caf4dfb0009f) pool-12-thread-1 backgrounder: INFO  com.tableausoftware.model.workgroup.workers.RefreshExtractsWorker - |status=ExtractTimingSuccess|jobId=201|jobLuid=ee6dd62e-f472-4252-a931-caf4dfb0009f|siteName=\"Sales\"|datasourceName=\"Sales data\"|refreshedAt=\"2019-08-09T21:50:17.638Z\"|sessionId=F7162DFF82CB48D386850188BD5B190A-1:1|scheduleName=\"Weekday early mornings\"|scheduleType=\"FullRefresh\"|jobName=\"Refresh Extracts\"|jobType=\"RefreshExtracts\"|totalTimeSeconds=48|runTimeSeconds=46|queuedTime=\"2019-08-09T21:49:29.076Z\"|startedTime=\"2019-08-09T21:49:31.262Z\"|endTime=\"2019-08-09T21:50:17.638Z\"|correlationId=65|priority=0|serialId=null|extractsSizeBytes=57016320|jobNotes=\"Finished refresh of extracts (new extract id:{1811744A-39A0-47AA-9234-594A7891DCBE}) for Data Source 'Sales data' \"",
                LogType = LogType.BackgrounderJava,
                LogFileInfo = TestLogFileInfo,
                LineNumber = 123,
                ExpectedOutput = new
                {
                    BackgrounderJobId = 201,
                    ExtractGuid = (string) null,
                    ExtractId =  "1811744A-39A0-47AA-9234-594A7891DCBE",
                    ExtractSize = 57016320,
                    ExtractUrl = "Sales data",
                    JobNotes = "Finished refresh of extracts (new extract id:{1811744A-39A0-47AA-9234-594A7891DCBE}) for Data Source 'Sales data' ",
                    ResourceName = (string) null,
                    ResourceType = (string) null,
                    ScheduleName = "Weekday early mornings",
                    Site = "Sales",
                    TotalSize = (long?) null,
                    TwbSize = (long?) null,
                    VizqlSessionId = "F7162DFF82CB48D386850188BD5B190A-1:1",
                }},

            new PluginTestCase // new format - partial event
            {
                LogContents = "2019-08-09 21:50:17.641 +0000 (Default,,,,201,:refresh_extracts,ee6dd62e-f472-4252-a931-caf4dfb0009f) pool-12-thread-1 backgrounder: INFO  com.tableausoftware.model.workgroup.workers.RefreshExtractsWorker - |status=ExtractTimingSuccess|jobId=201|jobLuid=ee6dd62e-f472-4252-a931-caf4dfb0009f|siteName=\"Default\"|workbookName=\"Large1\"",
                LogType = LogType.BackgrounderJava,
                LogFileInfo = TestLogFileInfo,
                LineNumber = 123,
                ExpectedOutput = new
                {
                    BackgrounderJobId = 201,
                    ExtractGuid = (string) null,
                    ExtractId =  (string) null,
                    ExtractSize = (long?) null,
                    ExtractUrl = "Large1",
                    JobNotes = (string) null,
                    ResourceName = (string) null,
                    ResourceType = (string) null,
                    ScheduleName = (string) null,
                    Site = "Default",
                    TotalSize = (long?) null,
                    TwbSize = (long?) null,
                    VizqlSessionId = (string) null
                }},
        };
        
        private readonly List<PluginTestCase> _subscriptionJobDetailTestCases = new List<PluginTestCase>
        {
            new PluginTestCase
            {
                LogContents = "2018-07-11 16:00:53.506 -0700 (Default,john.doe,,FA88A9BC626A40A29228ECE09F04A76B,1367091,:single_subscription_notify,-) pool-4-thread-1 backgrounder: INFO  com.tableausoftware.model.workgroup.service.VqlSessionService - Created session id:FA88A9BC626A40A29228ECE09F04A76B",
                LogType = LogType.BackgrounderJava,
                LogFileInfo = TestLogFileInfo,
                LineNumber = 123,
                ExpectedOutput = new
                {
                    BackgrounderJobId = 1367091,
                    RecipientEmail = (string) null,
                    SenderEmail = (string) null,
                    SmtpServer = (string) null,
                    SubscriptionName = (string) null,
                    VizqlSessionId = "FA88A9BC626A40A29228ECE09F04A76B",
                }},
            
            new PluginTestCase
            {
                LogContents = "2018-07-11 16:00:53.445 -0700 (Default,john.doe,,,1367091,:single_subscription_notify,-) pool-4-thread-1 backgrounder: INFO  com.tableausoftware.model.workgroup.service.subscriptions.SubscriptionRunner - Starting subscription Id 66 for User John.Smith \"Weekly Report\"",
                LogType = LogType.BackgrounderJava,
                LogFileInfo = TestLogFileInfo,
                LineNumber = 124,
                ExpectedOutput = new
                {
                    BackgrounderJobId = 1367091,
                    RecipientEmail = (string) null,
                    SenderEmail = (string) null,
                    SmtpServer = (string) null,
                    SubscriptionName = "Weekly Report",
                    VizqlSessionId = (string) null,
                }},

            new PluginTestCase
            {
                LogContents = "2019-12-03 08:24:37.802 +0100 (Default,TestUser,,,1993727,:single_subscription_notify,1865c919-2e6a-45c7-994e-f281a78de6fa) pool-5-thread-1 backgrounder: INFO  com.tableausoftware.domain.subscription.SubscriptionRunner - Starting Subscription Id 97 for User TestUser Created by TestUser with Subject test test (12BB)",
                LogType = LogType.BackgrounderJava,
                LogFileInfo = TestLogFileInfo,
                LineNumber = 124,
                ExpectedOutput = new
                {
                    BackgrounderJobId = 1993727,
                    RecipientEmail = (string) null,
                    SenderEmail = (string) null,
                    SmtpServer = (string) null,
                    SubscriptionName = "test test (12BB)",
                    VizqlSessionId = (string) null,
                }},

            new PluginTestCase
            {
                LogContents = "2018-07-11 16:00:53.445 -0700 (Default,john.doe,,,1367091,:single_subscription_notify,-) pool-4-thread-1 backgrounder: INFO  com.tableausoftware.model.workgroup.service.subscriptions.SubscriptionRunner - Starting Subscription Id 66 for User John.Smith Weekly Report",
                LogType = LogType.BackgrounderJava,
                LogFileInfo = TestLogFileInfo,
                LineNumber = 124,
                ExpectedOutput = new
                {
                    BackgrounderJobId = 1367091,
                    RecipientEmail = (string) null,
                    SenderEmail = (string) null,
                    SmtpServer = (string) null,
                    SubscriptionName = "Weekly Report",
                    VizqlSessionId = (string) null,
                }},

            new PluginTestCase
            {
                LogContents = "2019-11-24 08:16:44.452 +0800 (RBAC-AE,TestUser,,,1253164,:single_subscription_notify,-) pool-3-thread-1 backgrounder: INFO  com.tableausoftware.model.workgroup.service.subscriptions.SubscriptionRunner - Starting Subscription Id 34 for User TestUser Subject Pin \"Bent\" 1423",
                LogType = LogType.BackgrounderJava,
                LogFileInfo = TestLogFileInfo,
                LineNumber = 124,
                ExpectedOutput = new
                {
                    BackgrounderJobId = 1253164,
                    RecipientEmail = (string) null,
                    SenderEmail = (string) null,
                    SmtpServer = (string) null,
                    SubscriptionName = "Pin \"Bent\" 1423",
                    VizqlSessionId = (string) null,
                }},

            new PluginTestCase
            {
                LogContents = "2018-07-11 16:01:00.629 -0700 (Default,john.doe,,,1367091,:single_subscription_notify,-) pool-4-thread-1 backgrounder: INFO  com.tableausoftware.model.workgroup.util.EmailHelper - Sending email from tableau@test.com to john.doe@test.com from server mail.test.com",
                LogType = LogType.BackgrounderJava,
                LogFileInfo = TestLogFileInfo,
                LineNumber = 125,
                ExpectedOutput = new
                {
                    BackgrounderJobId = 1367091,
                    RecipientEmail = "john.doe@test.com",
                    SenderEmail = "tableau@test.com",
                    SmtpServer = "mail.test.com",
                    SubscriptionName = (string) null,
                    VizqlSessionId = (string) null,
                }},
            
            new PluginTestCase
            {
                LogContents = "2018-07-12 16:01:00.629 -0700 (Default,john.doe,,,1367091,:single_subscription_notify,-) pool-4-thread-1 backgrounder: INFO  com.tableausoftware.model.workgroup.util.EmailHelper - Sending email from  to null from server smtp.testmailserver.com",
                LogType = LogType.BackgrounderJava,
                LogFileInfo = TestLogFileInfo,
                LineNumber = 126,
                ExpectedOutput = new
                {
                    BackgrounderJobId = 1367091,
                    RecipientEmail = "null",
                    SenderEmail = string.Empty,
                    SmtpServer = "smtp.testmailserver.com",
                    SubscriptionName = (string) null,
                    VizqlSessionId = (string) null,
                }},
        };
    }
}