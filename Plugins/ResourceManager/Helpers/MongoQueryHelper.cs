using log4net;
using Logshark.PluginLib.Helpers;
using Logshark.PluginLib.Logging;
using Logshark.Plugins.ResourceManager.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Logshark.Plugins.ResourceManager.Helpers
{
    public class MongoQueryHelper
    {
        private static readonly Regex CpuLimitRegex = new Regex(@".*\s(?<cpu_limit>\d+?)%.*", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        // Gets byte count by greedily capturing a numeric sequence (composed of digits and comma separators) from between the "Limit:" and "bytes\n" tokens.
        private static readonly Regex MemoryLimitRegex = new Regex(@".*Limit: (?<memory_limit>[\d,]+?) bytes$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        private static readonly FilterDefinitionBuilder<BsonDocument> Query = Builders<BsonDocument>.Filter;

        private static readonly ILog Log = PluginLogFactory.GetLogger(Assembly.GetExecutingAssembly(),
                                                                      MethodBase.GetCurrentMethod());

        public static IEnumerable<string> GetDistinctWorkers(IMongoCollection<BsonDocument> collection)
        {
            var filter = Query.Exists("worker");
            return collection.Distinct<string>("worker", filter).ToList();
        }

        public static IEnumerable<int> GetAllUniquePidsByWorker(string workerId, IMongoCollection<BsonDocument> collection)
        {
            var filter = Query.Eq("worker", workerId);
            return collection.Distinct<int>("pid", filter).ToList();
        }

        public static IList<BsonDocument> GetSrmStartEventsForWorker(string workerId, IMongoCollection<BsonDocument> collection)
        {
            var filter = Query.And(Query.Eq("worker", workerId),
                                   Query.Eq("k", "msg"),
                                   Query.Regex("v", new BsonRegularExpression("^Resource Manager: listening on port")));
            return collection.Find(filter).ToList();
        }

        public static int GetCpuLimit(BsonDocument srmStartEvent, IMongoCollection<BsonDocument> collection)
        {
            var filter = Query.And(FilterMessagesByWorkerPidAndFile(srmStartEvent),
                                   Query.Regex("v", new BsonRegularExpression("^Resource Manager: Max CPU limited to")));

            string cpuLimitString = collection.Distinct<string>("v", filter).First();

            var match = CpuLimitRegex.Match(cpuLimitString);
            if (match.Success)
            {
                return Int32.Parse(match.Groups["cpu_limit"].Value);
            }
            else
            {
                throw new Exception("No cpu limit found for " + srmStartEvent);
            }
        }

        public static long GetPerProcessMemoryLimit(BsonDocument srmStartEvent, IMongoCollection<BsonDocument> collection)
        {
            var filter = Query.And(FilterMessagesByWorkerPidAndFile(srmStartEvent),
                                   Query.Regex("v", new BsonRegularExpression("^Resource Manager: Per Process Memory Limit:")));

            string memoryLimitString = collection.Distinct<string>("v", filter).First();

            var match = MemoryLimitRegex.Match(memoryLimitString);
            if (match.Success)
            {
                return Int64.Parse(match.Groups["memory_limit"].Value.Replace(",", ""));
            }
            else
            {
                throw new Exception("No process memory limit found for " + srmStartEvent);
            }
        }

        public static long GetTotalMemoryLimit(BsonDocument srmStartEvent, IMongoCollection<BsonDocument> collection)
        {
            var filter = Query.And(FilterMessagesByWorkerPidAndFile(srmStartEvent),
                                   Query.Regex("v", new BsonRegularExpression("^Resource Manager: All Processes Memory Limit:")));

            string memoryLimitString = collection.Distinct<string>("v", filter).First();

            var match = MemoryLimitRegex.Match(memoryLimitString);
            if (match.Success)
            {
                return Int64.Parse(match.Groups["memory_limit"].Value.Replace(",", ""));
            }
            else
            {
                throw new Exception("No total memory limit found for " + srmStartEvent);
            }
        }

        public static ResourceManagerThreshold GetThreshold(BsonDocument srmStartEvent, IMongoCollection<BsonDocument> collection)
        {
            long totalMemoryLimit = GetTotalMemoryLimit(srmStartEvent, collection);
            long processMemoryLimit = GetPerProcessMemoryLimit(srmStartEvent, collection);
            int cpuLimit = GetCpuLimit(srmStartEvent, collection);
            string processName = GetProcessName(collection);

            ResourceManagerThreshold threshold = new ResourceManagerThreshold(srmStartEvent, processName, cpuLimit, processMemoryLimit, totalMemoryLimit);

            return threshold;
        }

        public static IList<ResourceManagerCpuInfo> GetCpuInfos(string workerId, int pid, IMongoCollection<BsonDocument> collection)
        {
            IList<ResourceManagerCpuInfo> cpuInfos = new List<ResourceManagerCpuInfo>();

            string processName = GetProcessName(collection);

            var filter = Query.And(FilterMessagesByWorkerAndPid(workerId, pid),
                                   Query.Regex("v", new BsonRegularExpression("^Resource Manager: CPU info:")));

            var cpuInfoDocuments = collection.Find(filter).ToList();

            foreach (var document in cpuInfoDocuments)
            {
                try
                {
                    ResourceManagerCpuInfo cpuInfo = new ResourceManagerCpuInfo(document, processName);
                    cpuInfos.Add(cpuInfo);
                }
                catch (Exception ex)
                {
                    Log.Error("Unable to parse cpuInfo from " + document, ex);
                }
            }

            return cpuInfos;
        }

        public static IList<ResourceManagerMemoryInfo> GetMemoryInfos(string workerId, int pid, IMongoCollection<BsonDocument> collection)
        {
            IList<ResourceManagerMemoryInfo> memoryInfos = new List<ResourceManagerMemoryInfo>();

            string processName = GetProcessName(collection);

            var filter = Query.And(FilterMessagesByWorkerAndPid(workerId, pid),
                                   Query.Regex("v", new BsonRegularExpression("^Resource Manager: Memory info:")));

            var memoryInfoDocuments = collection.Find(filter).ToList();

            foreach (var document in memoryInfoDocuments)
            {
                try
                {
                    ResourceManagerMemoryInfo memoryInfo = new ResourceManagerMemoryInfo(document, processName);
                    memoryInfos.Add(memoryInfo);
                }
                catch (Exception ex)
                {
                    Log.Error("Unable to parse memoryInfo from " + document, ex);
                }
            }

            return memoryInfos;
        }

        public static IList<ResourceManagerAction> GetActions(string workerId, int pid, IMongoCollection<BsonDocument> collection)
        {
            IList<ResourceManagerAction> actions = new List<ResourceManagerAction>();

            string processName = GetProcessName(collection);

            var filter = Query.And(
                                FilterMessagesByWorkerAndPid(workerId, pid),
                                Query.Or(
                                        Query.Regex("v", new BsonRegularExpression(ResourceManagerAction.CpuUsageExceededRegex)),
                                        Query.Regex("v", new BsonRegularExpression(ResourceManagerAction.ProcessMemoryUsageExceededRegex)),
                                        Query.Regex("v", new BsonRegularExpression(ResourceManagerAction.TotalMemoryUsageExceededRegex))
                                )
                         );

            var memoryInfoDocuments = collection.Find(filter).ToList();

            foreach (var document in memoryInfoDocuments)
            {
                try
                {
                    ResourceManagerAction memoryInfo = new ResourceManagerAction(document, processName);
                    actions.Add(memoryInfo);
                }
                catch (Exception ex)
                {
                    Log.Error("Unable to parse memoryInfo from " + document, ex);
                }
            }

            return actions;
        }

        public static FilterDefinition<BsonDocument> FilterMessagesByWorkerAndPid(string workerId, int pid)
        {
            return Query.And(Query.Eq("worker", workerId),
                             Query.Eq("pid", pid),
                             Query.Eq("k", "msg"));
        }

        public static FilterDefinition<BsonDocument> FilterMessagesByWorkerPidAndFile(BsonDocument document)
        {
            return Query.And(Query.Eq("worker", BsonDocumentHelper.GetString("worker", document)),
                             Query.Eq("pid", BsonDocumentHelper.GetInt("pid", document)),
                             Query.Eq("file", BsonDocumentHelper.GetString("file", document)),
                             Query.Eq("k", "msg"));
        }

        public static string GetProcessName(IMongoCollection<BsonDocument> collection)
        {
            string processName = collection.CollectionNamespace.CollectionName;

            if (processName.Contains("_"))
            {
                return processName.Split('_')[0];
            }
            else
            {
                return processName;
            }
        }
    }
}