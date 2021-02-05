namespace LogShark.Shared.LogReading.Containers
{
    public class FileIsZipWithLogsResult
    {
        public bool ContainsLogFiles { get; }
        public string ErrorMessage { get; }
        public bool ValidZip { get; }
        
        public static FileIsZipWithLogsResult Success()
        {
            return new FileIsZipWithLogsResult(true, true, null);
        }
        
        public static FileIsZipWithLogsResult InvalidZip(string errorMessage)
        {
            return new FileIsZipWithLogsResult(false, false, errorMessage);
        }
        
        public static FileIsZipWithLogsResult NoLogsFound()
        {
            return new FileIsZipWithLogsResult(false, true, null);
        }

        private FileIsZipWithLogsResult(bool containsLogFiles, bool validZip, string errorMessage)
        {
            ContainsLogFiles = containsLogFiles;
            ErrorMessage = errorMessage;
            ValidZip = validZip;
        }
    }
}