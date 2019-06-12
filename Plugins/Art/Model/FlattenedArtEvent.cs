using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Logshark.Plugins.Art.Model
{
    [BsonIgnoreExtraElements]
    public class FlattenedArtEvent : LogEventWithArt
    {
        [BsonIgnore] public string Details => Message?.ToJson();

        [BsonIgnore] public string ArtId => ArtData?.UniqueId;
        [BsonIgnore] public int? ArtDepth => ArtData?.Depth;
        [BsonIgnore] public string ArtName => ArtData?.Name;
        [BsonIgnore] public string ArtType => ArtData?.Type;
        [BsonIgnore] public string ArtDescription => ArtData?.Description;
        [BsonIgnore] public string ArtView => ContextMetrics?.View ?? ArtData?.View;
        [BsonIgnore] public string ArtWorkbook => ContextMetrics?.Workbook ?? ArtData?.Workbook;
        [BsonIgnore] public string ArtCustomAttributes => ArtData?.CustomAttributes;
        [BsonIgnore] public string ArtSponsorId => ArtData?.SponsorId;
        [BsonIgnore] public string ArtRootId => ArtData?.RootId;
        [BsonIgnore] public DateTime? ArtBeginTime => ArtData?.BeginTimestamp; 
        [BsonIgnore] public DateTime? ArtEndTime => ArtData?.EndTimestamp;
        [BsonIgnore] public double? ArtElapsedSeconds => ArtData?.ElapsedSeconds;
        [BsonIgnore] public string ArtResultKey => ArtData?.ResultKey;
        [BsonIgnore] public string ArtResultValue => ArtData?.ResultValue?.ToJson();
        
        [BsonIgnore] public double? ArtAllocatedBytesThisActivity => ParseNullableScientificDouble(ArtData?.ResourceConsumptionMetrics?.AllocatedMemoryMetrics?.BytesThisActivity);
        [BsonIgnore] public double? ArtAllocatedBytesThisActivityPlusSponsored => ParseNullableScientificDouble(ArtData?.ResourceConsumptionMetrics?.AllocatedMemoryMetrics?.BytesThisActivityPlusSponsored);
        [BsonIgnore] public double? ArtAllocatedBytesMaxThisActivity => ParseNullableScientificDouble(ArtData?.ResourceConsumptionMetrics?.AllocatedMemoryMetrics?.MaxThisActivity);
        [BsonIgnore] public int? ArtNumberOfAllocationsThisActivity => ArtData?.ResourceConsumptionMetrics?.AllocatedMemoryMetrics?.NumberOfAllocationsThisActivity;
        [BsonIgnore] public int? ArtNumberOfAllocationsThisActivityPlusSponsored => ArtData?.ResourceConsumptionMetrics?.AllocatedMemoryMetrics?.NumberOfAllocationsThisActivityPlusSponsored;
        
        [BsonIgnore] public double? ArtReleasedBytesThisActivity => ParseNullableScientificDouble(ArtData?.ResourceConsumptionMetrics?.FreedMemoryMetrics?.BytesThisActivity);
        [BsonIgnore] public double? ArtReleasedBytesThisActivityPlusSponsored => ParseNullableScientificDouble(ArtData?.ResourceConsumptionMetrics?.FreedMemoryMetrics?.BytesThisActivityPlusSponsored);
        [BsonIgnore] public int? ArtNumberOfReleasesThisActivity => ArtData?.ResourceConsumptionMetrics?.FreedMemoryMetrics?.NumberOfReleasesThisActivity;
        [BsonIgnore] public int? ArtNumberOfReleasesThisActivityPlusSponsored => ArtData?.ResourceConsumptionMetrics?.FreedMemoryMetrics?.NumberOfReleasesThisActivityPlusSponsored;
        
        [BsonIgnore] public int? ArtKernelCpuTimeThisActivityMilliseconds => ArtData?.ResourceConsumptionMetrics?.KernelSpaceCpuMetrics?.CpuTimeThisActivityMilliseconds;
        [BsonIgnore] public int? ArtKernelCpuTimeThisActivityPlusSponsoredMilliseconds => ArtData?.ResourceConsumptionMetrics?.KernelSpaceCpuMetrics?.CpuTimeThisActivityPlusSponsoredMilliseconds;
        
        [BsonIgnore] public int? ArtUserCpuTimeThisActivityMilliseconds => ArtData?.ResourceConsumptionMetrics?.UserSpaceCpuMetrics?.CpuTimeThisActivityMilliseconds;
        [BsonIgnore] public int? ArtUserCpuTimeThisActivityPlusSponsoredMilliseconds => ArtData?.ResourceConsumptionMetrics?.UserSpaceCpuMetrics?.CpuTimeThisActivityPlusSponsoredMilliseconds;
        
        [BsonIgnore] public int? ArtNumberOfThreadsActivityRanOn => ArtData?.ResourceConsumptionMetrics?.NumberOfThreadsActivityRanOn;

        private static double? ParseNullableScientificDouble(object number)
        {
            if (number is double)
            {
                return (double) number;
            }

            var numberAsString = number as string;
            if (numberAsString != null)
            {
                double value;
                var success = double.TryParse(numberAsString, out value);
                return success
                    ? value
                    : (double?) null;
            }
            
            return null;
        }
    }
}