using System;
using System.Collections.Generic;
using System.Linq;
using Flurl.Util;
using LogShark.Extensions;
using LogShark.Plugins.Config.Models;
using LogShark.Shared;

namespace LogShark.Plugins.Config
{
    public class ProcessInfoExtractor
    {
        private const string ConfigKeyWithHostMapping = "worker.hosts";

        private readonly ConfigFile _workgroupYml;
        private readonly ConfigFile _tabsvcYml;
        private readonly IDictionary<string, string> _hostnameNodeMapping;
        private readonly IDictionary<int, string> _hostnameMap;
        private readonly IProcessingNotificationsCollector _processingNotificationsCollector;

        public ProcessInfoExtractor(ConfigFile workgroupYml, ConfigFile tabsvcYml,Dictionary<string,string> HostnameNodeMapping, IProcessingNotificationsCollector processingNotificationsCollector)
        {
            _workgroupYml = workgroupYml;
            _tabsvcYml = tabsvcYml;
            _hostnameNodeMapping = HostnameNodeMapping;
            _hostnameMap = GetWorkerHostnameMap(workgroupYml, processingNotificationsCollector);
            _processingNotificationsCollector = processingNotificationsCollector;
        }

        public IList<ConfigProcessInfo> GenerateProcessInfoRecords()
        {
            if (_workgroupYml == null || _hostnameMap.Count == 0)
            {
                const string error = "Config file is null or hostname map is empty. Cannot generate process info records";
                _processingNotificationsCollector.ReportError(error, nameof(ConfigPlugin));
                return new List<ConfigProcessInfo>();
            }
            
            var processInfoRecords = new List<ConfigProcessInfo>();
            
            for (var workerIndex = 0; workerIndex < _hostnameMap.Count; workerIndex++)
            {
                processInfoRecords.AddRange(GetWorkerDetails(workerIndex));
            }

            // Add process info records that work differently than other processes
            processInfoRecords.AddRange(GetPostgresProcessDetails());

            return processInfoRecords;
        }
        
        private IDictionary<int, string> GetWorkerHostnameMap(ConfigFile configFile, IProcessingNotificationsCollector processingNotificationsCollector)
        {
            if (configFile == null || !configFile.Values.ContainsKey(ConfigKeyWithHostMapping))
            {
                var error = $"Config file is null or doesn't contain {ConfigKeyWithHostMapping} key. Cannot generate hostname map for workers";
                processingNotificationsCollector.ReportError(error, nameof(ConfigPlugin));
                return new Dictionary<int, string>();
            }

            var workerHostsLine = configFile.Values[ConfigKeyWithHostMapping];
            
            return workerHostsLine
                .Split(',')
                .Select((hostName, ind) => new
                {
                    Index = ind,
                    HostName = hostName.Trim()
                })
                .ToDictionary(obj => obj.Index, obj => obj.HostName);
        }

        private IEnumerable<ConfigProcessInfo> GetWorkerDetails(int workerIndex)
        {
            var workerDetails = new List<ConfigProcessInfo>();
            var workerKeyRoot = $"worker{workerIndex}.";
            var workerKeys = _workgroupYml.Values.Keys.Where(key => key.StartsWith(workerKeyRoot, StringComparison.InvariantCultureIgnoreCase));
            var hostName = _hostnameMap[workerIndex];
            var node = "node0"; // Node0 doesn't ever exist so this is a default value

                if (_hostnameNodeMapping!=null && _hostnameNodeMapping.TryGetValue(hostName, out node))
                {
                    node = _hostnameNodeMapping[hostName].ToKeyValuePairs().First().Key;
                }
                else
                { // Provide worker index if node number cannot be found. It can still be indentified as its will be just a number with no leading "node"
                    node = "worker"+workerIndex.ToString();
                }

            var processNames = workerKeys
                .Select(key => key.Substring(workerKeyRoot.Length))
                .Where(key => key.Contains('.'))
                .Select(key => key.Substring(0, key.IndexOf('.')))
                .Distinct();

            foreach (var processName in processNames)
            {
                var processCount = GetProcessCount(workerIndex, processName);

                if (!processCount.HasValue)
                {
                    continue;
                }

                for (var i = 0; i < processCount; i++)
                {
                    var processPort = GetPort(workerIndex, processName);
                    if (processPort == null)
                    {
                        processPort = GetPort(workerIndex, $"{processName}_{i}");
                    }
                    else
                    {
                        processPort += i;
                    }

                    if (processPort.HasValue)
                    {
                        var configProcessInfo = new ConfigProcessInfo(_workgroupYml.LogFileInfo.LastModifiedUtc, _hostnameMap[workerIndex], processPort.Value, processName, node);
                        workerDetails.Add(configProcessInfo);
                    }
                }
            }

            return workerDetails;
        }

        private IEnumerable<ConfigProcessInfo> GetPostgresProcessDetails()
        {
            const string pgsqlProcessName = "pgsql";
            var postgresProcessInfo = new List<ConfigProcessInfo>();
            var workerCount = _hostnameMap.Count;
            
            for (var postgresProcessIndex = 0; postgresProcessIndex < workerCount; postgresProcessIndex++)
            {
                var pgsqlHostKey = $"{pgsqlProcessName}{postgresProcessIndex}.host";
                var pgsqlPortKey = $"{pgsqlProcessName}{postgresProcessIndex}.port";

                var port = _workgroupYml.Values.GetIntValueOrNull(pgsqlPortKey);

                var hostname = workerCount == 1
                    ? _hostnameMap[0] // Single node deployments don't store out the pgsqlX.host key, but we can safely assume that postgres lives on the -only- hostname.
                    : _workgroupYml.Values.GetStringValueOrNull(pgsqlHostKey) // If workgroup.yml has a pgsqlX.host entry, use it.
                      ?? _workgroupYml.Values.GetStringValueOrNull("pgsql.host") // otherwise, for a multi node server with only one pg process, "pgsql.host"
                      ?? _tabsvcYml?.Values.GetStringValueOrNull("pgsql.host") // Otherwise try to get pgsql.host from tabsvc.yml.  We have to do this when multiple nodes are used and no failover node is specified.
                      ?? "(Unknown)";
                
                var workerIndex = _hostnameMap
                    .Where(pair => pair.Value.Equals(hostname, StringComparison.InvariantCultureIgnoreCase))
                    .Select(pair => pair.Key)
                    .Cast<int?>()
                    .FirstOrDefault();
                
              
                var node = "node0"; // Node0 doesn't ever exist so this is a default value

                    if (_hostnameNodeMapping!=null && _hostnameNodeMapping.TryGetValue(hostname, out node))
                    {
                        node = _hostnameNodeMapping[hostname].ToKeyValuePairs().First().Key;
                    }
                    else
                    { // Provide worker index if node number cannot be found. It can still be indentified as its will be just a number with no leading "node"
                    node = "worker" + workerIndex.ToString();
                }

                if (port.HasValue && workerIndex.HasValue)
                {
                    postgresProcessInfo.Add(new ConfigProcessInfo(_workgroupYml.LogFileInfo.LastModifiedUtc, hostname, port ?? -1, pgsqlProcessName, node )); // These -1 are not used as we check "HasValue" above. They only needed to shut up compiler
                }
            }

            return postgresProcessInfo;
        }
                
        private int? GetProcessCount(int workerIndex, string processName)
        {
            var processKeyPrefix = $"worker{workerIndex}.{processName}".ToLowerInvariant();
            var procsKey = $"{processKeyPrefix}.procs";
            var enabledKey = $"{processKeyPrefix}.enabled";

            // We have two cases to deal with here: instances where the number of procs is called out, and instances where all we know is whether it is enabled or not.
            var processCount = _workgroupYml.Values.GetIntValueOrNull(procsKey);
            if (processCount.HasValue)
            {
                return processCount;
            }
            
            var processEnabled = _workgroupYml.Values.GetBoolValueOrNull(enabledKey);
            return processEnabled ?? false
                ? (int?) 1
                : null;
        }

        private int? GetPort(int workerIndex, string processName)
        {
            var processKeyPrefix = $"worker{workerIndex}.{processName}".ToLowerInvariant();
            var portKey = $"{processKeyPrefix}.port";
            var globalPortKey = $"{processName}.port";

            return _workgroupYml.Values.GetIntValueOrNull(portKey)
                   ?? _workgroupYml.Values.GetIntValueOrNull(globalPortKey);
        }
    }
}