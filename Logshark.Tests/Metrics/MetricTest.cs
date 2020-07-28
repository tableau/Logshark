using FluentAssertions;
using LogShark.Containers;
using LogShark.Metrics;
using LogShark.Metrics.Models;
using LogShark.Tests.Plugins.Helpers;
using LogShark.Writers.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace LogShark.Tests.Metrics
{
    public class MetricTest : InvariantCultureTestsBase
    {
        private readonly MetricTestUploader _testUploader;
        private readonly MetricsConfig _configurationLevelFull;
        private readonly MetricsConfig _configurationLevelBasic;
        private readonly MetricsConfig _configurationLevelNone;

        private readonly LogSharkConfiguration _configDefault;
        private readonly RunSummary _runSummaryDefault;

        public MetricTest()
        {
            _testUploader = new MetricTestUploader();
            _configurationLevelFull = new MetricsConfig(_testUploader, TelemetryLevel.Full);
            _configurationLevelBasic = new MetricsConfig(_testUploader, TelemetryLevel.Basic);
            _configurationLevelNone = new MetricsConfig(_testUploader, TelemetryLevel.None);
            _configDefault = new LogSharkConfiguration(new LogSharkCommandLineParameters(), new ConfigurationBuilder().Build(), null);
            _runSummaryDefault = RunSummary.FailedRunSummary(null, null);
        }

        [Fact]
        public async Task MetricsShouldUploadOnTelemetryLevelFull()
        {
            var metricsModule = new MetricsModule(_configurationLevelFull, new NullLoggerFactory());

            await metricsModule.ReportStartMetrics(_configDefault);
            _testUploader.UploadCallCount.Should().Be(1);

            await metricsModule.ReportEndMetrics(_runSummaryDefault);
            _testUploader.UploadCallCount.Should().Be(2);
        }

        [Fact]
        public async Task MetricsShouldUploadOnTelemetryLevelBasic()
        {
            var metricsModule = new MetricsModule(_configurationLevelBasic, new NullLoggerFactory());

            await metricsModule.ReportStartMetrics(_configDefault);
            _testUploader.UploadCallCount.Should().Be(1);

            await metricsModule.ReportEndMetrics(_runSummaryDefault);
            _testUploader.UploadCallCount.Should().Be(2);
        }

        [Fact]
        public async Task MetricsShouldNotUploadOnTelemetryLevelNone()
        {
            var metricsModule = new MetricsModule(_configurationLevelNone, new NullLoggerFactory());

            await metricsModule.ReportStartMetrics(_configDefault);
            _testUploader.UploadCallCount.Should().Be(0);

            await metricsModule.ReportEndMetrics(_runSummaryDefault);
            _testUploader.UploadCallCount.Should().Be(0);
        }

        [Fact]
        public async Task MetricsShouldNotUploadOnEmptyConfig()
        {
            var metricsModule = new MetricsModule(null, new NullLoggerFactory());

            await metricsModule.ReportStartMetrics(_configDefault);
            _testUploader.UploadCallCount.Should().Be(0);

            await metricsModule.ReportEndMetrics(_runSummaryDefault);
            _testUploader.UploadCallCount.Should().Be(0);
        }

        [Fact]
        public async Task MetricsShouldKeepUserDataOnLevelFull()
        {
            var metricsModule = new MetricsModule(_configurationLevelFull, new NullLoggerFactory());

            await metricsModule.ReportStartMetrics(_configDefault);
            var uploadedModel = _testUploader.UploadedPayloads[0] as StartMetrics;
            uploadedModel.System.Username.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task MetricsShouldRemoveUserDataOnLevelBasic()
        {
            var metricsModule = new MetricsModule(_configurationLevelBasic, new NullLoggerFactory());

            await metricsModule.ReportStartMetrics(_configDefault);
            var uploadedModel = _testUploader.UploadedPayloads[0] as StartMetrics;
            uploadedModel.System.Username.Should().BeNull();
        }
    }
}
