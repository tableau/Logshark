using Logshark.Config;
using System;

namespace Logshark.RequestModel.Config
{
    /// <summary>
    /// Contains configuration information for various Logshark tuning options.
    /// </summary>
    public class LogsharkTuningOptions
    {
        public int FilePartitionerConcurrencyLimit { get; protected set; }
        public int FilePartitionerThresholdMb { get; protected set; }
        public int FileProcessorConcurrencyLimitPerCore { get; protected set; }

        public LogsharkTuningOptions(TuningOptions configTuningOptions)
        {
            FilePartitionerConcurrencyLimit = configTuningOptions.FilePartitioner.ConcurrencyLimit;
            if (FilePartitionerConcurrencyLimit < 1)
            {
                throw new ArgumentException("Invalid tuning option: FilePartitionerConcurrencyLimit cannot be less than 1!");
            }

            FilePartitionerThresholdMb = configTuningOptions.FilePartitioner.MaxFileSizeMB;
            if (FilePartitionerThresholdMb < 1)
            {
                throw new ArgumentException("Invalid tuning option: FilePartitionerThresholdMb cannot be less than 1!");
            }

            FileProcessorConcurrencyLimitPerCore = configTuningOptions.FileProcessor.ConcurrencyLimitPerCore;
            if (FileProcessorConcurrencyLimitPerCore < 1)
            {
                throw new ArgumentException("Invalid tuning option: FileProcessorConcurrencyLimitPerCore cannot be less than 1!");
            }
        }

        public override string ToString()
        {
            return String.Format("FilePartitionerThresholdMb:{0}, FileProcessorConcurrencyLimitPerCore:{1}",
                                  FilePartitionerThresholdMb, FileProcessorConcurrencyLimitPerCore);
        }
    }
}