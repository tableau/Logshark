namespace LogShark.Containers
{
    public class ProcessStreamResult
    {
        public string ErrorMessage { get; }
        public ExitReason ExitReason { get; }
        public long LinesProcessed { get; }

        public ProcessStreamResult(long linesProcessed, string errorMessage = null, ExitReason exitReason = ExitReason.CompletedSuccessfully)
        {
            ErrorMessage = errorMessage;
            ExitReason = exitReason;
            LinesProcessed = linesProcessed;
        }
    }
}