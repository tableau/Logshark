using Logshark.PluginLib.Extensions;
using Logshark.Plugins.Tabadmin.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;

namespace Logshark.Plugins.Tabadmin
{
    /// <summary>
    /// Queries Mongo for Tableau Server version information, found in tabadmin logs. Creates a timeline of versions from the log data. Superfluous consecutive entries of the same version are removed.
    /// </summary>
    internal static class TabadminVersionProcessor
    {
        public static IEnumerable<TableauServerVersion> BuildVersionTimeline(IMongoCollection<BsonDocument> collection)
        {
            var versionsByWorker = QueryVersionsByWorker(collection);
            var distinctConsecutiveVersions = RemoveConsecutiveWorkerVersions(versionsByWorker);
            return UpdateEndDates(distinctConsecutiveVersions);
        }

        private static IDictionary<string, IList<TableauServerVersion>> QueryVersionsByWorker(IMongoCollection<BsonDocument> collection)
        {
            var versionsByWorker = new Dictionary<string, IList<TableauServerVersion>>(); // Key: Worker ID. Value: TableauServerVersion objects belonging to that worker.

            var query = BuildTabadminVersionQuery();

            var versions = collection.Find(query).ToEnumerable();
            foreach (var version in versions)
            {
                string worker = version.GetString("worker");

                if (!versionsByWorker.ContainsKey(worker))
                {
                    versionsByWorker[worker] = new List<TableauServerVersion>();
                }

                versionsByWorker[worker].Add(new TableauServerVersion(version));
            }

            return versionsByWorker;
        }

        private static FilterDefinition<BsonDocument> BuildTabadminVersionQuery()
        {
            return Builders<BsonDocument>.Filter.Regex("message", new BsonRegularExpression(@"^====>> <script> (?<shortVersion>.+?) \(build: (?<longVersion>.+?)\):.*<<====$"));
        }

        private static IDictionary<string, IList<TableauServerVersion>> RemoveConsecutiveWorkerVersions(IDictionary<string, IList<TableauServerVersion>> versionsByWorker)
        {
            var updatedVersionsByWorker = new Dictionary<string, IList<TableauServerVersion>>();

            foreach (string worker in versionsByWorker.Keys)
            {
                var updatedVersionsForWorker = versionsByWorker[worker].OrderBy(version => version.StartDateGmt)
                                                                       .Aggregate(new List<TableauServerVersion>(), (current, next) =>
                                                                       {
                                                                           if (!current.Any() || current.Last().VersionLong != next.VersionLong)
                                                                           {
                                                                               current.Add(next);
                                                                           }

                                                                           return current;
                                                                       });

                updatedVersionsByWorker.Add(worker, new List<TableauServerVersion>(updatedVersionsForWorker));
            }

            return updatedVersionsByWorker;
        }

        /// <summary>
        /// Set the EndDate and EndDateGmt field of each TableauServerVersion object in the provided list.
        /// </summary>
        private static IEnumerable<TableauServerVersion> UpdateEndDates(IDictionary<string, IList<TableauServerVersion>> versionsByWorker)
        {
            return versionsByWorker.Select(workerVersions => workerVersions.Value.OrderBy(version => version.StartDateGmt))
                                   .SelectMany(versions => versions.Aggregate(new List<TableauServerVersion>(), (current, next) =>
                                                                   {
                                                                       if (current.Any())
                                                                       {
                                                                           current.Last().EndDate = next.StartDate;
                                                                           current.Last().EndDateGmt = next.StartDateGmt;
                                                                       }

                                                                       current.Add(next);
                                                                       return current;
                                                                   }));
        }
    }
}