namespace LogShark.Containers
{
    public class ProcessFileResult
    {
        public string ErrorMessage { get; }
        public ExitReason ExitReason { get; }
        public long LinesProcessed { get; }
        public bool IsSuccessful => ExitReason == ExitReason.CompletedSuccessfully;

        public ProcessFileResult(long linesProcessed, string errorMessage = null, ExitReason exitReason = ExitReason.CompletedSuccessfully)
        {
            ErrorMessage = errorMessage;
            ExitReason = exitReason;
            LinesProcessed = linesProcessed;
        }
    }
}