using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LogShark.Containers;
using LogShark.Plugins;
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

        private static (TestWriterFactory, PluginManager) InitPluginsWithTestWriter(
            string selectedPlugins,
            string defaultSet = null,
            string excludedPlugins = null,
            bool? usePluginsFromLogSharkAssembly = null)
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
                new LogSharkCommandLineParameters() { RequestedPlugins = selectedPlugins },
                ConfigGenerator.GetConfigFromDictionary(configDictionary), 
                null);

            var pluginManager = new PluginManager(config, new NullLoggerFactory());
            pluginManager.CreatePlugins(testWriterFactory, null);

            return (testWriterFactory, pluginManager);
        }
    }
}