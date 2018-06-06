using log4net;
using Logshark.Common.Helpers;
using Logshark.ConnectionModel.Mongo;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Logshark.Core.Mongo
{
    /// <summary>
    /// Handles starting & stopping local mongod processes.
    /// </summary>
    public sealed class LocalMongoProcessManager
    {
        // MongoDB connection & process settings.
        private const int DefaultConnectionPoolSize = 200;

        private const int DefaultConnectionTimeout = 30;
        private const int DefaultInsertionRetries = 3;
        private const int DefaultStartPort = 27017;
        private const string DefaultUsername = "";
        private const string DefaultPassword = "";
        private const int MongoProcessKillTimeoutMs = 3000;

        // Paths to MongoDB assets.
        private static readonly string MongoDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "MongoDB");

        private static readonly string MongoDataDirectory = Path.Combine(MongoDirectory, "data");
        private static readonly string MongoExecutable = Path.Combine(MongoDirectory, "bin", "mongod.exe");
        private static readonly string MongoLogDirectory = Path.Combine(MongoDirectory, "logs");
        private static readonly string MongoLogPath = Path.Combine(MongoLogDirectory, "mongod.log");
        private static readonly string MongoProcessName = "mongod";

        private readonly int port;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public LocalMongoProcessManager(int portToStartOn = DefaultStartPort)
        {
            port = portToStartOn;
        }

        #region Public Methods

        /// <summary>
        /// Starts a local mongod process, if requested.
        /// </summary>
        public Process StartMongoProcess(bool purgeDataOnStartup = false)
        {
            if (IsMongoRunning())
            {
                Log.InfoFormat("A MongoDB process is already running.  Attempting to shut it down..");
                KillAllMongoProcesses();
            }

            if (purgeDataOnStartup)
            {
                PurgeData();
            }

            // Ensure data directory exists.
            if (!Directory.Exists(MongoDataDirectory))
            {
                Log.DebugFormat("Creating MongoDB data directory at {0}", MongoDataDirectory);
                try
                {
                    Directory.CreateDirectory(MongoDataDirectory);
                }
                catch (Exception ex)
                {
                    Log.ErrorFormat("Failed to create MongoDB data directory at '{0}':{1}", MongoDataDirectory, ex.Message);
                    throw new MongoException(ex.Message, ex);
                }
            }

            // Ensure log directory exists
            if (!Directory.Exists(MongoLogDirectory))
            {
                Log.DebugFormat("Creating MongoDB log directory at {0}", MongoLogDirectory);
                try
                {
                    Directory.CreateDirectory(MongoLogDirectory);
                }
                catch (Exception ex)
                {
                    Log.ErrorFormat("Failed to create MongoDB log directory at '{0}':{1}", MongoLogDirectory, ex.Message);
                    throw new MongoException(ex.Message, ex);
                }
            }

            // Start Mongo.
            Log.InfoFormat("Starting local MongoDB instance on port {0}..", port);
            ProcessStartInfo mongoProcessStartInfo = BuildMongoProcessStartInfo(port);
            Log.DebugFormat("MongoDB Process start arguments: {0}", mongoProcessStartInfo.Arguments);
            try
            {
                Process mongoProcess = Process.Start(mongoProcessStartInfo);

                // Validate the process started correctly.
                if (!IsMongoRunning())
                {
                    throw new MongoException("Issued mongod process start, but process is not running.  This could be due to a permissions error.");
                }

                Log.Info("MongoDB started successfully!");
                return mongoProcess;
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Failed to start MongoDB process: {0}", ex.Message);
                throw new MongoException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Retrieve the connection information for the local mongod process.
        /// </summary>
        /// <returns>MongoConnectionInfo object that can be used to connect to the local mongod process.</returns>
        public MongoConnectionInfo GetConnectionInfo()
        {
            ICollection<MongoServerAddress> servers = new List<MongoServerAddress>();
            servers.Add(new MongoServerAddress("localhost", port));

            return new MongoConnectionInfo(servers, DefaultUsername, DefaultPassword, DefaultConnectionPoolSize, DefaultConnectionTimeout, DefaultInsertionRetries);
        }

        /// <summary>
        /// Deletes the contents of the Mongo data directory.
        /// </summary>
        public bool PurgeData()
        {
            Log.Info("Purging existing MongoDB data..");

            // Bail out if data directory doesn't exist.
            if (!Directory.Exists(MongoDataDirectory))
            {
                return false;
            }

            // Delete all files and subfolders within the root extraction location.
            try
            {
                if (IsMongoRunning())
                {
                    KillAllMongoProcesses();
                }

                DirectoryHelper.DeleteDirectory(MongoDataDirectory);
                return true;
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Failed to purge MongoDB data: {0}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Indicates whether a mongod process is currently running.
        /// </summary>
        /// <returns>True if at least one mongod process is running.</returns>
        public bool IsMongoRunning()
        {
            return GetRunningMongoProcesses().Any();
        }

        /// <summary>
        /// Kills all mongod processes on the local machine.
        /// </summary>
        public void KillAllMongoProcesses()
        {
            IEnumerable<Process> runningMongoProcesses = GetRunningMongoProcesses();
            foreach (Process runningMongoProcess in runningMongoProcesses.Where(IsMongoProcessOwnedByThisApplication))
            {
                Log.Debug("Found a running MongoDB process owned by this application.  Attempting to terminate it..");
                KillMongoProcess(runningMongoProcess);
            }
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Builds a ProcessStartInfo object that can be used to start a local mongod process.
        /// </summary>
        private static ProcessStartInfo BuildMongoProcessStartInfo(int port)
        {
            return new ProcessStartInfo
            {
                FileName = MongoExecutable,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = true,
                Verb = "runas",
                Arguments = String.Format(@"--port {0} --dbpath ""{1}"" --logpath ""{2}"" --logappend",
                                          port, MongoDataDirectory, MongoLogPath)
            };
        }

        /// <summary>
        /// Kills a mongod processes.
        /// </summary>
        private void KillMongoProcess(Process mongoProcess)
        {
            try
            {
                mongoProcess.Kill();
                mongoProcess.WaitForExit(MongoProcessKillTimeoutMs);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Failed to kill MongoDB process '{0}': {1}", mongoProcess.Id, ex.Message);
                throw;
            }

            if (!mongoProcess.HasExited)
            {
                throw new MongoException(String.Format("MongoDB process '{0}' failed to stop in a timely manner.", mongoProcess.Id));
            }
        }

        /// <summary>
        /// Returns a collection of all running mongod processes on the local machine.
        /// </summary>
        private IEnumerable<Process> GetRunningMongoProcesses()
        {
            return Process.GetProcessesByName(MongoProcessName).Where(process => !process.HasExited);
        }

        /// <summary>
        /// Indicates whether a given process was spawned by a given process' copy of mongod.
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        private bool IsMongoProcessOwnedByThisApplication(Process process)
        {
            return process.MainModule.FileName.Equals(MongoExecutable, StringComparison.OrdinalIgnoreCase);
        }

        #endregion Protected Methods
    }
}