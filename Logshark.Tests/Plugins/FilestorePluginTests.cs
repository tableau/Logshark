using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LogShark.Plugins.Filestore;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;
using LogShark.Tests.Plugins.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogShark.Tests.Plugins
{
    public class FilestorePluginTests : InvariantCultureTestsBase
    {
        private  static readonly LogFileInfo TestLogFileInfo = new LogFileInfo("test.log", @"folder1/test.log", "node1", DateTime.MinValue);

        [Fact]
        public void RunTestCases()
        {
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new FilestorePlugin())
            {
                plugin.Configure(testWriterFactory, null, null, new NullLoggerFactory());

                foreach (var testCase in _testCases)
                {
                    var logLine = testCase.GetLogLine();
                    plugin.ProcessLogLine(logLine, LogType.Filestore);
                }
            }

            var expectedOutput = _testCases.Select(testCase => testCase.ExpectedOutput).ToList();
            testWriterFactory.Writers.Count.Should().Be(1);
            
            var testWriter = testWriterFactory.Writers.Values.First() as TestWriter<FilestoreEvent>;
            testWriter.WasDisposed.Should().Be(true);
            testWriter.ReceivedObjects.Should().BeEquivalentTo(expectedOutput);
        }
        
        [Fact]
        public void BadInput()
        {
            var processingNotificationsCollector = new ProcessingNotificationsCollector(10);
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new FilestorePlugin())
            {
                plugin.Configure(testWriterFactory, null, processingNotificationsCollector, new NullLoggerFactory());
                
                var wrongContentFormat = new LogLine(new ReadLogLineResult(123, 1234), TestLogFileInfo);
                var nullContent = new LogLine(new ReadLogLineResult(123, null), TestLogFileInfo);
                
                plugin.ProcessLogLine(wrongContentFormat, LogType.Apache);
                plugin.ProcessLogLine(nullContent, LogType.Apache);
            }

            testWriterFactory.AssertAllWritersAreDisposedAndEmpty(1);
            processingNotificationsCollector.TotalErrorsReported.Should().Be(2);
        }

        private readonly IList<PluginTestCase> _testCases = new List<PluginTestCase>
        {
            new PluginTestCase { // Random line from test logs
                LogContents = "2015-05-19 14:22:23.585 +1000 missingFoldersFetcherScheduler-1   INFO  : com.tableausoftware.tdfs.filestore.FileReconciliationService - Reaped folderId '{28F887BD-D8CB-46D5-84F2-681B87647182}' of type 'extract'",
                LogFileInfo = TestLogFileInfo,
                LineNumber = 123,
                ExpectedOutput = new
                {
                    Class = "com.tableausoftware.tdfs.filestore.FileReconciliationService",
                    FileName = TestLogFileInfo.FileName,
                    FilePath = TestLogFileInfo.FilePath,
                    LineNumber = 123,
                    Message = "Reaped folderId '{28F887BD-D8CB-46D5-84F2-681B87647182}' of type 'extract'",
                    Severity = "INFO",
                    Timestamp = new DateTime(2015, 5, 19, 14, 22, 23, 585),
                    Worker = TestLogFileInfo.Worker,
                }},
            
            new PluginTestCase { // Random multi-line event from test logs
                LogContents = "2015-05-19 14:35:24.366 +1000 missingFoldersFetcherScheduler-1   WARN  : com.tableausoftware.tdfs.filestore.FileReconciliationService - Failed to reaped folderId '{3887C530-D556-4D55-B8DB-5D6FD5A7BC08}' of type 'extract'\\njava.io.IOException: Unable to delete file: C:\\ProgramData\\Tableau\\Tableau Server\\data\\tabsvc\\dataengine\\extract\\2f\\f3\\{3887C530-D556-4D55-B8DB-5D6FD5A7BC08}\\oracle_41432_358308935189.tde\\n	at org.apache.commons.io.FileUtils.forceDelete(FileUtils.java:1919)\\n	at org.apache.commons.io.FileUtils.cleanDirectory(FileUtils.java:1399)\\n	at org.apache.commons.io.FileUtils.deleteDirectory(FileUtils.java:1331)\\n	at org.apache.commons.io.FileUtils.forceDelete(FileUtils.java:1910)\\n	at org.apache.commons.io.FileUtils.cleanDirectory(FileUtils.java:1399)\\n	at org.apache.commons.io.FileUtils.deleteDirectory(FileUtils.java:1331)\\n	at org.apache.commons.io.FileUtils.forceDelete(FileUtils.java:1910)\\n	at org.apache.commons.io.FileUtils.cleanDirectory(FileUtils.java:1399)\\n	at org.apache.commons.io.FileUtils.deleteDirectory(FileUtils.java:1331)\\n	at org.apache.commons.io.FileUtils.forceDelete(FileUtils.java:1910)\\n	at com.tableausoftware.tdfs.common.FileStorePath.deleteUptoRoot(FileStorePath.java:129)\\n	at com.tableausoftware.tdfs.filestore.FileReconciliationService.deleteFolder(FileReconciliationService.java:318)\\n	at com.tableausoftware.tdfs.filestore.jobs.MissingFoldersFetcher.fetch(MissingFoldersFetcher.java:118)\\n	at com.tableausoftware.tdfs.filestore.jobs.MissingFoldersFetcher.run(MissingFoldersFetcher.java:73)\\n	at sun.reflect.GeneratedMethodAccessor7.invoke(Unknown Source)\\n	at sun.reflect.DelegatingMethodAccessorImpl.invoke(DelegatingMethodAccessorImpl.java:43)\\n	at java.lang.reflect.Method.invoke(Method.java:606)\\n	at org.springframework.scheduling.support.ScheduledMethodRunnable.run(ScheduledMethodRunnable.java:65)\\n	at org.springframework.scheduling.support.DelegatingErrorHandlingRunnable.run(DelegatingErrorHandlingRunnable.java:54)\\n	at java.util.concurrent.Executors$RunnableAdapter.call(Executors.java:471)\\n	at java.util.concurrent.FutureTask.runAndReset(FutureTask.java:304)\\n	at java.util.concurrent.ScheduledThreadPoolExecutor$ScheduledFutureTask.access$301(ScheduledThreadPoolExecutor.java:178)\\n	at java.util.concurrent.ScheduledThreadPoolExecutor$ScheduledFutureTask.run(ScheduledThreadPoolExecutor.java:293)\\n	at java.util.concurrent.ThreadPoolExecutor.runWorker(ThreadPoolExecutor.java:1145)\\n	at java.util.concurrent.ThreadPoolExecutor$Worker.run(ThreadPoolExecutor.java:615)\\n	at java.lang.Thread.run(Thread.java:745)",
                LogFileInfo = TestLogFileInfo,
                LineNumber = 124,
                ExpectedOutput = new
                {
                    Class = "com.tableausoftware.tdfs.filestore.FileReconciliationService",
                    FileName = TestLogFileInfo.FileName,
                    FilePath = TestLogFileInfo.FilePath,
                    LineNumber = 124,
                    Message = "Failed to reaped folderId '{3887C530-D556-4D55-B8DB-5D6FD5A7BC08}' of type 'extract'\\njava.io.IOException: Unable to delete file: C:\\ProgramData\\Tableau\\Tableau Server\\data\\tabsvc\\dataengine\\extract\\2f\\f3\\{3887C530-D556-4D55-B8DB-5D6FD5A7BC08}\\oracle_41432_358308935189.tde\\n	at org.apache.commons.io.FileUtils.forceDelete(FileUtils.java:1919)\\n	at org.apache.commons.io.FileUtils.cleanDirectory(FileUtils.java:1399)\\n	at org.apache.commons.io.FileUtils.deleteDirectory(FileUtils.java:1331)\\n	at org.apache.commons.io.FileUtils.forceDelete(FileUtils.java:1910)\\n	at org.apache.commons.io.FileUtils.cleanDirectory(FileUtils.java:1399)\\n	at org.apache.commons.io.FileUtils.deleteDirectory(FileUtils.java:1331)\\n	at org.apache.commons.io.FileUtils.forceDelete(FileUtils.java:1910)\\n	at org.apache.commons.io.FileUtils.cleanDirectory(FileUtils.java:1399)\\n	at org.apache.commons.io.FileUtils.deleteDirectory(FileUtils.java:1331)\\n	at org.apache.commons.io.FileUtils.forceDelete(FileUtils.java:1910)\\n	at com.tableausoftware.tdfs.common.FileStorePath.deleteUptoRoot(FileStorePath.java:129)\\n	at com.tableausoftware.tdfs.filestore.FileReconciliationService.deleteFolder(FileReconciliationService.java:318)\\n	at com.tableausoftware.tdfs.filestore.jobs.MissingFoldersFetcher.fetch(MissingFoldersFetcher.java:118)\\n	at com.tableausoftware.tdfs.filestore.jobs.MissingFoldersFetcher.run(MissingFoldersFetcher.java:73)\\n	at sun.reflect.GeneratedMethodAccessor7.invoke(Unknown Source)\\n	at sun.reflect.DelegatingMethodAccessorImpl.invoke(DelegatingMethodAccessorImpl.java:43)\\n	at java.lang.reflect.Method.invoke(Method.java:606)\\n	at org.springframework.scheduling.support.ScheduledMethodRunnable.run(ScheduledMethodRunnable.java:65)\\n	at org.springframework.scheduling.support.DelegatingErrorHandlingRunnable.run(DelegatingErrorHandlingRunnable.java:54)\\n	at java.util.concurrent.Executors$RunnableAdapter.call(Executors.java:471)\\n	at java.util.concurrent.FutureTask.runAndReset(FutureTask.java:304)\\n	at java.util.concurrent.ScheduledThreadPoolExecutor$ScheduledFutureTask.access$301(ScheduledThreadPoolExecutor.java:178)\\n	at java.util.concurrent.ScheduledThreadPoolExecutor$ScheduledFutureTask.run(ScheduledThreadPoolExecutor.java:293)\\n	at java.util.concurrent.ThreadPoolExecutor.runWorker(ThreadPoolExecutor.java:1145)\\n	at java.util.concurrent.ThreadPoolExecutor$Worker.run(ThreadPoolExecutor.java:615)\\n	at java.lang.Thread.run(Thread.java:745)",
                    Severity = "WARN",
                    Timestamp = new DateTime(2015, 5, 19, 14, 35, 24, 366),
                    Worker = TestLogFileInfo.Worker,
                }},
            
            new PluginTestCase { // Another random multi-line event from 2018.2 Windows logs
                LogContents = "2018-10-03 00:14:01.883 +0000 scheduledFoldersPropagatorScheduler-1   INFO  : com.tableausoftware.tdfs.filestore.jobs.ScheduledFoldersPropagator - Ran ScheduledFoldersPropagator job 894 times between now and last report.",
                LogFileInfo = TestLogFileInfo,
                LineNumber = 125,
                ExpectedOutput = new
                {
                    Class = "com.tableausoftware.tdfs.filestore.jobs.ScheduledFoldersPropagator",
                    FileName = TestLogFileInfo.FileName,
                    FilePath = TestLogFileInfo.FilePath,
                    LineNumber = 125,
                    Message = "Ran ScheduledFoldersPropagator job 894 times between now and last report.",
                    Severity = "INFO",
                    Timestamp = new DateTime(2018, 10, 3, 0, 14, 1, 883),
                    Worker = TestLogFileInfo.Worker,
                }},
        };
    }
}