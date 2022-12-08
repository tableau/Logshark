using FluentAssertions;
using LogShark.Plugins.Prep;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;
using LogShark.Tests.Plugins.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using Xunit;

namespace LogShark.Tests.Plugins
{
    public class PrepPluginTests : InvariantCultureTestsBase
    {
        private static readonly LogFileInfo TestLogFileInfo = new("foo.log", @"prep/foo.log", "worker0", DateTime.MinValue);


        [Fact]
        public void BadInput()
        {
            var processingNotificationsCollector = new ProcessingNotificationsCollector(10);
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new PrepPlugin())
            {
                plugin.Configure(testWriterFactory, null, processingNotificationsCollector, new NullLoggerFactory());

                var wrongContentFormat = new LogLine(new ReadLogLineResult(123, "An invalid Prep log line"), TestLogFileInfo);
                var nullContent = new LogLine(new ReadLogLineResult(123, null), TestLogFileInfo);

                plugin.ProcessLogLine(nullContent, LogType.Prep);
                plugin.ProcessLogLine(wrongContentFormat, LogType.Prep);
            }

            testWriterFactory.AssertAllWritersAreDisposedAndEmpty(1);
            processingNotificationsCollector.TotalErrorsReported.Should().Be(2);
        }


        [Fact]
        public void GoodInput()
        {
            var processingNotificationsCollector = new ProcessingNotificationsCollector(10);
            var testWriterFactory = new TestWriterFactory();
            using (var plugin = new PrepPlugin())
            {
                plugin.Configure(testWriterFactory, null, processingNotificationsCollector, new NullLoggerFactory());

                var goodLog = new LogLine(new ReadLogLineResult(123, "2022-06-22 19:37:30.244 -0500 (,,,,) Curator-PathChildrenCache-19 : ERROR org.apache.curator.framework.recipes.cache.PathChildrenCache - "), TestLogFileInfo);

                plugin.ProcessLogLine(goodLog, LogType.Prep);
            }

            testWriterFactory.GetOneWriterAndVerifyOthersAreEmptyAndDisposed<PrepEvent>("PrepEvents", 1);
            processingNotificationsCollector.TotalErrorsReported.Should().Be(0);
        }
    }
}
