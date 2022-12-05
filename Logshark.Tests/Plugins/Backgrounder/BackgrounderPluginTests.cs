using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LogShark.Plugins.Backgrounder;
using LogShark.Plugins.Backgrounder.Model;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;
using LogShark.Tests.Plugins.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogShark.Tests.Plugins.Backgrounder
{
    public class BackgrounderPluginTests : InvariantCultureTestsBase
    {
        private static readonly LogFileInfo TestLogFileInfo = new LogFileInfo("backgrounder-1.log.2018-07-11", @"folder1/backgrounder-1.log.2018-07-11", "worker0", DateTime.MinValue);

        [Fact]
        public void BadOrNoOpInput()
        {
            var processingNotificationsCollector = new ProcessingNotificationsCollector(10);
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new BackgrounderPlugin())
            {
                plugin.Configure(testWriterFactory, null, processingNotificationsCollector, new NullLoggerFactory());

                var wrongContentFormat = new LogLine(new ReadLogLineResult(123, 456), TestLogFileInfo);
                var nullContent = new LogLine(new ReadLogLineResult(123, null), TestLogFileInfo);

                plugin.ProcessLogLine(wrongContentFormat, LogType.BackgrounderJava);
                plugin.ProcessLogLine(nullContent, LogType.BackgrounderJava);
            }

            testWriterFactory.AssertAllWritersAreDisposedAndEmpty(4);
            processingNotificationsCollector.TotalErrorsReported.Should().Be(2);
        }

        [Fact]
        public void BackgrounderPluginTest()
        {
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new BackgrounderPlugin())
            {
                plugin.Configure(testWriterFactory, null, null, new NullLoggerFactory());

                foreach (var testCase in _testCases)
                {
                    var logLine = testCase.GetLogLine();
                    plugin.ProcessLogLine(logLine, testCase.LogType);
                }

                plugin.CompleteProcessing();
            }

            var expectedOutput = _testCases
                .Where(@case => @case.ExpectedOutput != null)
                .Select(@case => @case.ExpectedOutput)
                .ToList();
            var jobWriter = testWriterFactory.GetOneWriterAndVerifyOthersAreEmptyAndDisposed<BackgrounderJob>("BackgrounderJobs", 4);
            jobWriter.ReceivedObjects.Should().BeEquivalentTo(expectedOutput);
        }

        // Only testing very basis events to make sure plugin functions as expected. More thorough testing is performed on EventParser and EventPersister classes
        private readonly List<PluginTestCase> _testCases = new List<PluginTestCase>
        {
            // Sample begin event
            new PluginTestCase
            {
                LogContents = "2018-08-08 11:17:13.491 +1000 (,,,,9,:purge_expired_wgsessions,-) scheduled-background-job-runner-1 backgrounder: INFO  com.tableausoftware.backgrounder.runner.BackgroundJobRunner - Running job of type PurgeExpiredWgSessions; no timeout; priority: 0; id: 9; args: null",
                LogType = LogType.BackgrounderJava,
                LogFileInfo = TestLogFileInfo,
                LineNumber = 123
            },
            // Sample end event to match previous one (so it is persisted right away)
            new PluginTestCase
            {
                LogContents = "2018-08-08 11:17:13.518 +1000 (,,,,9,:purge_expired_wgsessions,-) scheduled-background-job-runner-1 backgrounder: INFO  com.tableausoftware.backgrounder.runner.BackgroundJobRunner - Job finished: SUCCESS; name: Purge Expired WG Sessions; type :purge_expired_wgsessions; id: 9; notes: null; total time: 1 sec; run time: 0 sec",
                LogType = LogType.BackgrounderJava,
                LogFileInfo = TestLogFileInfo,
                LineNumber = 124,
                ExpectedOutput = new
                {
                    Args = (string) null,
                    BackgrounderId = "1",
                    EndFile = TestLogFileInfo.FileName,
                    EndLine = 124,
                    EndTime = new DateTime(2018, 8, 8, 11, 17, 13, 518),
                    ErrorMessage = (string) null,
                    JobId = "9",
                    JobType = "purge_expired_wgsessions",
                    Notes = (string) null,
                    Priority = 0,
                    RunTime = 0,
                    StartFile = TestLogFileInfo.FileName,
                    StartLine = 123,
                    StartTime = new DateTime(2018, 8, 8, 11, 17, 13, 491),
                    Success = true,
                    Timeout = (int?) null,
                    TotalTime = 1,
                    WorkerId = "worker0",
                }
            },
            // Begin event without matching end event. This is to make sure plugin calls "drain queue" method on persister.
            // This should turn into timeout, as it is not the last start event in the log file
            new PluginTestCase
            {
                LogContents = "2018-08-08 14:45:17.152 +1000 (,,,,326,:enqueue_data_alerts,-) scheduled-background-job-runner-1 backgrounder: INFO  com.tableausoftware.backgrounder.runner.BackgroundJobRunner - Running job of type EnqueueDataAlerts; no timeout; priority: 10; id: 326; args: null",
                LogType = LogType.BackgrounderJava,
                LogFileInfo = TestLogFileInfo,
                LineNumber = 125,
                ExpectedOutput = new
                {
                    Args = (string) null,
                    BackgrounderId = "1",
                    EndFile = (string) null,
                    EndLine = (int?) null,
                    EndTime = (DateTime?) null,
                    ErrorMessage = "There is no end event for this job in the logs, but a different job was processed later by the same backgrounder. This could be caused by a number of different problems: job was crashed, job was cancelled by other component, or log files are incomplete/corrupt",
                    JobId = "326",
                    JobType = "enqueue_data_alerts",
                    Notes = (string) null,
                    Priority = 10,
                    RunTime = (int?) null,
                    StartFile = TestLogFileInfo.FileName,
                    StartLine = 125,
                    StartTime = new DateTime(2018, 8, 8, 14, 45, 17, 152),
                    Success = false,
                    Timeout = (int?) null,
                    TotalTime = (int?) null,
                    WorkerId = "worker0",
                }
            },
            // Another begin event without matching end event. This is to make sure plugin calls "drain queue" method on persister
            // This should turn into "Unknown" result, as it is the last start event in the file
            new PluginTestCase
            {
                LogContents = "2018-08-08 14:46:17.152 +1000 (,,,,327,:enqueue_data_alerts,-) scheduled-background-job-runner-1 backgrounder: INFO  com.tableausoftware.backgrounder.runner.BackgroundJobRunner - Running job of type EnqueueDataAlerts; timeout: 9000; priority: 10; id: 327; args: null",
                LogType = LogType.BackgrounderJava,
                LogFileInfo = TestLogFileInfo,
                LineNumber = 126,
                ExpectedOutput = new
                {
                    Args = (string) null,
                    BackgrounderId = "1",
                    EndFile = (string) null,
                    EndLine = (int?) null,
                    EndTime = (DateTime?) null,
                    ErrorMessage = "Job end event seems to be outside of the time covered by logs",
                    JobId = "327",
                    JobType = "enqueue_data_alerts",
                    Notes = (string) null,
                    Priority = 10,
                    RunTime = (int?) null,
                    StartFile = TestLogFileInfo.FileName,
                    StartLine = 126,
                    StartTime = new DateTime(2018, 8, 8, 14, 46, 17, 152),
                    Success = (bool?) null,
                    Timeout = 9000,
                    TotalTime = (int?) null,
                    WorkerId = "worker0",
                }
            }
        };
    }
}
