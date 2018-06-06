namespace Logshark.Core.Controller.Processing
{
    internal class LogsetProcessingStatus
    {
        public ProcessedLogsetState State { get; protected set; }

        public long? ProcessedDataVolumeBytes { get; protected set; }

        public LogsetProcessingStatus(ProcessedLogsetState state, long? processedDataVolumeBytes = null)
        {
            State = state;
            ProcessedDataVolumeBytes = processedDataVolumeBytes;
        }
    }
}
