using System;

namespace LogShark.Writers.Containers
{
    public class WorkbookPublishResult
    {
        public Exception Exception { get; }
        public bool PublishedSuccessfully => Exception == null;
        public string PublishedWorkbookName { get; }

        public WorkbookPublishResult(string publishedWorkbookName, Exception exception = null)
        {
            Exception = exception;
            PublishedWorkbookName = publishedWorkbookName;
        }
    }
}