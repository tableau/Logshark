using Logshark.PluginLib.Helpers;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginLib.Persistence;
using Logshark.Plugins.Tabadmin.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Logshark.Plugins.Tabadmin
{
    /// <summary>
    /// Queries Mongo for Tableau Server version information, found in tabadmin logs. Creates a timeline of versions from the log data. Superfluous consecutive entries of the same version are removed.
    /// </summary>
    public class TabadminVersionProcessor : TabadminEventBase
    {
        // Key: Worker ID. Value: TSVersion objects belonging to that worker.
        private readonly ConcurrentDictionary<int, ConcurrentStack<TSVersion>> versionsByWorker;

        /// <summary>
        /// Execute the processing of version information and persist the results to the database.
        /// </summary>
        /// <param name="collection">A collection initialized for the "tabadmin" collection.</param>
        /// <param name="persister">A generic persister for TabadminModelBase will work.</param>
        /// <param name="pluginResponse">Logshark plugin response.</param>
        /// <param name="logsetHash">The unique fingerprint of the logset.</param>
        public static void Execute(IMongoCollection<BsonDocument> collection, IPersister<TabadminModelBase> persister, PluginResponse pluginResponse, Guid logsetHash)
        {
            var tabadminVersionProcessor = new TabadminVersionProcessor(collection, persister, pluginResponse, logsetHash);
            tabadminVersionProcessor.QueryMongo();
            List<TSVersion> reducedVersionList = tabadminVersionProcessor.ReduceVersionLists();
            tabadminVersionProcessor.AddEndDatesToVersionList(reducedVersionList);
            tabadminVersionProcessor.PersistVersionListToDatabase(reducedVersionList);
        }

        private TabadminVersionProcessor(IMongoCollection<BsonDocument> collection, IPersister<TabadminModelBase> persister, PluginResponse pluginResponse, Guid logsetHash)
            : base(collection, persister, pluginResponse, logsetHash)
        {
            versionsByWorker = new ConcurrentDictionary<int, ConcurrentStack<TSVersion>>();
        }

        private static FilterDefinition<BsonDocument> QueryTabadminVersionMessages()
        {
            return Query.Regex("message", new BsonRegularExpression(@"^====>> <script> (?<shortVersion>.+?) \(build: (?<longVersion>.+?)\):.*<<====$"));
        }

        /// <summary>
        /// Query Mongo for version-related messages and compile the results.
        /// </summary>
        private void QueryMongo()
        {
            FilterDefinition<BsonDocument> versionQuery = QueryTabadminVersionMessages();
            IAsyncCursor<BsonDocument> versionCursor = collection.Find(versionQuery).ToCursor();
            var versionTasks = new List<Task>();
            // Grab all the matching TSVersion rows from the data source and parse them into a stack of TSVersions.
            while (versionCursor.MoveNext())
            {
                versionTasks.AddRange(versionCursor.Current.Select(document => Task.Factory.StartNew(() => AddVersionToStack(document))));
            }
            Task.WaitAll(versionTasks.ToArray());
        }

        /// <summary>
        /// Populate the versionsByWorker class attribute from a mongoDocument.
        /// </summary>
        /// <param name="mongoDocument">Document from Mongo which can represent a TSVersion object.</param>
        private void AddVersionToStack(BsonDocument mongoDocument)
        {
            ConcurrentStack<TSVersion> workerVersionList;
            int worker = BsonDocumentHelper.GetInt("worker", mongoDocument);
            // Get the ConcurrentStack for this worker id if it exists. Create it if it doesn't exist.
            if (!versionsByWorker.TryGetValue(worker, out workerVersionList))
            {
                versionsByWorker[worker] = new ConcurrentStack<TSVersion>();
                workerVersionList = versionsByWorker[worker];
            }
            workerVersionList.Push(new TSVersion(mongoDocument, logsetHash));
        }

        /// <summary>
        /// Create for each worker a set of TSVersions, sorted by Timestamp, and then remove consecutive identical versions.
        /// TODO: Can this be simplified and the number of loops reduced?
        /// TODO: It would be nice to break this out a bit, maybe have two methods to do this work instead of one.
        /// </summary>
        /// <returns>A list of TSVersions, one combined list for all workers, with superfluous values removed.</returns>
        private List<TSVersion> ReduceVersionLists()
        {
            // Turn the ConcurrentStack of versions into a Dictionary of Lists, keyed by worker ID, so that the per-worker Lists can be sorted.
            // Once sorted by StartDateGmt, remove superfluous consecutive identical version numbers within each worker set.
            Dictionary<int, List<TSVersion>> workerVersionLists = new Dictionary<int, List<TSVersion>>();
            foreach (var workerList in versionsByWorker)
            {
                workerVersionLists[workerList.Key] = new List<TSVersion>();
                foreach (var workerVersions in workerList.Value)
                {
                    workerVersionLists[workerList.Key].Add(workerVersions);
                }
                workerVersionLists[workerList.Key].Sort();  // Sort the TSVersion object list by the StartDateGmt field (ascending).
            }

            // Loop through the sorted Lists by worker and remove entries with consecutive version numbers, producing a new reduced Dictionary in the process.
            Dictionary<int, List<TSVersion>> reducerDict = new Dictionary<int, List<TSVersion>>();  // Stores one list per worker, consecutive version numbers removed.
            List<TSVersion> reducedList = new List<TSVersion>();  // The final product: a combined list of version info for all workers.
            foreach (var workerVersions in workerVersionLists)
            {
                reducerDict[workerVersions.Key] = new List<TSVersion>();
                foreach (var tsVersion in workerVersions.Value)
                {
                    if ((reducerDict[workerVersions.Key].Count == 0) || (tsVersion.VersionStringLong != reducerDict[workerVersions.Key].Last().VersionStringLong))
                    {
                        reducerDict[workerVersions.Key].Add(tsVersion);
                    }
                }
                reducedList.AddRange(reducerDict[workerVersions.Key]);
            }
            return reducedList;
        }

        /// <summary>
        /// Set the EndDate and EndDateGmt field of each TSVersion object in the provided list.
        /// </summary>
        /// <param name="tsVersionList">A list of TSVersions to update.</param>
        private void AddEndDatesToVersionList(List<TSVersion> tsVersionList)
        {
            tsVersionList.Sort();
            for (int i = 0; i < tsVersionList.Count; i++)
            {
                foreach (var forwardVersion in tsVersionList.Skip(i + 1))
                {
                    if (forwardVersion.Worker == tsVersionList[i].Worker)
                    {
                        tsVersionList[i].EndDateGmt = forwardVersion.StartDateGmt;
                        tsVersionList[i].EndDate = forwardVersion.StartDate;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Write a list of TSVersions to the database.
        /// </summary>
        /// <param name="tsVersionList">A list of TSVersion items to write to the database.</param>
        private void PersistVersionListToDatabase(List<TSVersion> tsVersionList)
        {
            // Persist to database.
            var versionDBTasks = new List<Task>();
            foreach (var version in tsVersionList)
            {
                versionDBTasks.Add(Task.Factory.StartNew(() => PersistTSVersion(version)));
            }
            Task.WaitAll(versionDBTasks.ToArray());
        }

        /// <summary>
        /// Enqueue a TSVersion object to be persisted to the database.
        /// </summary>
        /// <param name="tsVersion">TSVersion object to persist.</param>
        private void PersistTSVersion(TSVersion tsVersion)
        {
            try
            {
                persister.Enqueue(tsVersion);
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Encountered an exception on {0}: {1}", tsVersion.Id, ex);
                pluginResponse.AppendError(errorMessage);
                Log.Error(errorMessage);
            }
        }
    }
}