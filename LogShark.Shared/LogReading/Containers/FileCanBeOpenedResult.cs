namespace LogShark.Shared.LogReading.Containers
{
    public class FileCanBeOpenedResult
    {
        public string ErrorMessage { get; }
        public bool FileCanBeOpened => string.IsNullOrWhiteSpace(ErrorMessage);

        public static FileCanBeOpenedResult Success()
        {
            return new FileCanBeOpenedResult(null);
        }
        
        public static FileCanBeOpenedResult Failure(string errorMessage)
        {
            return new FileCanBeOpenedResult(errorMessage);
        }

        private FileCanBeOpenedResult(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }
    }
}