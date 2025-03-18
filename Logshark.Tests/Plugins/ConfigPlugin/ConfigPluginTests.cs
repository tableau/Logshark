using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LogShark.Containers;
using LogShark.Plugins.Config.Models;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;
using LogShark.Tests.Plugins.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogShark.Tests.Plugins.ConfigPlugin
{
    public class ConfigPluginTests : InvariantCultureTestsBase
    {
        private  static readonly LogFileInfo TestWorkgroupYmlInfo = new LogFileInfo("workgroup.yml", "config/workgroup.yml", "node1", DateTime.Now);
        private  static readonly LogFileInfo TestTabsvcYmlInfo = new LogFileInfo("tabsvc.yml", @"tabsvc.yml", "node1", DateTime.Now);

        [Fact]
        public void TestWithBothFiles()
        {
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new LogShark.Plugins.Config.ConfigPlugin())
            {
                plugin.Configure(testWriterFactory, null, null, new NullLoggerFactory());
                
                foreach (var testCase in _testCases)
                {
                    plugin.ProcessLogLine(testCase.GetLogLine(), testCase.LogType);
                }
                
                var pluginsExecutionResults = plugin.CompleteProcessing();
                pluginsExecutionResults.HasAdditionalTags.Should().BeFalse();
            }

            VerifyWritersState(testWriterFactory.Writers, _expectedConfigEntries, _expectedProcessInfoRecords);
        }

        [Fact]
        public void TestWithWorkgroupOnly()
        {
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new LogShark.Plugins.Config.ConfigPlugin())
            {
                plugin.Configure(testWriterFactory, null, null, new NullLoggerFactory());

                var filteredTestCases = _testCases.Where(testCase => testCase.LogType == LogType.WorkgroupYml);
                foreach (var testCase in filteredTestCases)
                {
                    plugin.ProcessLogLine(testCase.GetLogLine(), testCase.LogType);
                }
                
                plugin.CompleteProcessing();
            }

            var expectedConfigEntries = _expectedConfigEntries.Where(entry => entry.FileName == "workgroup.yml");
            VerifyWritersState(testWriterFactory.Writers, expectedConfigEntries, _expectedProcessInfoRecords );
        }

        [Fact]
        public void TestWithTabsvcOnly()
        {
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new LogShark.Plugins.Config.ConfigPlugin())
            {
                plugin.Configure(testWriterFactory, null, null, new NullLoggerFactory());

                var filteredTestCases = _testCases.Where(testCase => testCase.LogType == LogType.TabsvcYml);
                foreach (var testCase in filteredTestCases)
                {
                    plugin.ProcessLogLine(testCase.GetLogLine(), testCase.LogType);
                }
            }

            var expectedConfigEntries = _expectedConfigEntries.Where(entry => entry.FileName == "tabsvc.yml");
            VerifyWritersState(testWriterFactory.Writers, expectedConfigEntries, new List<ConfigProcessInfo>() );
            testWriterFactory.Writers.Count.Should().Be(2);
        }

        [Fact]
        public void BadInput()
        {
            var processingNotificationsCollector = new ProcessingNotificationsCollector(10);
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new LogShark.Plugins.Config.ConfigPlugin())
            {
                plugin.Configure(testWriterFactory, null, processingNotificationsCollector, new NullLoggerFactory());
                
                var wrongContentFormat = new LogLine(new ReadLogLineResult(123, "stringIsNotWhatConfigPluginExpects"), TestWorkgroupYmlInfo );
                var nullContent = new LogLine(new ReadLogLineResult(123, null), TestWorkgroupYmlInfo );
                
                plugin.ProcessLogLine(wrongContentFormat, LogType.WorkgroupYml);
                plugin.ProcessLogLine(nullContent, LogType.WorkgroupYml);
                
                plugin.CompleteProcessing();
            }

            VerifyWritersState(testWriterFactory.Writers, new List<ConfigEntry>(), new List<ConfigProcessInfo>());
            processingNotificationsCollector.TotalErrorsReported.Should().Be(2);
        }
        
        [Fact]
        public void TestVersionOutput()
        {
            var testWriterFactory = new TestWriterFactory();
            var processNotificationCollector = new ProcessingNotificationsCollector(10);
            using (var plugin = new LogShark.Plugins.Config.ConfigPlugin())
            {
                plugin.Configure(testWriterFactory, null, processNotificationCollector, new NullLoggerFactory());
                plugin.ProcessLogLine(_workgroupYamlWithVersionInfo.GetLogLine(), _workgroupYamlWithVersionInfo.LogType);
                var pluginsExecutionResults = plugin.CompleteProcessing();
                pluginsExecutionResults.HasAdditionalTags.Should().BeTrue();
                pluginsExecutionResults.AdditionalTags.Count.Should().Be(2);
                pluginsExecutionResults.AdditionalTags[0].Should().Be("9000.15.0427.2036");
                pluginsExecutionResults.AdditionalTags[1].Should().Be("9.0.1");
            }
        }

        private static void VerifyWritersState(IDictionary<DataSetInfo, object> writers, IEnumerable<dynamic> expectedConfigEntries, IEnumerable<dynamic> expectedProcessInfoRecords)
        {
            writers.Count.Should().Be(2);
            var configEntriesWriter = writers[new DataSetInfo("Config", "ConfigEntries")] as TestWriter<ConfigEntry>;
            var processTopologyWriter = writers[new DataSetInfo("Config", "ProcessTopology")] as TestWriter<ConfigProcessInfo>;

            configEntriesWriter.WasDisposed.Should().BeTrue();
            processTopologyWriter.WasDisposed.Should().BeTrue();
            
            configEntriesWriter.ReceivedObjects.Should().BeEquivalentTo(expectedConfigEntries);
            processTopologyWriter.ReceivedObjects.Should().BeEquivalentTo(expectedProcessInfoRecords);
        }

        private readonly IList<PluginTestCase> _testCases = new List<PluginTestCase>
        {
            // workgroup.yml
            new PluginTestCase {
                LineNumber = 0,
                LogFileInfo = TestWorkgroupYmlInfo, 
                LogType = LogType.WorkgroupYml,
                LogContents = new Dictionary<string, string>
                { 
                    { "lineWithoutDot" , "value1" },
                    { "line.WithDot", "value2" },
                    { "line.With.Dots", "value3" },
                    { "worker.hosts", "machine1, machine2" },
                    { "worker0.backgrounder.port", "8250" },
                    { "worker0.backgrounder.procs", "2" },
                    { "worker1.vizportal.port", "8600" },
                    { "worker1.vizportal.procs", "1" },
                    {"host","machine1" }
                }
            },

            // tabsvc.yml
            new PluginTestCase
            {
                LineNumber = 0,
                LogFileInfo = TestTabsvcYmlInfo,
                LogType = LogType.TabsvcYml,
                LogContents = new Dictionary<string, string>
                {
                    { "tabsvcLine1", "value1" },
                    { "worker0.backgrounder.port", "9999" }
                }
            }
        };
        
        private readonly PluginTestCase _workgroupYamlWithVersionInfo = new PluginTestCase
        {
            LineNumber = 0,
            LogFileInfo = TestWorkgroupYmlInfo,
            LogType = LogType.WorkgroupYml,
            LogContents = new Dictionary<string, string>
            {
                { "someOtherKey", "value1" },
                { "version", "123" },
                { "version.rstr", "9000.15.0427.2036" },
                { "version.external", "9.0.1" }
            }
        };
        
        private readonly ISet<dynamic> _expectedConfigEntries = new HashSet<dynamic>
        {
            ExpectedConfigEntry(TestWorkgroupYmlInfo, "lineWithoutDot", "lineWithoutDot", "value1"),
            ExpectedConfigEntry(TestWorkgroupYmlInfo, "line.WithDot", "line", "value2"),
            ExpectedConfigEntry(TestWorkgroupYmlInfo, "line.With.Dots", "line", "value3"),
            ExpectedConfigEntry(TestWorkgroupYmlInfo, "host","host", "machine1"),
            ExpectedConfigEntry(TestWorkgroupYmlInfo, "worker.hosts", "worker", "machine1, machine2"),
            ExpectedConfigEntry(TestWorkgroupYmlInfo, "worker0.backgrounder.port", "worker0", "8250"),
            ExpectedConfigEntry(TestWorkgroupYmlInfo, "worker0.backgrounder.procs", "worker0", "2"),
            ExpectedConfigEntry(TestWorkgroupYmlInfo, "worker1.vizportal.port", "worker1", "8600"),
            ExpectedConfigEntry(TestWorkgroupYmlInfo, "worker1.vizportal.procs", "worker1", "1"),
            

            ExpectedConfigEntry(TestTabsvcYmlInfo, "tabsvcLine1", "tabsvcLine1", "value1"),
            ExpectedConfigEntry(TestTabsvcYmlInfo, "worker0.backgrounder.port", "worker0", "9999"),
        };

        private readonly ISet<dynamic> _expectedProcessInfoRecords = new HashSet<dynamic>
        {
            ExpectedProcessInfoEntry("machine1", 8250, "backgrounder", "node1"),
            ExpectedProcessInfoEntry("machine1", 8251, "backgrounder", "node1"),
            ExpectedProcessInfoEntry("machine2", 8600, "vizportal", "worker1"),
        };

        private static dynamic ExpectedConfigEntry(LogFileInfo logFileInfo, string key, string rootKey, string value)
        {
            return new
            {
                FileLastModifiedUtc = logFileInfo.LastModifiedUtc,
                FileName = logFileInfo.FileName,
                FilePath = logFileInfo.FilePath,
                Key = key,
                RootKey = rootKey,
                Value = value,
                Worker = logFileInfo.Worker,
            };
        }

        public static dynamic ExpectedProcessInfoEntry(string hostname, int port, string process, string worker, DateTime? lastModified = null)
        {
            return new
            {
                FileLastModifiedUtc = lastModified ?? TestWorkgroupYmlInfo.LastModifiedUtc,
                Hostname = hostname,
                Port = port,
                Process = process,
                Worker = worker,
            };
        }
    }
}