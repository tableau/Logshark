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
    public class PluginInitializerTests : InvariantCultureTestsBase
    {
        #region TestsWithtoutDefaultSetParameters
        
        [Fact]
        public void LoadAndDisposeAllPluginsExplicitly()
        {
            var (testWriterFactory, plugins) = InitPluginsWithTestWriter("All");
            var pluginsExecutionResults = plugins.SendCompleteProcessingSignalToPlugins();
            AssertLoadAllResults(testWriterFactory, plugins, pluginsExecutionResults.GetWritersStatistics());
            plugins.Dispose();
            testWriterFactory.AssertAllWritersDisposedState(true);
        }
        
        [Fact]
        public void LoadAndDisposeAllPluginsEmptyString()
        {
            var (testWriterFactory, plugins) = InitPluginsWithTestWriter(string.Empty);
            var pluginsExecutionResults = plugins.SendCompleteProcessingSignalToPlugins();
            AssertLoadAllResults(testWriterFactory, plugins, pluginsExecutionResults.GetWritersStatistics());
            plugins.Dispose();
            testWriterFactory.AssertAllWritersDisposedState(true);
        }
        
        [Fact]
        public void LoadAndDisposeAllPluginsNull()
        {
            var (testWriterFactory, plugins) = InitPluginsWithTestWriter(null);
            var pluginsExecutionResults = plugins.SendCompleteProcessingSignalToPlugins();
            AssertLoadAllResults(testWriterFactory, plugins, pluginsExecutionResults.GetWritersStatistics());
            plugins.Dispose();
            testWriterFactory.AssertAllWritersDisposedState(true);
        }
        
        [Fact]
        public void LoadAndDisposeOnePlugin()
        {
            var (testWriterFactory, plugins) = InitPluginsWithTestWriter("Apache");
            
            plugins.LoadedPlugins.Count.Should().Be(1);
            plugins.LoadedPlugins.First().Name.Should().Be("Apache");
            testWriterFactory.Writers.Count.Should().Be(1);
            testWriterFactory.AssertAllWritersDisposedState(false);
            
            var pluginsExecutionResults = plugins.SendCompleteProcessingSignalToPlugins();
            var writersStatistics = pluginsExecutionResults.GetWritersStatistics();
            writersStatistics.DataSets.Count.Should().Be(1);
            writersStatistics.DataSets.Should().ContainKey(new DataSetInfo("Apache", "ApacheRequests"));
            
            plugins.Dispose();
            testWriterFactory.AssertAllWritersDisposedState(true);
        }
        
        [Fact]
        public void LoadAndDisposeTwoPlugins()
        {
            var (testWriterFactory, plugins) = InitPluginsWithTestWriter("Apache;Filestore");
            
            plugins.LoadedPlugins.Count.Should().Be(2);
            plugins.LoadedPlugins.FirstOrDefault(plugin => plugin.Name == "Apache").Should().NotBe(null);
            plugins.LoadedPlugins.FirstOrDefault(plugin => plugin.Name == "Filestore").Should().NotBe(null);
            testWriterFactory.Writers.Count.Should().Be(2);
            testWriterFactory.AssertAllWritersDisposedState(false);
            
            var pluginsExecutionResults = plugins.SendCompleteProcessingSignalToPlugins();
            var writersStatistics = pluginsExecutionResults.GetWritersStatistics();
            writersStatistics.DataSets.Count.Should().Be(2);
            writersStatistics.DataSets.Should().ContainKey(new DataSetInfo("Apache", "ApacheRequests"));
            writersStatistics.DataSets.Should().ContainKey(new DataSetInfo("Filestore", "Filestore"));
            
            plugins.Dispose();
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
            var (testWriterFactory, plugins) = InitPluginsWithTestWriter(requestedPlugins, defaultSet, excludedPlugins);
            var pluginsExecutionStatistics = plugins.SendCompleteProcessingSignalToPlugins();

            if (expectedPlugins == null)
            {
                AssertLoadAllResults(testWriterFactory, plugins, pluginsExecutionStatistics.GetWritersStatistics());
            }
            else
            {
                var loadedPluginNames = plugins.LoadedPlugins
                    .Select(plugin => plugin.Name)
                    .OrderBy(name => name);
                var actualPluginNames = string.Join(';', loadedPluginNames);
                actualPluginNames.Should().Be(expectedPlugins);
            }
            
            plugins.Dispose();
            testWriterFactory.AssertAllWritersDisposedState(true);
        }

        [Theory]
        [InlineData("Postgres")]
        [InlineData("Apache;Postgres")]
        public void TestExclusionsWithoutDefaultSpecified(string excludedPlugins)
        {
            var (testWriterFactory, plugins) = InitPluginsWithTestWriter("all", null, excludedPlugins);

            var loadedPluginNames = plugins.LoadedPlugins
                .Select(plugin => plugin.Name)
                .ToHashSet();
            var excludedPluginsList = excludedPlugins.Split(";");
            loadedPluginNames.Should().NotContain(excludedPluginsList);
            
            plugins.SendCompleteProcessingSignalToPlugins();
            plugins.Dispose();
            testWriterFactory.AssertAllWritersDisposedState(true);
        }
        
        #endregion TestsWithDefaultSetParameters

        [Fact]
        public void TestAssemblySelectionSwitch()
        {
            var someDefaultPlugins = new List<string> { "Apache", "Art", "Backgrounder", "Filestore", "Hyper"};
            
            var pluginNamesDefaults = PluginInitializer.GetAllAvailablePluginNames();
            var pluginNamesWithTrue = PluginInitializer.GetAllAvailablePluginNames(true);
            var pluginNamesWithFalse = PluginInitializer.GetAllAvailablePluginNames(false);
            
            pluginNamesDefaults.Should().Contain(someDefaultPlugins);
            pluginNamesWithTrue.Should().Contain(someDefaultPlugins);
            pluginNamesWithFalse.Should().BeEmpty(); // Entry Assembly for unit tests is different assembly that this one, so it doesn't have any IPlugin implementations
        }
        
        private static void AssertLoadAllResults(TestWriterFactory testWriterFactory, PluginInitializer plugins, WritersStatistics writersStatistics)
        {
            // As number of plugins can chance over time, we're simply verifying that two known plugins are loaded correctly
            plugins.LoadedPlugins.Count.Should().BeGreaterThan(1);
            plugins.LoadedPlugins.FirstOrDefault(plugin => plugin.Name == "Apache").Should().NotBe(null);
            plugins.LoadedPlugins.FirstOrDefault(plugin => plugin.Name == "Filestore").Should().NotBe(null);
            
            testWriterFactory.Writers.Count.Should().BeGreaterThan(1);
            testWriterFactory.AssertAllWritersDisposedState(false);
            
            writersStatistics.DataSets.Should().ContainKey(new DataSetInfo("Apache", "ApacheRequests"));
            writersStatistics.DataSets.Should().ContainKey(new DataSetInfo("Filestore", "Filestore"));
        }

        private static (TestWriterFactory, PluginInitializer) InitPluginsWithTestWriter(
            string selectedPlugins,
            string defaultSet = null,
            string excludedPlugins = null)
        {
            var testWriterFactory = new TestWriterFactory();
            var config = new LogSharkConfiguration(
                new LogSharkCommandLineParameters() { RequestedPlugins = selectedPlugins },
                ConfigGenerator.GetConfigFromDictionary(new Dictionary<string, string>
                {
                    { "PluginsConfiguration:ApachePlugin:IncludeGatewayChecks", "true" },
                    { "PluginsConfiguration:DefaultPluginSet:PluginsToRunByDefault", defaultSet },
                    { "PluginsConfiguration:DefaultPluginSet:PluginsToExcludeFromDefaultSet", excludedPlugins }
                }), 
                null);

            var plugins = new PluginInitializer(testWriterFactory, config, null, new NullLoggerFactory());

            return (testWriterFactory, plugins);
        }
    }
}