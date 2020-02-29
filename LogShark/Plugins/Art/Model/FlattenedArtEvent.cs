using System;
using LogShark.Containers;
using LogShark.Plugins.Shared;
using Newtonsoft.Json;

namespace LogShark.Plugins.Art.Model
{
    public class FlattenedArtEvent : BaseEvent
    {
        private readonly ArtData _artData;
        private readonly NativeJsonLogsBaseEvent _baseEvent;

        public int ProcessId => _baseEvent.ProcessId;
        public string RequestId => _baseEvent.RequestId;
        public string SessionId => _baseEvent.SessionId;
        public string Site => _baseEvent.Site;
        public string ThreadId => _baseEvent.ThreadId;
        public string Username => _baseEvent.Username;

        public string Details => _baseEvent?.EventPayload?.ToString(Formatting.None);

        public DateTime? ArtBeginTime => _artData?.BeginTimestamp; 
        public string ArtCustomAttributes => _artData?.CustomAttributes;
        public int? ArtDepth => _artData?.Depth;
        public string ArtDescription => _artData?.Description;
        public double? ArtElapsedSeconds => _artData?.ElapsedSeconds;
        public DateTime? ArtEndTime => _artData?.EndTimestamp;
        public string ArtId => _artData?.UniqueId;
        public string ArtName => _artData?.Name;
        public string ArtRootId => _artData?.RootId;
        public string ArtResultKey => _artData?.ResultKey ?? _artData?.ResultKeyOldFormat;
        public string ArtResultValue => _artData?.ResultValue?.ToString(Formatting.None) ?? _artData?.ResultValueOldFormat;
        public string ArtSponsorId => _artData?.SponsorId;
        public string ArtType => _artData?.Type;
        public string ArtView => _baseEvent?.ContextMetrics?.View ?? _artData?.View ?? _artData?.ViewOldFormat;
        public string ArtWorkbook => _baseEvent?.ContextMetrics?.Workbook ?? _artData?.Workbook ?? _artData?.WorkbookOldFormat;
        
        public double? ArtAllocatedBytesThisActivity => ParseNullableScientificDouble(_artData?.ResourceConsumptionMetrics?.AllocatedMemoryMetrics?.BytesThisActivity);
        public double? ArtAllocatedBytesThisActivityPlusSponsored => ParseNullableScientificDouble(_artData?.ResourceConsumptionMetrics?.AllocatedMemoryMetrics?.BytesThisActivityPlusSponsored);
        public double? ArtAllocatedBytesMaxThisActivity => ParseNullableScientificDouble(_artData?.ResourceConsumptionMetrics?.AllocatedMemoryMetrics?.MaxThisActivity);
        public long? ArtNumberOfAllocationsThisActivity => _artData?.ResourceConsumptionMetrics?.AllocatedMemoryMetrics?.NumberOfAllocationsThisActivity;
        public long? ArtNumberOfAllocationsThisActivityPlusSponsored => _artData?.ResourceConsumptionMetrics?.AllocatedMemoryMetrics?.NumberOfAllocationsThisActivityPlusSponsored;
        
        public double? ArtReleasedBytesThisActivity => ParseNullableScientificDouble(_artData?.ResourceConsumptionMetrics?.FreedMemoryMetrics?.BytesThisActivity);
        public double? ArtReleasedBytesThisActivityPlusSponsored => ParseNullableScientificDouble(_artData?.ResourceConsumptionMetrics?.FreedMemoryMetrics?.BytesThisActivityPlusSponsored);
        public long? ArtNumberOfReleasesThisActivity => _artData?.ResourceConsumptionMetrics?.FreedMemoryMetrics?.NumberOfReleasesThisActivity;
        public long? ArtNumberOfReleasesThisActivityPlusSponsored => _artData?.ResourceConsumptionMetrics?.FreedMemoryMetrics?.NumberOfReleasesThisActivityPlusSponsored;
        
        public long? ArtKernelCpuTimeThisActivityMilliseconds => _artData?.ResourceConsumptionMetrics?.KernelSpaceCpuMetrics?.CpuTimeThisActivityMilliseconds;
        public long? ArtKernelCpuTimeThisActivityPlusSponsoredMilliseconds => _artData?.ResourceConsumptionMetrics?.KernelSpaceCpuMetrics?.CpuTimeThisActivityPlusSponsoredMilliseconds;
        
        public long? ArtUserCpuTimeThisActivityMilliseconds => _artData?.ResourceConsumptionMetrics?.UserSpaceCpuMetrics?.CpuTimeThisActivityMilliseconds;
        public long? ArtUserCpuTimeThisActivityPlusSponsoredMilliseconds => _artData?.ResourceConsumptionMetrics?.UserSpaceCpuMetrics?.CpuTimeThisActivityPlusSponsoredMilliseconds;
        
        public int? ArtNumberOfThreadsActivityRanOn => _artData?.ResourceConsumptionMetrics?.NumberOfThreadsActivityRanOn;

        public FlattenedArtEvent(ArtData artData, NativeJsonLogsBaseEvent baseEvent, LogLine logLine) : base(logLine, baseEvent.Timestamp)
        {
            _artData = artData;
            _baseEvent = baseEvent;
        }

        private static double? ParseNullableScientificDouble(object number)
        {
            switch (number)
            {
                case double numberAsDouble:
                    return numberAsDouble;
                case string numberAsString:
                    var success = double.TryParse(numberAsString, out var value);
                    return success
                        ? value
                        : (double?) null;
                default:
                    return null;
            }
        }
    }
}