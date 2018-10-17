namespace Logshark.Core.Controller.Parsing
{
    internal class DocumentWriteResult
    {
        public DocumentWriteResultType Result { get; }

        public string ErrorMessage { get; }

        public DocumentWriteResult(DocumentWriteResultType isSuccessful, string errorMessage = "")
        {
            Result = isSuccessful;
            ErrorMessage = errorMessage;
        }
    }
}