using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using LogShark.Containers;
using LogShark.LogParser;
using LogShark.LogParser.Containers;
using LogShark.LogParser.LogReaders;
using LogShark.Plugins;
using LogShark.Tests.Plugins.Helpers;
using LogShark.Writers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LogShark.Tests.LogParser
{
    public class TableauLogProcessorTests : InvariantCultureTestsBase
    {
        private const string UnZippedTestSet = "TestData/TableauLogsReaderTest";
        private const string ZippedTestSet = "TestData/TableauLogsReaderTest.zip";
        
        private static readonly LogTypeInfo TestLogTypeInfo = new LogTypeInfo(LogType.Apache, (stream, _) => new SimpleLinePerLineReader(stream), new List<Regex>
        {
            new Regex(@"correctLog\d\.log", RegexOptions.Compiled),
            new Regex(@"correctMultiLineLog\.log", RegexOptions.Compiled)
        });

        private readonly Mock<ILogTypeDetails> _logTypeDetailsMock;
        private readonly (Mock<IPlugin> Mock, IList<LogLine> Output) _plugin1;
        private readonly (Mock<IPlugin> Mock, IList<LogLine> Output) _plugin2;
        private readonly Mock<IPluginManager> _pluginManager;
        private readonly TestWriterFactory _writerFactory;

        public TableauLogProcessorTests()
        {
            _logTypeDetailsMock = new Mock<ILogTypeDetails>();
            _logTypeDetailsMock
                .Setup(m => m.GetInfoForLogType(It.Is<LogType>(value => value == LogType.Apache)))
                .Returns(TestLogTypeInfo);

            _plugin1 = GetPluginMock();
            _plugin2 = GetPluginMock();
            _pluginManager = GetPluginManagerMock(new List<IPlugin> {_plugin1.Mock.Object, _plugin2.Mock.Object});
            
            _writerFactory = new TestWriterFactory();
        }

        [Theory]
        [InlineData(UnZippedTestSet)]
        [InlineData(ZippedTestSet)]
        public void RunTestWithTwoPluginsOnALogSet(string logSetPath)
        {
            var config = GetConfig(logSetPath);
            var logsProcessor = new TableauLogsProcessor(config, _pluginManager.Object, _writerFactory, _logTypeDetailsMock.Object, null, new NullLoggerFactory());
            
            var results = logsProcessor.ProcessLogSet();
            
            results.LogProcessingStatistics.Count.Should().Be(1);
            results.IsSuccessful.Should().BeTrue();
            results.ExitReason.Should().Be(ExitReason.CompletedSuccessfully);
            var processingStats = results.LogProcessingStatistics.First().Value;
            processingStats.FilesProcessed.Should().Be(4);
            processingStats.LinesProcessed.Should().Be(6);

            _plugin1.Output.Should().BeEquivalentTo(_plugin2.Output);           
            _plugin1.Output.Should().BeEquivalentTo(_expectedLogLines, options =>
             {
                 options.Using<DateTime>(ctx => ctx.Subject.Should().BeAfter(new DateTime(2019, 3, 18), "Test files were created on those dates")).WhenTypeIs<DateTime>();
                 return options;
             });
            
            _pluginManager.Verify(
                m => m.SendCompleteProcessingSignalToPlugins(false),
                Times.Once,
                "Log Processor must send this call to plugins, otherwise some plugins return incomplete data");
        }
        
        [Fact]
        public void NoLogsFound()
        {
            const string emptyDirName = "empty_dir";
            if (Directory.Exists(emptyDirName))
            {
                Directory.Delete(emptyDirName);
            }
            Directory.CreateDirectory(emptyDirName);
            
            var config = GetConfig(emptyDirName);
            var logsProcessor = new TableauLogsProcessor(config, _pluginManager.Object, _writerFactory, _logTypeDetailsMock.Object, null, new NullLoggerFactory());
            
            var results = logsProcessor.ProcessLogSet();
            
            results.LogProcessingStatistics.Count.Should().Be(1);
            results.IsSuccessful.Should().BeFalse();
            results.ExitReason.Should().Be(ExitReason.LogSetDoesNotContainRelevantLogs);
            results.ErrorMessage.Should().Contain("Did not find any Tableau log files associated with requested plugins");

            _pluginManager.Verify(
                m => m.SendCompleteProcessingSignalToPlugins(false),
                Times.Once,
                "Log Processor must send this call to plugins, otherwise some plugins return incomplete data");
        }
        
        [Fact]
        public void BadPluginConfig()
        {
            var badPluginNames = (IEnumerable<string>) new List<string> {"badPlugin1", "badPlugin2"};
            _pluginManager
                .Setup(m => m.IsValidPluginConfiguration(out badPluginNames))
                .Returns(false);
            var config = GetConfig(UnZippedTestSet);
            var logsProcessor = new TableauLogsProcessor(config, _pluginManager.Object, _writerFactory, _logTypeDetailsMock.Object, null, new NullLoggerFactory());
            
            var results = logsProcessor.ProcessLogSet();
            
            results.IsSuccessful.Should().BeFalse();
            results.ExitReason.Should().Be(ExitReason.IncorrectConfiguration);
            results.ErrorMessage.Should().Contain("The following plugins were requested but cannot be found");
            results.ErrorMessage.Should().Contain(((List<string>) badPluginNames)[0]);
            results.ErrorMessage.Should().Contain(((List<string>) badPluginNames)[1]);

            _pluginManager.Verify(
                m => m.SendCompleteProcessingSignalToPlugins(It.IsAny<bool>()),
                Times.Never);
            _pluginManager.Verify(
                m => m.CreatePlugins(It.IsAny<IWriterFactory>(), It.IsAny<IProcessingNotificationsCollector>()),
                Times.Never);
        }
        
        [Fact]
        public void OomException()
        {
            _plugin1.Mock
                .Setup(m => m.ProcessLogLine(It.IsAny<LogLine>(), It.IsAny<LogType>()))
                .Throws<OutOfMemoryException>();
            var config = GetConfig(UnZippedTestSet);
            var logsProcessor = new TableauLogsProcessor(config, _pluginManager.Object, _writerFactory, _logTypeDetailsMock.Object, null, new NullLoggerFactory());
            
            var results = logsProcessor.ProcessLogSet();
            
            results.IsSuccessful.Should().BeFalse();
            results.ExitReason.Should().Be(ExitReason.OutOfMemory);
            results.ErrorMessage.Should().Contain("Out of memory exception occurred");

            _pluginManager.Verify(
                m => m.SendCompleteProcessingSignalToPlugins(true),
                Times.Once,
                "Log Processor must send this call to plugins even after error occurs to prevent further errors in logs");
            _plugin1.Mock.Verify(
                m => m.ProcessLogLine(It.IsAny<LogLine>(), It.IsAny<LogType>()),
                Times.Once,
                "We should stop at the first exception, this method should only be called once");
            _plugin2.Mock.Verify(
                m => m.ProcessLogLine(It.IsAny<LogLine>(), It.IsAny<LogType>()),
                Times.Never,
                "We should stop at the first exception, so second plugin should never be called");
        }
        
        [Fact]
        public void GenericException()
        {
            _plugin1.Mock
                .Setup(m => m.ProcessLogLine(It.IsAny<LogLine>(), It.IsAny<LogType>()))
                .Throws<Exception>();
            var config = GetConfig(UnZippedTestSet);
            var logsProcessor = new TableauLogsProcessor(config, _pluginManager.Object, _writerFactory, _logTypeDetailsMock.Object, null, new NullLoggerFactory());
            
            var results = logsProcessor.ProcessLogSet();
            
            results.IsSuccessful.Should().BeFalse();
            results.ExitReason.Should().Be(ExitReason.UnclassifiedError);
            results.ErrorMessage.Should().Contain("Unhandled exception occurred while processing log type");

            _pluginManager.Verify(
                m => m.SendCompleteProcessingSignalToPlugins(true),
                Times.Once,
                "Log Processor must send this call to plugins even after error occurs to prevent further errors in logs");
            _plugin1.Mock.Verify(
                m => m.ProcessLogLine(It.IsAny<LogLine>(), It.IsAny<LogType>()),
                Times.Once,
                "We should stop at the first exception, this method should only be called once");
            _plugin2.Mock.Verify(
                m => m.ProcessLogLine(It.IsAny<LogLine>(), It.IsAny<LogType>()),
                Times.Never,
                "We should stop at the first exception, so second plugin should never be called");
        }
        
        private static (Mock<IPlugin>, IList<LogLine>) GetPluginMock()
        {
            var output = new List<LogLine>();
            var pluginMock = new Mock<IPlugin>();
            pluginMock
                .Setup(p => p.ConsumedLogTypes)
                .Returns(new List<LogType> { LogType.Apache });
            pluginMock
                .Setup(m => m.ProcessLogLine(It.IsAny<LogLine>(), It.Is<LogType>(type => type == LogType.Apache)))
                .Callback((LogLine ll, LogType _) => output.Add(ll));

            return (pluginMock, output);
        }

        private static Mock<IPluginManager> GetPluginManagerMock(IList<IPlugin> plugins)
        {
            var isValidPluginConfigResult = (IEnumerable<string>) new List<string>();
            var pluginManagerMock = new Mock<IPluginManager>();
            pluginManagerMock
                .Setup(m => m.IsValidPluginConfiguration(out isValidPluginConfigResult))
                .Returns(true);
            pluginManagerMock
                .Setup(m => m.CreatePlugins(It.IsAny<IWriterFactory>(), It.IsAny<IProcessingNotificationsCollector>()))
                .Returns(plugins);
            pluginManagerMock
                .Setup(m => m.GetRequiredLogTypes())
                .Returns(new List<LogType> { LogType.Apache });
            pluginManagerMock
                .Setup(m => m.SendCompleteProcessingSignalToPlugins(It.IsAny<bool>()))
                .Returns(new PluginsExecutionResults());

            return pluginManagerMock;
        }

        private static LogSharkConfiguration GetConfig(string logLocation)
        {
            var iConfig = ConfigGenerator.GetConfigFromDictionary(new Dictionary<string, string>());
            var @params = new LogSharkCommandLineParameters
            {
                LogSetLocation = logLocation
            };
            return new LogSharkConfiguration(@params, iConfig, new NullLoggerFactory());
        }
        
        private static LogLine GetLogLine(string fileName, string pathToFile, int lineNumber, string @string)
        {
            var filePath = pathToFile == null
                ? fileName
                : $"{pathToFile}/{fileName}";
            var lastModifiedUtc = GetLastModifiedUtc(pathToFile, fileName);
            var logLineWithLineNumber = new ReadLogLineResult(lineNumber, @string);
            var logFileInfo = new LogFileInfo(fileName, filePath, "worker0", lastModifiedUtc);
            return new LogLine(logLineWithLineNumber, logFileInfo);
        }
        
        private static DateTime GetLastModifiedUtc(string pathToFile, string fileName)
        {
            var fullPath = Path.Combine(UnZippedTestSet, pathToFile ?? string.Empty, fileName);
            var fileInfo = new FileInfo(fullPath);
            return new DateTimeOffset(fileInfo.LastWriteTime.Ticks, TimeSpan.Zero).UtcDateTime;
        }
        
        private readonly HashSet<LogLine> _expectedLogLines = new HashSet<LogLine>
        {
            GetLogLine("correctLog1.log", null, 1, "Expected line 1"),
            GetLogLine("correctLog2.log", null, 1, "Expected line 2"),
            GetLogLine("correctLog3.log", "folder", 1, "Expected line 3"),
            GetLogLine("correctMultiLineLog.log", null, 1, "Expected multiline log line 1"),
            GetLogLine("correctMultiLineLog.log", null, 2, "Expected multiline log line 2"),
            GetLogLine("correctMultiLineLog.log", null, 3, "Expected multiline log line 3"),
        };
    }
}