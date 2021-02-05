using System;
using Tools.TableauServerRestApi.Models;

namespace LogShark.Writers.Containers
{
    public class WorkbookPublishResult
    {
        public Exception Exception { get; }
        public string OriginalWorkbookName { get; }
        public string PublishedWorkbookId { get; }
        public string PublishedWorkbookName { get; }
        public WorkbookPublishState PublishState { get; }

        private WorkbookPublishResult(
            WorkbookPublishState state,
            string originalWorkbookName,
            string publishedWorkbookId = null,
            string publishedWorkbookName = null,
            Exception exception = null)
        {
            Exception = exception;
            OriginalWorkbookName = originalWorkbookName;
            PublishedWorkbookId = publishedWorkbookId;
            PublishedWorkbookName = publishedWorkbookName;
            PublishState = state;
        }

        public static WorkbookPublishResult Fail(string originalWorkbookName, Exception exception)
        {
            return new WorkbookPublishResult(WorkbookPublishState.Fail, originalWorkbookName, null, null, exception);
        }

        public static WorkbookPublishResult Timeout(string originalWorkbookName)
        {
            return new WorkbookPublishResult(WorkbookPublishState.Timeout, originalWorkbookName);
        }
        
        public static WorkbookPublishResult Success(string originalWorkbookName, WorkbookInfo workbookInfo)
        {
            return new WorkbookPublishResult(WorkbookPublishState.Success, originalWorkbookName, workbookInfo.Id, workbookInfo.Name);
        }

        public enum WorkbookPublishState
        {
            Fail,
            Success,
            Timeout
        }
    }
}