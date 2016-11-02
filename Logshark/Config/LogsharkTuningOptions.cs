using System;

namespace Logshark.Config
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
            FilePartitionerThresholdMb = configTuningOptions.FilePartitioner.MaxFileSizeMB;
            FileProcessorConcurrencyLimitPerCore = configTuningOptions.FileProcessor.ConcurrencyLimitPerCore;

            Validate();
        }

        public void Validate()
        {
            if (FilePartitionerConcurrencyLimit < 1)
            {
                throw new ArgumentException("Invalid tuning option: FilePartitionerConcurrencyLimit cannot be less than 1!");
            }
            if (FilePartitionerThresholdMb < 1)
            {
                throw new ArgumentException("Invalid tuning option: FilePartitionerThresholdMb cannot be less than 1!");
            }
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
