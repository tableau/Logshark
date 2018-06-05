using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.PluginLib.Helpers;
using Logshark.Plugins.Config.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Logshark.Plugins.Config.Helpers
{
    public class ConfigReader
    {
        protected readonly IMongoDatabase mongoDatabase;
        protected readonly BsonDocument configDocument;
        protected readonly IDictionary<string, string> config;
        protected readonly DateTime? fileLastModifiedTimestamp;
        protected readonly IDictionary<int, string> workerHostnameMap;
        protected readonly string logsetHash;

        public ConfigReader(IMongoDatabase mongoDatabase, Guid logsetHash)
        {
            this.mongoDatabase = mongoDatabase;
            configDocument = LoadConfigDocument();
            config = LoadConfig();
            fileLastModifiedTimestamp = GetConfigModificationTimestamp();
            workerHostnameMap = ConfigDataHelper.GetWorkerHostnameMap(mongoDatabase);
            this.logsetHash = logsetHash.ToString();
        }

        #region Public Methods

        /// <summary>
        /// Loads the config JSON from MongoDB and flattens it into an easy-to-use dictionary.
        /// </summary>
        /// <returns>Config as a dictionary of key-value pairs.</returns>
        public IDictionary<string, string> LoadConfig()
        {
            var configContents = configDocument["contents"].AsBsonDocument;

            IDictionary<string, string> configDictionary = new Dictionary<string, string>();

            foreach (var configElement in configContents.Elements)
            {
                var configEntries = GetConfigEntriesFromElement(configElement);
                foreach (var configEntry in configEntries)
                {
                    configDictionary.Add(configEntry);
                }
            }

            return configDictionary;
        }

        public DateTime? GetConfigModificationTimestamp()
        {
            return BsonDocumentHelper.GetNullableDateTime("last_modified_at", configDocument);
        }

        /// <summary>
        /// Retrieves all workgroup.yml config entries in the given logset.
        /// </summary>
        /// <returns>Collection of config entries for the given logset.</returns>
        public ICollection<ConfigEntry> GetConfigEntries()
        {
            return config.Select(configEntry => new ConfigEntry(logsetHash, fileLastModifiedTimestamp, configEntry.Key, configEntry.Value)).ToList();
        }

        /// <summary>
        /// Retrieves process information for all workers in the given logset.
        /// </summary>
        /// <returns>Collection of process topology information for the given logset.</returns>
        public ICollection<ConfigProcessInfo> GetConfigProcessInfo()
        {
            // Retrieve process & port information for each worker.
            var workerDetails = new List<ConfigProcessInfo>();
            for (int i = 0; i < workerHostnameMap.Count; i++)
            {
                workerDetails.AddRange(GetWorkerDetails(i));
            }

            // Retrieve pgsql process information, which works slightly differently.
            workerDetails.AddRange(GetPostgresProcessDetails());

            return workerDetails;
        }

        #endregion Public Methods

        #region Protected Methods

        protected BsonDocument LoadConfigDocument()
        {
            IMongoCollection<BsonDocument> configCollection = mongoDatabase.GetCollection<BsonDocument>(ParserConstants.ConfigCollectionName);

            // Get a handle on the config.
            var query = MongoQueryHelper.GetConfig(configCollection);
            return configCollection.Find(query).FirstOrDefault();
        }

        /// <summary>
        /// Retrieves all worker topology information for a worker with a given index.
        /// </summary>
        /// <param name="workerIndex">The index of the worker, e.g. 0 for worker0.</param>
        /// <returns>All worker topology information for a worker with a given index.</returns>
        protected IEnumerable<ConfigProcessInfo> GetWorkerDetails(int workerIndex)
        {
            // Get a list of all config keys for this worker.
            var workerDetails = new List<ConfigProcessInfo>();
            var workerKeyRoot = String.Concat("worker", workerIndex, ".");
            var workerKeys = config.Keys.Where(key => key.StartsWith(workerKeyRoot, StringComparison.InvariantCultureIgnoreCase)).ToList();

            // Construct a set of unique process names.
            var processNames = new HashSet<string>();
            foreach (var workerKey in workerKeys)
            {
                var processKey = workerKey.Substring(workerKeyRoot.Length);
                if (processKey.Contains('.'))
                {
                    var processName = processKey.Substring(0, processKey.IndexOf('.'));
                    processNames.Add(processName);
                }
            }

            // Get the details for each unique process on this worker.
            foreach (var processName in processNames)
            {
                var workerDetailsForProcess = ExtractProcessInfo(workerIndex, processName);
                workerDetails.AddRange(workerDetailsForProcess);
            }

            return workerDetails;
        }

        /// <summary>
        /// Retrieves Postgres topology information.  Postgres process information is stored a little differently in the workgroup.yml file, so we need to handle it differently.
        /// </summary>
        protected IEnumerable<ConfigProcessInfo> GetPostgresProcessDetails()
        {
            string pgsqlProcessName = "pgsql";
            var postgresProcessInfo = new List<ConfigProcessInfo>();

            int workerCount = workerHostnameMap.Count;

            for (int postgresProcessIndex = 0; postgresProcessIndex < workerCount; postgresProcessIndex++)
            {
                string pgsqlHostKey = String.Format("{0}{1}.host", pgsqlProcessName, postgresProcessIndex);
                string pgsqlPortKey = String.Format("{0}{1}.port", pgsqlProcessName, postgresProcessIndex);

                if (config.ContainsKey(pgsqlPortKey) && workerCount > 0)
                {
                    int port;
                    bool parsedPort = Int32.TryParse(config[pgsqlPortKey], out port);

                    string hostname = "(Unknown)";
                    int? workerIndex = null;

                    if (workerCount == 1)
                    {
                        // Single node deployments don't store out the pgsqlX.host key, but we can safely assume that postgres lives on the -only- hostname.
                        hostname = workerHostnameMap[0];
                        workerIndex = 0;
                    }
                    else if (workerCount > 1) // HA
                    {
                        // If workgroup.yml has a pgsqlX.host entry, use it.
                        if (config.ContainsKey(pgsqlHostKey))
                        {
                            hostname = config[pgsqlHostKey];
                            workerIndex = GetWorkerIndexOfHostname(hostname);
                        }
                        // Otherwise try to get pgsql.host from tabsvc.yml.  We have to do this when multiple nodes are used and no failover node is specified.
                        else
                        {
                            hostname = GetHostnameFromTabSvcYml();
                            workerIndex = GetWorkerIndexOfHostname(hostname);
                        }
                    }

                    if (parsedPort && workerIndex.HasValue)
                    {
                        postgresProcessInfo.Add(new ConfigProcessInfo(logsetHash, fileLastModifiedTimestamp, hostname, pgsqlProcessName, workerIndex.Value.ToString(), port));
                    }
                }
            }

            return postgresProcessInfo;
        }

        protected string GetHostnameFromTabSvcYml()
        {
            BsonDocument tabSvcYml = GetTabSvcYml();
            if (tabSvcYml.Contains("pgsql") && tabSvcYml["pgsql"].AsBsonDocument.Contains("host"))
            {
                return tabSvcYml.GetPath("pgsql.host").AsString;
            }
            else
            {
                return "(Unknown)";
            }
        }

        protected BsonDocument GetTabSvcYml()
        {
            IMongoCollection<BsonDocument> configCollection = mongoDatabase.GetCollection<BsonDocument>(ParserConstants.ConfigCollectionName);
            var query = MongoQueryHelper.GetTabSvcYml(configCollection);
            BsonDocument tabSvcDocument = configCollection.Find(query).FirstOrDefault();

            return tabSvcDocument["contents"].AsBsonDocument;
        }

        /// <summary>
        /// Performs a reverse lookup on the hostname dictionary to retrieve the worker index for a given hostname
        /// </summary>
        /// <param name="hostname">The hostname of the worker.</param>
        /// <returns>Worker index for given hostname, or null if no match is found.</returns>
        protected int? GetWorkerIndexOfHostname(string hostname)
        {
            foreach (var workerHostnameMapEntry in workerHostnameMap)
            {
                if (workerHostnameMapEntry.Value.Equals(hostname, StringComparison.InvariantCultureIgnoreCase))
                {
                    return workerHostnameMapEntry.Key;
                }
            }

            return null;
        }

        /// <summary>
        /// Extracts process configuration info for a given process on a given worker.
        /// </summary>
        /// <param name="workerIndex">The index of the worker, e.g. 0 for worker0.</param>
        /// <param name="processName">The name of the process.</param>
        /// <returns>Collection of discovered process configuration information.</returns>
        protected IEnumerable<ConfigProcessInfo> ExtractProcessInfo(int workerIndex, string processName)
        {
            // Retrieve the hostname for a worker with the given index.
            var hostname = GetHostname(workerIndex);

            // Get process count.
            var processCount = GetProcessCount(workerIndex, processName);

            // Get port.
            var processPort = GetPort(workerIndex, processName);

            // Sanity check.
            if (!processCount.HasValue || !processPort.HasValue)
            {
                return new List<ConfigProcessInfo>();
            }

            ICollection<ConfigProcessInfo> results = new List<ConfigProcessInfo>();
            for (int i = 0; i < processCount; i++)
            {
                var configProcessInfo = new ConfigProcessInfo(logsetHash, fileLastModifiedTimestamp, hostname, processName, workerIndex.ToString(), processPort.Value + i);
                results.Add(configProcessInfo);
            }

            return results;
        }

        /// <summary>
        /// Retrieves the process count for a given process on a given worker.
        /// </summary>
        /// <param name="workerIndex">The index of the worker, e.g. 0 for worker0.</param>
        /// <param name="processName">The name of the process.</param>
        /// <returns>The process count for the given process on the given worker, or null if none is found.</returns>
        protected int? GetProcessCount(int workerIndex, string processName)
        {
            string processKeyPrefix = String.Format("worker{0}.{1}", workerIndex, processName).ToLowerInvariant();
            string procsKey = String.Format("{0}.procs", processKeyPrefix);
            string enabledKey = String.Format("{0}.enabled", processKeyPrefix);

            // Get number of procs. We have two cases to deal with here: instances where the number of procs
            // is called out, and instances where all we know is whether it is enabled or not.
            int processCount = 0;
            bool retrievedProcessCount = false;
            if (config.ContainsKey(procsKey))
            {
                retrievedProcessCount = Int32.TryParse(config[procsKey], out processCount);
            }
            else if (config.ContainsKey(enabledKey))
            {
                bool processIsEnabled;
                bool parsedEnabledField = Boolean.TryParse(config[enabledKey], out processIsEnabled);

                if (parsedEnabledField && processIsEnabled)
                {
                    processCount = 1;
                    retrievedProcessCount = true;
                }
            }

            if (!retrievedProcessCount)
            {
                return null;
            }

            return processCount;
        }

        /// <summary>
        /// Retrieves the hostname for the given worker.
        /// </summary>
        /// <param name="workerIndex">The index of the worker, e.g. 0 for worker0.</param>
        /// <returns>The hostname for the given worker, or null if none is found.</returns>
        protected string GetHostname(int workerIndex)
        {
            string hostnameKey = String.Format("worker{0}.host", workerIndex);

            if (!config.ContainsKey(hostnameKey))
            {
                return null;
            }

            return config[hostnameKey];
        }

        /// <summary>
        /// Retrieves the port for a given process on a given worker.
        /// </summary>
        /// <param name="workerIndex">The index of the worker, e.g. 0 for worker0.</param>
        /// <param name="processName"></param>
        /// <returns>The port for the given process on the given worker, or null if none is found.</returns>
        protected int? GetPort(int workerIndex, string processName)
        {
            string processKeyPrefix = String.Format("worker{0}.{1}", workerIndex, processName).ToLowerInvariant();
            string portKey = String.Format("{0}.port", processKeyPrefix);

            int? processPort = null;
            if (config.ContainsKey(portKey))
            {
                int portValue;
                bool hasPortValue = Int32.TryParse(config[portKey], out portValue);
                if (hasPortValue)
                {
                    processPort = portValue;
                }
            }
            else
            {
                // Certain services only have a globally configured port.  If we don't have a worker-specific port, try checking for a global port before giving up.
                var globalPortKey = String.Format("{0}.port", processName);
                if (config.ContainsKey(globalPortKey))
                {
                    int portValue;
                    bool hasPortValue = Int32.TryParse(config[globalPortKey], out portValue);
                    if (hasPortValue)
                    {
                        processPort = portValue;
                    }
                }
            }

            return processPort;
        }

        /// <summary>
        /// Recursively descends through the BSON tree and "flattens" the nested config entries into a set of key-value pairs.
        /// </summary>
        /// <param name="element">The root BsonElement to traverse.</param>
        /// <param name="key">The current key name.</param>
        /// <returns>List of key/value pairs for config entries, e.g. "gateway.timeout | 3600".</returns>
        protected List<KeyValuePair<string, string>> GetConfigEntriesFromElement(BsonElement element, string key = "")
        {
            var entries = new List<KeyValuePair<string, string>>();

            key += element.Name;

            if (element.Value.IsBsonDocument)
            {
                var nestedDocument = (BsonDocument)element.Value;
                foreach (var nestedDocumentElement in nestedDocument.Elements)
                {
                    entries.AddRange(GetConfigEntriesFromElement(nestedDocumentElement, key + "."));
                }
            }
            else if (element.Value.IsBsonNull)
            {
                entries.Add(new KeyValuePair<string, string>(key, null));
            }
            else if (element.Value.IsBsonArray)
            {
                entries.Add(new KeyValuePair<string, string>(key, String.Join(",", element.Value.AsBsonArray)));
            }
            else
            {
                entries.Add(new KeyValuePair<string, string>(key, element.Value.AsString));
            }

            return entries;
        }

        #endregion Protected Methods
    }
}