using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LogShark.Containers;
using LogShark.Plugins.Config;
using LogShark.Plugins.Config.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogShark.Tests.Plugins.ConfigPlugin
{
    public class ProcessInfoExtractorTests : InvariantCultureTestsBase
    {
        private const string MachineOneName = "machine1";
        private const string MachineTwoName = "machine2";
        
        private static readonly LogFileInfo TabsvcYmlLogFileInfo = new LogFileInfo("tabsvc.yml", "tabsvc.yml", "worker0", DateTime.Now);
        private static readonly LogFileInfo WorkgroupYmlLogFileInfo = new LogFileInfo("workgroup.yml", "config/workgrpup.yml", "worker0", DateTime.Now);
        
        [Fact]
        public void OneWorkerHost()
        {
            var workgroupYml = new ConfigFile(WorkgroupYmlLogFileInfo, _singleMachineWorkgroupYmlValues, LogType.WorkgroupYml);
            
            var extractor = new ProcessInfoExtractor(workgroupYml, null, null);
            var results = extractor.GenerateProcessInfoRecords();
            
            results.Should().BeEquivalentTo(_expectedResultsForSingleMachine);
        }
        
        [Fact]
        public void TwoWorkersHosts_OnePostgres()
        {
            var workgroupYml = new ConfigFile(WorkgroupYmlLogFileInfo, GetTwoMachinesWorkgroupYmlValues(), LogType.WorkgroupYml);
            
            var extractor = new ProcessInfoExtractor(workgroupYml, null, null);
            var results = extractor.GenerateProcessInfoRecords();

            var expectedResults = _expectedResultsForSingleMachine;
            expectedResults.UnionWith(_expectedResultsForSecondMachine);

            results.Should().BeEquivalentTo(expectedResults);
        }

        [Theory]
        [InlineData("8062", MachineTwoName, true)]
        [InlineData("8062", "someOtherMachine", false)]
        [InlineData("blah", MachineTwoName, false)]
        private void TwoWorkersHosts_VariousPostgresConfigurations(string portValue, string hostValue, bool expectObjectCreated)
        {
            var values = GetTwoMachinesWorkgroupYmlValues();
            values.Add("pgsql1.port", portValue);
            values.Add("pgsql1.host", hostValue);
            var workgroupYml = new ConfigFile(WorkgroupYmlLogFileInfo, values, LogType.WorkgroupYml);
            
            var extractor = new ProcessInfoExtractor(workgroupYml, null, null);
            var results = extractor.GenerateProcessInfoRecords();

            var expectedResults = _expectedResultsForSingleMachine;
            expectedResults.UnionWith(_expectedResultsForSecondMachine);
            if (expectObjectCreated)
            {
                var port = int.Parse(portValue);
                expectedResults.Add(new ConfigProcessInfo(WorkgroupYmlLogFileInfo.LastModifiedUtc, hostValue, port, "pgsql", 1));
            }

            results.Should().BeEquivalentTo(expectedResults);
        }
        
        [Fact]
        public void TwoWorkerHosts_OnlyProcessesConfiguredForOne()
        {
            var values = _singleMachineWorkgroupYmlValues;
            values["worker.hosts"] = $"{MachineOneName}, SomeOtherMachine";
            values["pgsql0.host"] = MachineOneName;
            var workgroupYml = new ConfigFile(WorkgroupYmlLogFileInfo, values, LogType.WorkgroupYml);
            
            var extractor = new ProcessInfoExtractor(workgroupYml, null, null);
            var results = extractor.GenerateProcessInfoRecords();

            results.Should().BeEquivalentTo(_expectedResultsForSingleMachine);
        }
        
        [Fact]
        public void OneWorkerHost_ProcessesConfiguredForTwo()
        {
            var values = GetTwoMachinesWorkgroupYmlValues();
            values["worker.hosts"] = MachineOneName;
            var workgroupYml = new ConfigFile(WorkgroupYmlLogFileInfo, values, LogType.WorkgroupYml);
            
            var extractor = new ProcessInfoExtractor(workgroupYml, null, null);
            var results = extractor.GenerateProcessInfoRecords();

            results.Should().BeEquivalentTo(_expectedResultsForSingleMachine);
        }
        
        [Fact]
        public void TwoWorkerHosts_SecondSetOfProcessesConfiguredForTheWrongWorker()
        {
            var values = GetTwoMachinesWorkgroupYmlValues();
            values = values
                .Select(pair => pair.Key.StartsWith("worker1")
                    ? new KeyValuePair<string, string>(pair.Key.Replace("worker1", "worker2"), pair.Value)
                    : pair)
                .ToDictionary(pair => pair.Key, pair => pair.Value);
            var workgroupYml = new ConfigFile(WorkgroupYmlLogFileInfo, values, LogType.WorkgroupYml);
            
            var extractor = new ProcessInfoExtractor(workgroupYml, null, null);
            var results = extractor.GenerateProcessInfoRecords();

            results.Should().BeEquivalentTo(_expectedResultsForSingleMachine);
        }
        
        [Fact]
        public void TwoWorkerHost_TryGetPostgresConfigFromTabsvc_ConfigPresent()
        {
            var workgroupValues = GetTwoMachinesWorkgroupYmlValues_StripPostgresHosts();
            var workgroupYml = new ConfigFile(WorkgroupYmlLogFileInfo, workgroupValues, LogType.WorkgroupYml);
            
            var tabsvcValues = new Dictionary<string,string>
            {
                { "pgsql.host", MachineOneName },
                { "someOtherKey", "someOtherValue"}
            };
            var tabsvcYml = new ConfigFile(TabsvcYmlLogFileInfo, tabsvcValues, LogType.TabsvcYml);
            
            var extractor = new ProcessInfoExtractor(workgroupYml, tabsvcYml, null);
            var results = extractor.GenerateProcessInfoRecords();
            
            var expectedResults = _expectedResultsForSingleMachine;
            expectedResults.UnionWith(_expectedResultsForSecondMachine);

            results.Should().BeEquivalentTo(expectedResults.Where(processInfo => !(processInfo.Process == "pgsql" && processInfo.Worker == 1)));
        }
        
        [Fact]
        public void TwoWorkerHost_TryGetPostgresConfigFromTabsvc_ConfigLineMissing()
        {
            var workgroupValues = GetTwoMachinesWorkgroupYmlValues_StripPostgresHosts();
            var workgroupYml = new ConfigFile(WorkgroupYmlLogFileInfo, workgroupValues, LogType.WorkgroupYml);
            
            var tabsvcValues = new Dictionary<string,string>
            {
                { "someOtherKey", "someOtherValue"}
            };
            var tabsvcYml = new ConfigFile(TabsvcYmlLogFileInfo, tabsvcValues, LogType.TabsvcYml);
            
            var extractor = new ProcessInfoExtractor(workgroupYml, tabsvcYml, null);
            var results = extractor.GenerateProcessInfoRecords();
            
            var expectedResults = _expectedResultsForSingleMachine;
            expectedResults.UnionWith(_expectedResultsForSecondMachine);

            results.Should().BeEquivalentTo(expectedResults.Where(processInfo => processInfo.Process != "pgsql"));
        }
        
        [Fact]
        public void TwoWorkerHost_TryGetPostgresConfigFromTabsvc_TabsvcNotPresent()
        {
            var workgroupValues = GetTwoMachinesWorkgroupYmlValues_StripPostgresHosts();
            var workgroupYml = new ConfigFile(WorkgroupYmlLogFileInfo, workgroupValues, LogType.WorkgroupYml);
            
            var extractor = new ProcessInfoExtractor(workgroupYml, null, null);
            var results = extractor.GenerateProcessInfoRecords();
            
            var expectedResults = _expectedResultsForSingleMachine;
            expectedResults.UnionWith(_expectedResultsForSecondMachine);

            results.Should().BeEquivalentTo(expectedResults.Where(processInfo => processInfo.Process != "pgsql"));
        }
        
        private readonly IDictionary<string, string> _singleMachineWorkgroupYmlValues = new Dictionary<string, string>
        {
            { "worker.hosts", MachineOneName },
            { "worker0.backgrounder.port", "8250" },
            { "worker0.backgrounder.procs", "1" },
            { "dataengine.port", "27042" }, // Data engine uses "global" port settings
            { "worker0.dataengine.procs", "1" },
            { "worker0.dataserver.port", "9700" },
            { "worker0.dataserver.procs", "2" },
            { "filestore.port" , "9345" },
            { "worker0.filestore.enabled", "true" }, // Filestore doesn't have process count, just enabled/disabled switch
            { "worker0.gateway.enabled", "true" },
            { "worker0.gateway.port", "80" },
            { "worker0.host", "IShouldNotBeUsed!" },
            { "worker0.instance_id", "0" },
            { "worker0.vizhub.port", "7000" }, // Port is there, but process does not show up because it doesn't have config
            { "pgsql0.port" , "8060" }
        };

        private IDictionary<string, string> GetTwoMachinesWorkgroupYmlValues()
        {
            var secondMachineUniqueValues = new Dictionary<string, string>
            {
                { "worker1.host", "StillShouldn'tBeUsed" },
                { "worker1.vizportal.port", "8600" },
                { "worker1.vizportal.procs", "1" },
                { "worker1.dataserver.port", "9700" },
                { "worker1.dataserver.procs", "2" },
                { "worker1.dataserver.keychain.port", "8640" },
                { "worker1.searchserver.enabled", "true" },
                { "worker1.searchserver.port", "8859" },
                { "worker1.searchserver.startup.port", "8503" },
                { "worker1.dataengine.procs", "0" }, // Global port setting is included into "primary" config. But we have procs set to 0 anyway
                { "worker1.hyper.enabled", "true" },
                { "worker1.hyper.port", "8360" },
                { "worker1.filestore.enabled", "true" }, // Global port is in "primary" config
                { "gateway.port", "8000" }, // Global setting which should be used for this worker, but ignored by Primary
                { "worker1.gateway.enabled", "true" },
                { "pgsql0.host" , "machine1" }
            };

            var combinedConfig = secondMachineUniqueValues.Union(_singleMachineWorkgroupYmlValues).ToDictionary(pair => pair.Key, pair => pair.Value);
            combinedConfig["worker.hosts"] = $" {MachineOneName}  , \n {MachineTwoName}";
            return combinedConfig;
        }

        private IDictionary<string, string> GetTwoMachinesWorkgroupYmlValues_StripPostgresHosts()
        {
            var configValues = GetTwoMachinesWorkgroupYmlValues();
            return configValues
                .Where(pair => !(pair.Key.StartsWith("pgsql") && pair.Key.EndsWith("host")))
                .ToDictionary(pair => pair.Key, pair => pair.Value);
        }
        
        private readonly ISet<dynamic> _expectedResultsForSingleMachine = new HashSet<dynamic>
        {
            ConfigPluginTests.ExpectedProcessInfoEntry(MachineOneName, 8250, "backgrounder", 0, WorkgroupYmlLogFileInfo.LastModifiedUtc),
            ConfigPluginTests.ExpectedProcessInfoEntry(MachineOneName, 27042, "dataengine", 0, WorkgroupYmlLogFileInfo.LastModifiedUtc),
            ConfigPluginTests.ExpectedProcessInfoEntry(MachineOneName, 9700, "dataserver", 0, WorkgroupYmlLogFileInfo.LastModifiedUtc),
            ConfigPluginTests.ExpectedProcessInfoEntry(MachineOneName, 9701, "dataserver", 0, WorkgroupYmlLogFileInfo.LastModifiedUtc),
            ConfigPluginTests.ExpectedProcessInfoEntry(MachineOneName, 9345, "filestore", 0, WorkgroupYmlLogFileInfo.LastModifiedUtc),
            ConfigPluginTests.ExpectedProcessInfoEntry(MachineOneName, 80, "gateway", 0, WorkgroupYmlLogFileInfo.LastModifiedUtc),
            ConfigPluginTests.ExpectedProcessInfoEntry(MachineOneName, 8060, "pgsql", 0, WorkgroupYmlLogFileInfo.LastModifiedUtc),
        };
        
        private readonly ISet<dynamic> _expectedResultsForSecondMachine = new HashSet<dynamic>
        {
            ConfigPluginTests.ExpectedProcessInfoEntry(MachineTwoName, 8600, "vizportal", 1, WorkgroupYmlLogFileInfo.LastModifiedUtc),
            ConfigPluginTests.ExpectedProcessInfoEntry(MachineTwoName, 9700, "dataserver", 1, WorkgroupYmlLogFileInfo.LastModifiedUtc),
            ConfigPluginTests.ExpectedProcessInfoEntry(MachineTwoName, 9701, "dataserver", 1, WorkgroupYmlLogFileInfo.LastModifiedUtc),
            ConfigPluginTests.ExpectedProcessInfoEntry(MachineTwoName, 8859, "searchserver", 1, WorkgroupYmlLogFileInfo.LastModifiedUtc),
            ConfigPluginTests.ExpectedProcessInfoEntry(MachineTwoName, 8360, "hyper", 1, WorkgroupYmlLogFileInfo.LastModifiedUtc),
            ConfigPluginTests.ExpectedProcessInfoEntry(MachineTwoName, 9345, "filestore", 1, WorkgroupYmlLogFileInfo.LastModifiedUtc),
            ConfigPluginTests.ExpectedProcessInfoEntry(MachineTwoName, 8000, "gateway", 1, WorkgroupYmlLogFileInfo.LastModifiedUtc),
        };
    }
}