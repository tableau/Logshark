namespace Logshark.Core.Controller.Parsing
{
    internal class DocumentWriteResult
    {
        public bool IsSuccessful { get; protected set; }

        public string ErrorMessage { get; protected set; }

        public DocumentWriteResult(bool isSuccessful, string errorMessage = "")
        {
            IsSuccessful = isSuccessful;
            ErrorMessage = errorMessage;
        }
    }
}