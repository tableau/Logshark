using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using LogShark.Shared;

namespace LogShark.Plugins.TabadminController
{
    public class BuildTracker : IBuildTracker
    {
        private readonly IProcessingNotificationsCollector _processingNotificationsCollector;
        private readonly ConcurrentDictionary<long, string> _timestampsForBuilds;

        public BuildTracker(IProcessingNotificationsCollector processingNotificationsCollector)
        {
            _processingNotificationsCollector = processingNotificationsCollector;
            _timestampsForBuilds = new ConcurrentDictionary<long, string>();
        }

        public void AddBuild(DateTime timestamp, string build)
        {
            var roundedTimestamp = GetTimestampRoundedDownToMinute(timestamp);

            // Adding timestamp and also +1 and -1 minute timestamps. It is a fairly safe assumption that timestamp will not change within 2 minutes,
            // but having those extra timestamps will help us catch scenarios when log line with version was logged right before (or after) minute value changed
            AddBuildRecordSafely(roundedTimestamp, build, timestamp);
            AddBuildRecordSafely(roundedTimestamp + 60, build, timestamp); // +1 minute
            AddBuildRecordSafely(roundedTimestamp - 60, build, timestamp); // -1 minute
        }
        
        public IEnumerable<TabadminControllerBuildRecord> GetBuildRecords()
        {
            return _timestampsForBuilds.Select(kvp => new TabadminControllerBuildRecord(
                DateTimeOffset.FromUnixTimeSeconds(kvp.Key),
                kvp.Value));
        }

        private static long GetTimestampRoundedDownToMinute(DateTime dateTime)
        {
            var roundedTimestamp = new DateTimeOffset(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0, TimeSpan.Zero);
            return roundedTimestamp.ToUnixTimeSeconds();
        }

        private void AddBuildRecordSafely(long roundedTimestamp, string build, DateTime originalTimestamp)
        {
            _timestampsForBuilds.AddOrUpdate(roundedTimestamp, build, (_, currentValue) =>
            {
                if (currentValue != build)
                {
                    _processingNotificationsCollector.ReportError($"Timestamp `{originalTimestamp}` contains reference for build `{build}`, however another build - `{_timestampsForBuilds[roundedTimestamp]}` - is registered within two minutes of this event. Build output around this time could be inconsistent.", nameof(BuildTracker));
                    return currentValue;
                }
                
                return build;
            });   
        }
    }
}