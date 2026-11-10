using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AwesomeAssertions;
using LogShark.Containers;
using LogShark.Plugins;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;
using LogShark.Tests.Plugins.Helpers;
using LogShark.Writers.Containers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogShark.Tests.Plugins
{
    public class PluginManagerTests : InvariantCultureTestsBase
    {
        #region TestsWithtoutDefaultSetParameters
        
        [Fact]
        public void LoadAndDisposeAllPluginsExplicitly()
        {
            var (testWriterFactory, pluginManager) = InitPluginsWithTestWriter("All");
            var pluginsExecutionResults = pluginManager.SendCompleteProcessingSignalToPlugins();
            AssertLoadAllResults(testWriterFactory, pluginManager, pluginsExecutionResults.GetWritersStatistics());
            pluginManager.Dispose();
            testWriterFactory.AssertAllWritersDisposedState(true);
        }
        
        [Fact]
        public void LoadAndDisposeAllPluginsEmptyString()
        {
            var (testWriterFactory, pluginManager) = InitPluginsWithTestWriter(string.Empty);
            var pluginsExecutionResults = pluginManager.SendCompleteProcessingSignalToPlugins();
            AssertLoadAllResults(testWriterFactory, pluginManager, pluginsExecutionResults.GetWritersStatistics());
            pluginManager.Dispose();
            testWriterFactory.AssertAllWritersDisposedState(true);
        }
        
        [Fact]
        public void LoadAndDisposeAllPluginsNull()
        {
            var (testWriterFactory, pluginManager) = InitPluginsWithTestWriter(null);
            var pluginsExecutionResults = pluginManager.SendCompleteProcessingSignalToPlugins();
            AssertLoadAllResults(testWriterFactory, pluginManager, pluginsExecutionResults.GetWritersStatistics());
            pluginManager.Dispose();
            testWriterFactory.AssertAllWritersDisposedState(true);
        }
        
        [Fact]
        public void LoadAndDisposeOnePlugin()
        {
            var (testWriterFactory, pluginManager) = InitPluginsWithTestWriter("Apache");
            var loadedPlugins = pluginManager.GetPlugins();
            loadedPlugins.Count().Should().Be(1);
            loadedPlugins.First().Name.Should().Be("Apache");
            testWriterFactory.Writers.Count.Should().Be(1);
            testWriterFactory.AssertAllWritersDisposedState(false);
            
            var pluginsExecutionResults = pluginManager.SendCompleteProcessingSignalToPlugins();
            var writersStatistics = pluginsExecutionResults.GetWritersStatistics();
            writersStatistics.DataSets.Count.Should().Be(1);
            writersStatistics.DataSets.Should().ContainKey(new DataSetInfo("Apache", "ApacheRequests"));

            pluginManager.Dispose();
            testWriterFactory.AssertAllWritersDisposedState(true);
        }
        
        [Fact]
        public void LoadAndDisposeTwoPlugins()
        {
            var (testWriterFactory, pluginManager) = InitPluginsWithTestWriter("Apache;Filestore");
            var loadedPlugins = pluginManager.GetPlugins();
            loadedPlugins.Count().Should().Be(2);
            loadedPlugins.FirstOrDefault(plugin => plugin.Name == "Apache").Should().NotBe(null);
            loadedPlugins.FirstOrDefault(plugin => plugin.Name == "Filestore").Should().NotBe(null);
            testWriterFactory.Writers.Count.Should().Be(2);
            testWriterFactory.AssertAllWritersDisposedState(false);
            
            var pluginsExecutionResults = pluginManager.SendCompleteProcessingSignalToPlugins();
            var writersStatistics = pluginsExecutionResults.GetWritersStatistics();
            writersStatistics.DataSets.Count.Should().Be(2);
            writersStatistics.DataSets.Should().ContainKey(new DataSetInfo("Apache", "ApacheRequests"));
            writersStatistics.DataSets.Should().ContainKey(new DataSetInfo("Filestore", "Filestore"));

            pluginManager.Dispose();
            testWriterFactory.AssertAllWritersDisposedState(true);
        }
        
        [Fact]
        public void AllAppendedToOtherPlugin()
        {
            Action initPluginsWithInvalidPluginList = () => InitPluginsWithTestWriter("Apache;All");
            initPluginsWithInvalidPluginList.Should().Throw<Exception>();
        }

        [Theory]
        [InlineData("Apach")]
        [InlineData("Apachee")]
        [InlineData("1234567890")]
        public void VerifyPluginsThatDoNotExistThrowException(string pluginName)
        {
            Action initPluginsWithInvalidPluginList = () => InitPluginsWithTestWriter(pluginName);
            initPluginsWithInvalidPluginList.Should().Throw<Exception>();
        }

        [Fact]
        public void VerifyingSendCompleteProcessingSignalSafeguardIsInPlace()
        {
            var (_, plugins) = InitPluginsWithTestWriter("All");
            Action testAction = () => plugins.Dispose();
            testAction.Should().Throw<Exception>();
        }
        
        [Fact]
        public void VerifyPluginsReturnResultsOnCompleteSignal()
        {
            var (_, plugins) = InitPluginsWithTestWriter("All");
            var completeProcessingOutput = plugins.SendCompleteProcessingSignalToPlugins();
            completeProcessingOutput.Should().NotBeNull();
            var tagsList = completeProcessingOutput.GetSortedTagsFromAllPlugins();
            tagsList.Should().NotBeNull();
            tagsList.Count.Should().Be(0); // At the time of the writing, none of the plugins return tags without receiving information first
            plugins.Dispose();
        }
        
        #endregion TestsWithtoutDefaultSetParameters
        
        #region TestsWithDefaultSetParameters
        
        [Theory]
        [InlineData(null, null, null, null)] // Expected "null" means "all"
        [InlineData("", null, null, null)]
        [InlineData("All", null, null, null)]
        [InlineData("all", null, null, null)]
        [InlineData("All", "Postgres", null, "Postgres")]
        [InlineData("All", "Apache;Postgres", null, "Apache;Postgres")]
        [InlineData("All", "Apache;Postgres;Apache", null, "Apache;Postgres")]
        [InlineData("All", "Apache;Hyper;Postgres;Vizportal", null, "Apache;Hyper;Postgres;Vizportal")]
        [InlineData("All", "Apache;Hyper;Postgres;Vizportal", "Postgres", "Apache;Hyper;Vizportal")]
        [InlineData("All", "Apache;Hyper;Postgres;Vizportal", "Postgres;Hyper", "Apache;Vizportal")]
        [InlineData("Apache;Postgres;", null, "Postgres", "Apache;Postgres")]
        [InlineData("Apache;Postgres;", "Filestore;Postgres", "Postgres", "Apache;Postgres")]
        public void TestWithDefaultSetParameters(string requestedPlugins, string defaultSet, string excludedPlugins, string expectedPlugins)
        {
            var (testWriterFactory, pluginManager) = InitPluginsWithTestWriter(requestedPlugins, defaultSet, excludedPlugins);
            var pluginsExecutionStatistics = pluginManager.SendCompleteProcessingSignalToPlugins();

            if (expectedPlugins == null)
            {
                AssertLoadAllResults(testWriterFactory, pluginManager, pluginsExecutionStatistics.GetWritersStatistics());
            }
            else
            {
                var loadedPluginNames = pluginManager
                    .GetPlugins()
                    .Select(plugin => plugin.Name)
                    .OrderBy(name => name);
                var actualPluginNames = string.Join(';', loadedPluginNames);
                actualPluginNames.Should().Be(expectedPlugins);
            }
            
            pluginManager.Dispose();
            testWriterFactory.AssertAllWritersDisposedState(true);
        }

        [Theory]
        [InlineData("Postgres")]
        [InlineData("Apache;Postgres")]
        public void TestExclusionsWithoutDefaultSpecified(string excludedPlugins)
        {
            var (testWriterFactory, pluginManager) = InitPluginsWithTestWriter("all", null, excludedPlugins);

            var loadedPluginNames = pluginManager
                .GetPlugins()
                .Select(plugin => plugin.Name)
                .ToHashSet();
            var excludedPluginsList = excludedPlugins.Split(";");
            loadedPluginNames.Should().NotContain(excludedPluginsList);
            
            pluginManager.SendCompleteProcessingSignalToPlugins();
            pluginManager.Dispose();
            testWriterFactory.AssertAllWritersDisposedState(true);
        }
        
        #endregion TestsWithDefaultSetParameters

        [Fact]
        public void TestAssemblySelectionSwitch()
        {
            var someDefaultPlugins = new List<string> { "Apache", "Art", "Backgrounder", "Filestore", "Hyper"};

            var (_, pluginManagerUsingDefaults) = InitPluginsWithTestWriter("All");
            var pluginNamesDefaults = pluginManagerUsingDefaults.GetKnownPluginNames();
            pluginNamesDefaults.Should().Contain(someDefaultPlugins);

            var (_, pluginManagerUsingPluginsFromLogSharkAssembly) = InitPluginsWithTestWriter("All", null, null, true);
            var pluginNamesUsingPluginsFromLogSharkAssembly = pluginManagerUsingPluginsFromLogSharkAssembly.GetKnownPluginNames();
            pluginNamesUsingPluginsFromLogSharkAssembly.Should().Contain(someDefaultPlugins);

            var (_, pluginManagerNotUsingPluginsFromLogSharkAssembly) = InitPluginsWithTestWriter("All", null, null, false);
            var pluginNamesNotUsingPluginsFromLogSharkAssembly = pluginManagerNotUsingPluginsFromLogSharkAssembly.GetKnownPluginNames();
            pluginNamesNotUsingPluginsFromLogSharkAssembly.Should().BeEmpty(); // Entry Assembly for unit tests is different assembly that this one, so it doesn't have any IPlugin implementations
        }
        
        private static void AssertLoadAllResults(TestWriterFactory testWriterFactory, PluginManager pluginManager, WritersStatistics writersStatistics)
        {
            // As number of plugins can chance over time, we're simply verifying that two known plugins are loaded correctly
            var loadedPlugins = pluginManager.GetPlugins();
            loadedPlugins.Count().Should().BeGreaterThan(1);
            loadedPlugins.FirstOrDefault(plugin => plugin.Name == "Apache").Should().NotBe(null);
            loadedPlugins.FirstOrDefault(plugin => plugin.Name == "Filestore").Should().NotBe(null);
            
            testWriterFactory.Writers.Count.Should().BeGreaterThan(1);
            testWriterFactory.AssertAllWritersDisposedState(false);
            
            writersStatistics.DataSets.Should().ContainKey(new DataSetInfo("Apache", "ApacheRequests"));
            writersStatistics.DataSets.Should().ContainKey(new DataSetInfo("Filestore", "Filestore"));
        }

        [Fact]
        public void DynamicPluginDirectoryNull_NoError()
        {
            var (_, pluginManager) = InitPluginsWithTestWriter("Apache", dynamicPluginsDir: null);
            var plugins = pluginManager.GetPlugins();
            plugins.Count().Should().Be(1);
            plugins.First().Name.Should().Be("Apache");
            pluginManager.SendCompleteProcessingSignalToPlugins();
            pluginManager.Dispose();
        }

        [Fact]
        public void DynamicPluginDirectoryNonexistent_NoError()
        {
            var (_, pluginManager) = InitPluginsWithTestWriter("Apache", dynamicPluginsDir: "/nonexistent/path");
            var plugins = pluginManager.GetPlugins();
            plugins.Count().Should().Be(1);
            plugins.First().Name.Should().Be("Apache");
            pluginManager.SendCompleteProcessingSignalToPlugins();
            pluginManager.Dispose();
        }

        [Fact]
        public void DynamicPluginDirectoryEmpty_NoError()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "logshark_test_empty_plugins_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                var (_, pluginManager) = InitPluginsWithTestWriter("Apache", dynamicPluginsDir: tempDir);
                var plugins = pluginManager.GetPlugins();
                plugins.Count().Should().Be(1);
                pluginManager.SendCompleteProcessingSignalToPlugins();
                pluginManager.Dispose();
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [SkippableFact]
        public void DynamicPluginDiscovered_AppearsInKnownPlugins()
        {
            var samplePluginDir = FindSamplePluginOutputDir();
            Skip.If(samplePluginDir == null, "SampleApachePlugin has not been built. Run: dotnet publish DynamicPlugins/SampleApachePlugin -o DynamicPlugins/output/SampleApachePlugin");

            var (_, pluginManager) = InitPluginsWithTestWriter("Apache", dynamicPluginsDir: samplePluginDir);
            var knownNames = pluginManager.GetKnownPluginNames().ToList();
            knownNames.Should().Contain("Apache");
            knownNames.Should().Contain("SampleApache");
            pluginManager.SendCompleteProcessingSignalToPlugins();
            pluginManager.Dispose();
        }

        [SkippableFact]
        public void DynamicPluginCanBeLoaded()
        {
            var samplePluginDir = FindSamplePluginOutputDir();
            Skip.If(samplePluginDir == null, "SampleApachePlugin has not been built. Run: dotnet publish DynamicPlugins/SampleApachePlugin -o DynamicPlugins/output/SampleApachePlugin");

            var (testWriterFactory, pluginManager) = InitPluginsWithTestWriter("SampleApache", dynamicPluginsDir: samplePluginDir);
            var loadedPlugins = pluginManager.GetPlugins().ToList();
            loadedPlugins.Count.Should().Be(1);
            loadedPlugins[0].Name.Should().Be("SampleApache");

            var results = pluginManager.SendCompleteProcessingSignalToPlugins();
            results.Should().NotBeNull();
            pluginManager.Dispose();
        }

        [SkippableFact]
        public void DynamicPluginProcessesLogLines()
        {
            var samplePluginDir = FindSamplePluginOutputDir();
            Skip.If(samplePluginDir == null, "SampleApachePlugin has not been built. Run: dotnet publish DynamicPlugins/SampleApachePlugin -o DynamicPlugins/output/SampleApachePlugin");

            var (testWriterFactory, pluginManager) = InitPluginsWithTestWriter("SampleApache", dynamicPluginsDir: samplePluginDir);
            var plugins = pluginManager.GetPlugins().ToList();
            plugins.Count.Should().Be(1);
            plugins[0].Name.Should().Be("SampleApache");

            var logFileInfo = new LogFileInfo("test.log", "worker0/httpd/access.2018_10_02_00_00_00.log", "worker0", DateTime.MinValue);
            var logLine = new LogLine(
                new ReadLogLineResult(1,
                    "10.177.50.147 10.177.50.147 - 2018-10-02 22:18:41.686 \"Coordinated Universal Time\" 80 \"GET /auth?language=en HTTP/1.1\" \"-\" 200 6330 \"-\" 703082 W7Puwe2JQy69mALs70FQ4wAAAiA"),
                logFileInfo);

            plugins[0].ProcessLogLine(logLine, LogType.Apache);

            testWriterFactory.Writers.Count.Should().Be(1);
            var writer = testWriterFactory.Writers.Values.First();
            ((IList<object>)((dynamic)writer).ReceivedObjects).Count.Should().Be(1);

            var results = pluginManager.SendCompleteProcessingSignalToPlugins();
            results.Should().NotBeNull();
            pluginManager.Dispose();
        }

        [SkippableFact]
        public void DynamicAndBuiltInPluginsCoexist()
        {
            var samplePluginDir = FindSamplePluginOutputDir();
            Skip.If(samplePluginDir == null, "SampleApachePlugin has not been built. Run: dotnet publish DynamicPlugins/SampleApachePlugin -o DynamicPlugins/output/SampleApachePlugin");

            var (testWriterFactory, pluginManager) = InitPluginsWithTestWriter("Apache;SampleApache", dynamicPluginsDir: samplePluginDir);
            var loadedPlugins = pluginManager.GetPlugins().ToList();
            loadedPlugins.Count.Should().Be(2);
            loadedPlugins.Should().Contain(p => p.Name == "Apache");
            loadedPlugins.Should().Contain(p => p.Name == "SampleApache");

            // Both plugins registered writers
            testWriterFactory.Writers.Count.Should().Be(2);
            testWriterFactory.Writers.Keys.Should().Contain(new DataSetInfo("Apache", "ApacheRequests"));
            testWriterFactory.Writers.Keys.Should().Contain(new DataSetInfo("SampleApache", "SampleApacheRequests"));

            var results = pluginManager.SendCompleteProcessingSignalToPlugins();
            results.Should().NotBeNull();
            pluginManager.Dispose();
        }

        private static string FindSamplePluginOutputDir()
        {
            var dir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            while (dir != null)
            {
                var candidate = Path.Combine(dir, "DynamicPlugins", "output", "SampleApachePlugin");
                if (Directory.Exists(candidate) && Directory.GetFiles(candidate, "SampleApachePlugin.dll").Length > 0)
                    return candidate;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }

        private static (TestWriterFactory, PluginManager) InitPluginsWithTestWriter(
            string selectedPlugins,
            string defaultSet = null,
            string excludedPlugins = null,
            bool? usePluginsFromLogSharkAssembly = null,
            string dynamicPluginsDir = null)
        {
            var testWriterFactory = new TestWriterFactory();

            var configDictionary = new Dictionary<string, string>()
            {
                { "PluginsConfiguration:ApachePlugin:IncludeGatewayChecks", "true" },
                { "PluginsConfiguration:DefaultPluginSet:PluginsToRunByDefault", defaultSet },
                { "PluginsConfiguration:DefaultPluginSet:PluginsToExcludeFromDefaultSet", excludedPlugins }
            };
            if (usePluginsFromLogSharkAssembly.HasValue)
            {
                configDictionary["EnvironmentConfig:UsePluginsFromLogSharkAssembly"] = usePluginsFromLogSharkAssembly.Value.ToString();
            }

            var config = new LogSharkConfiguration(
                new LogSharkCommandLineParameters() { RequestedPlugins = selectedPlugins, DynamicPluginsDirectory = dynamicPluginsDir },
                ConfigGenerator.GetConfigFromDictionary(configDictionary),
                null);

            var pluginManager = new PluginManager(config, new NullLoggerFactory());
            pluginManager.CreatePlugins(testWriterFactory, null);

            return (testWriterFactory, pluginManager);
        }
    }
}