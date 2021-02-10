using FluentAssertions;
using LogShark.Plugins.Tabadmin;
using LogShark.Plugins.Tabadmin.Model;
using LogShark.Tests.Plugins.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;
using Xunit;

namespace LogShark.Tests.Plugins
{
    public class TabadminPluginTests : InvariantCultureTestsBase
    {
        private static readonly LogFileInfo TestLogFileInfo = new LogFileInfo("tabadmin.log", @"logs/tabadmin.log", "worker0", DateTime.MinValue);

        [Fact]
        public void BadAndNoOpInput()
        {
            var processingNotificationsCollector = new ProcessingNotificationsCollector(10);
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new TabadminPlugin())
            {
                plugin.Configure(testWriterFactory, null, processingNotificationsCollector, new NullLoggerFactory());
                
                var wrongContentFormat = new LogLine(new ReadLogLineResult(123, 1234), TestLogFileInfo);
                var nullContent = new LogLine(new ReadLogLineResult(123, null), TestLogFileInfo);
                var wrongContent = new LogLine(new ReadLogLineResult(123, "Not a tabadmin line"), TestLogFileInfo);
                
                plugin.ProcessLogLine(wrongContentFormat, LogType.Apache);
                plugin.ProcessLogLine(nullContent, LogType.Apache);
                plugin.ProcessLogLine(wrongContent, LogType.Apache);
            }

            testWriterFactory.AssertAllWritersAreDisposedAndEmpty(3);
            processingNotificationsCollector.TotalErrorsReported.Should().Be(3);
        }
        
        [Fact]
        public void RunTestCases_TableauServerVersions()
        { 
            var versionTestCases = new List<PluginTestCase>
            {
                new PluginTestCase()
                {
                    LogContents = "2018-07-06 13:54:17.687 -0700_DEBUG_10.10.1.153:DEVSRV1_:_pid=18044_0x7a0e7ecd__user=__request=__ ====>> <script> 2018.1 (build: 20181.18.0615.1128): Starting at 2018-07-06 13:54:17.671 -0700 <<====",
                    LogFileInfo = TestLogFileInfo,
                    LineNumber = 1,
                    ExpectedOutput = new TableauServerVersion
                    {
                        EndDate = DateTime.Parse(@"2018-07-10 11:06:36.543"),
                        EndDateGmt = DateTime.Parse(@"2018-07-10 4:06:36.543"),
                        Id = "logs/tabadmin.log-1",
                        StartDate = DateTime.Parse(@"2018-07-06 13:54:17.687"),
                        StartDateGmt = DateTime.Parse(@"2018-07-06 6:54:17.687"),
                        TimestampOffset = "-0700",
                        Version = "2018.1",
                        VersionLong = "20181.18.0615.1128",
                        Worker = "worker0",
                    }
                },
                new PluginTestCase
                {
                    LogContents = "2018-07-10 11:06:36.543 -0700_DEBUG_10.10.1.153:DEVSRV1_:_pid=16924_0x43e9089__user=__request=__ ====>> <script> 10.3 (build: 10300.17.0524.0223): Starting at 2018-07-10 11:06:36.527 -0700 <<====",
                    LogFileInfo = TestLogFileInfo,
                    LineNumber = 2,
                    ExpectedOutput = new TableauServerVersion
                    {
                        EndDate = null,
                        EndDateGmt = null,
                        Id = "logs/tabadmin.log-2",
                        StartDate = DateTime.Parse(@"2018-07-10 11:06:36.543"),
                        StartDateGmt = DateTime.Parse(@"2018-07-10 04:06:36.543"),
                        TimestampOffset = "-0700",
                        Version = "10.3",
                        VersionLong = "10300.17.0524.0223",
                        Worker = "worker0",
                    }
                },
            };

            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new TabadminPlugin())
            {
                plugin.Configure(testWriterFactory, null, null, new NullLoggerFactory());

                foreach (var testCase in versionTestCases)
                {
                    var logLine = testCase.GetLogLine();
                    plugin.ProcessLogLine(logLine, LogType.Tabadmin);
                }

                plugin.CompleteProcessing();
            }

            var expectedOutput = versionTestCases.Select(testCase => testCase.ExpectedOutput).ToList();
            var tableauServerVersionWriter = testWriterFactory.Writers.Values.OfType<TestWriter<TableauServerVersion>>().First();

            testWriterFactory.Writers.Count.Should().Be(3);
            tableauServerVersionWriter.WasDisposed.Should().Be(true);
            tableauServerVersionWriter.ReceivedObjects.Should().BeEquivalentTo(expectedOutput);
        }

        [Fact]
        public void RunTestCases_TabadminActions()
        {
            var tabadminActionTestCases = new List<PluginTestCase>
            {
                new PluginTestCase()
                {
                    LogContents = @"2018-07-06 13:54:19.875 -0700_DEBUG_10.10.1.153:DEVSRV1_:_pid=18044_0x7a0e7ecd__user=__request=__ run as: <script> validate --skiptempIPv6 --output E:\Tableau Server\2018.1\temp\validation_results.txt",
                    LogFileInfo = TestLogFileInfo,
                    LineNumber = 3,
                    ExpectedOutput = new TabadminAction
                    {
                        Arguments = @"--skiptempIPv6 --output E:\Tableau Server\2018.1\temp\validation_results.txt",
                        Command = "validate",
                        File = "tabadmin.log",
                        FilePath = "logs/tabadmin.log",
                        Hostname = "DEVSRV1",
                        Id = "logs/tabadmin.log-3",
                        Line = 3,
                        Timestamp = DateTime.Parse(@"2018-07-06 13:54:19.875"),
                        TimestampGmt = DateTime.Parse(@"2018-07-06 06:54:19.875"),
                        TimestampOffset = "-0700",
                        Version = null,
                        VersionId = null,
                        VersionLong = null,
                        Worker = "worker0",
                    }
                },
            };

            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new TabadminPlugin())
            {
                plugin.Configure(testWriterFactory, null, null, new NullLoggerFactory());

                foreach (var testCase in tabadminActionTestCases)
                {
                    var logLine = testCase.GetLogLine();
                    plugin.ProcessLogLine(logLine, LogType.Tabadmin);
                }

                plugin.CompleteProcessing();
            }

            var expectedOutput = tabadminActionTestCases.Select(testCase => testCase.ExpectedOutput).ToList();
            var tabadminActionWriter = testWriterFactory.Writers.Values.OfType<TestWriter<TabadminAction>>().First();

            testWriterFactory.Writers.Count.Should().Be(3);
            tabadminActionWriter.WasDisposed.Should().Be(true);
            tabadminActionWriter.ReceivedObjects.Should().BeEquivalentTo(expectedOutput);
        }

        [Fact]
        public void RunTestCases_TabadminErrors()
        {
            var tabadminErrorTestCases = new List<PluginTestCase>
            {
                new PluginTestCase
                {
                    LogContents =
                        @"2018-07-10 11:28:33.510 -0700_FATAL_10.10.1.153:DEVSRV1_:_pid=12068_0x188ac8a3__user=__request=__ Error during restore: MultiCommand::ExternalCommandFailure 'Command '""E:/ Tableau Server / 10.3 / pgsql / bin / pg_restore.exe"" -h localhost -p 8060 -U tblwgadmin -C -d postgres -j 1 ""E:/ Tableau Server / workgroup.pg_dump""' failed with code 1, result was pg_restore: [archiver] unsupported version (1.13) in file header

'",
                    LogFileInfo = TestLogFileInfo,
                    LineNumber = 51335,
                    ExpectedOutput = new TabadminError
                    {
                        File = "tabadmin.log",
                        FilePath = "logs/tabadmin.log",
                        Hostname = "DEVSRV1",
                        Id = "logs/tabadmin.log-51335",
                        Line = 51335,
                        Message = @"Error during restore: MultiCommand::ExternalCommandFailure 'Command '""E:/ Tableau Server / 10.3 / pgsql / bin / pg_restore.exe"" -h localhost -p 8060 -U tblwgadmin -C -d postgres -j 1 ""E:/ Tableau Server / workgroup.pg_dump""' failed with code 1, result was pg_restore: [archiver] unsupported version (1.13) in file header

'",
                        Severity = "FATAL",
                        Timestamp = DateTime.Parse(@"2018-07-10 11:28:33.510"),
                        TimestampGmt = DateTime.Parse(@"2018-07-10 04:28:33.510"),
                        TimestampOffset = "-0700",
                        Version = null,
                        VersionId = null,
                        VersionLong = null,
                        Worker = "worker0",
                    }
                },
            };

            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new TabadminPlugin())
            {
                plugin.Configure(testWriterFactory, null, null, new NullLoggerFactory());

                foreach (var testCase in tabadminErrorTestCases)
                {
                    var logLine = testCase.GetLogLine();
                    plugin.ProcessLogLine(logLine, LogType.Tabadmin);
                }

                plugin.CompleteProcessing();
            }

            var expectedOutput = tabadminErrorTestCases.Select(testCase => testCase.ExpectedOutput).ToList();
            var tabadminActionWriter = testWriterFactory.Writers.Values.OfType<TestWriter<TabadminError>>().First();

            testWriterFactory.Writers.Count.Should().Be(3);
            tabadminActionWriter.WasDisposed.Should().Be(true);
            tabadminActionWriter.ReceivedObjects.Should().BeEquivalentTo(expectedOutput);
        }
    }
}
