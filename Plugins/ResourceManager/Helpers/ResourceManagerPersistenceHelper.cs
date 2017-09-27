using log4net;
using Logshark.PluginLib;
using Logshark.PluginLib.Helpers;
using Logshark.PluginLib.Logging;
using Logshark.PluginLib.Persistence;
using Logshark.PluginModel.Model;
using Logshark.Plugins.ResourceManager.Model;
using Npgsql;
using ServiceStack.OrmLite;
using System;
using System.Data;
using System.Reflection;

namespace Logshark.Plugins.ResourceManager.Helpers
{
    internal class ResourceManagerPersistenceHelper
    {
        private static readonly ILog Log = PluginLogFactory.GetLogger(Assembly.GetExecutingAssembly(), MethodBase.GetCurrentMethod());

        public static InsertionResult PersistResourceManagerInfo(IPluginRequest pluginRequest, IDbConnection dbConnection, ResourceManagerEvent resourceManagerInfo)
        {
            try
            {
                if (resourceManagerInfo is ResourceManagerCpuInfo)
                {
                    ResourceManagerCpuInfo cpuInfo = resourceManagerInfo as ResourceManagerCpuInfo;
                    cpuInfo.EventHash = GenerateCpuInfoEventHash(cpuInfo);
                    dbConnection.Insert(cpuInfo);
                }
                else if (resourceManagerInfo is ResourceManagerMemoryInfo)
                {
                    ResourceManagerMemoryInfo memoryInfo = resourceManagerInfo as ResourceManagerMemoryInfo;
                    memoryInfo.EventHash = GenerateMemoryInfoEventHash(memoryInfo);
                    dbConnection.Insert(memoryInfo);
                }
                else if (resourceManagerInfo is ResourceManagerAction)
                {
                    ResourceManagerAction actionEvent = resourceManagerInfo as ResourceManagerAction;
                    actionEvent.EventHash = GenerateActionEventHash(actionEvent);
                    dbConnection.Insert(actionEvent);
                }
                else if (resourceManagerInfo is ResourceManagerThreshold)
                {
                    ResourceManagerThreshold threshold = resourceManagerInfo as ResourceManagerThreshold;
                    threshold.EventHash = GenerateThresholdEventHash(threshold);
                    dbConnection.Insert(threshold);
                }

                return new InsertionResult
                {
                    SuccessfulInserts = 1,
                    FailedInserts = 0
                };
            }
            catch (PostgresException ex)
            {
                // Log an error only if this isn't a duplicate key exception.
                if (!ex.SqlState.Equals(PluginLibConstants.POSTGRES_ERROR_CODE_UNIQUE_VIOLATION))
                {
                    Log.ErrorFormat("Failed to persist ResourceManagerInfo event '{0}': {1}", resourceManagerInfo.EventHash, ex.Message);
                }

                return new InsertionResult
                {
                    SuccessfulInserts = 0,
                    FailedInserts = 1
                };
            }
            catch (NpgsqlException ex)
            {
                Log.ErrorFormat("Failed to persist ResourceManagerInfo event '{0}': {1}", resourceManagerInfo.EventHash, ex.Message);

                return new InsertionResult
                {
                    SuccessfulInserts = 0,
                    FailedInserts = 1
                };
            }
        }

        protected static Guid GenerateCpuInfoEventHash(ResourceManagerCpuInfo cpuInfo)
        {
            return HashHelper.GenerateHashGuid(cpuInfo.Timestamp,
                                               cpuInfo.ProcessName,
                                               cpuInfo.WorkerId,
                                               cpuInfo.ProcessCpuUtil,
                                               cpuInfo.Pid);
        }

        protected static Guid GenerateMemoryInfoEventHash(ResourceManagerMemoryInfo memoryInfo)
        {
            return HashHelper.GenerateHashGuid(memoryInfo.Timestamp,
                                               memoryInfo.ProcessName,
                                               memoryInfo.WorkerId,
                                               memoryInfo.ProcessMemoryUtil,
                                               memoryInfo.TotalMemoryUtil,
                                               memoryInfo.Pid);
        }

        protected static Guid GenerateActionEventHash(ResourceManagerAction actionEvent)
        {
            return HashHelper.GenerateHashGuid(actionEvent.Timestamp,
                                               actionEvent.ProcessName,
                                               actionEvent.WorkerId,
                                               actionEvent.CpuUtil,
                                               actionEvent.ProcessMemoryUtil,
                                               actionEvent.TotalMemoryUtil,
                                               actionEvent.Pid,
                                               actionEvent.CpuUtilTermination,
                                               actionEvent.ProcessMemoryUtilTermination,
                                               actionEvent.TotalMemoryUtilTermination);
        }

        protected static Guid GenerateThresholdEventHash(ResourceManagerThreshold threshold)
        {
            return HashHelper.GenerateHashGuid(threshold.Timestamp,
                                               threshold.ProcessName,
                                               threshold.ProcessId,
                                               threshold.WorkerId,
                                               threshold.Pid,
                                               threshold.CpuLimit,
                                               threshold.PerProcessMemoryLimit,
                                               threshold.TotalMemoryLimit);
        }
    }
}