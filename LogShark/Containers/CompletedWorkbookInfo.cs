using System;
using LogShark.Writers.Containers;

namespace LogShark.Containers
{
    public class CompletedWorkbookInfo
    {
        public bool HasAnyData { get; }
        public string FinalWorkbookName { get; }
        public string OriginalWorkbookName { get; }
        public string WorkbookPath { get; }

        public bool GeneratedSuccessfully => Exception == null;
        public Exception Exception { get; }

        public static CompletedWorkbookInfo GetFailedInfo(PackagedWorkbookTemplateInfo packagedWorkbookTemplateInfo, Exception exception)
        {
            return new CompletedWorkbookInfo(packagedWorkbookTemplateInfo, packagedWorkbookTemplateInfo.Name, null, false, exception);
        }

        public CompletedWorkbookInfo(string originalWorkbookName, string finalWorkbookName, string workbookPath, bool hasAnyData, Exception exception = null)
        {
            OriginalWorkbookName = originalWorkbookName;
            FinalWorkbookName = finalWorkbookName;
            WorkbookPath = workbookPath;
            HasAnyData = hasAnyData;
            Exception = exception;
        }

        public CompletedWorkbookInfo(PackagedWorkbookTemplateInfo packagedWorkbookTemplateInfo, string finalWorkbookName, string workbookPath, bool hasAnyData, Exception exception = null)
            : this(packagedWorkbookTemplateInfo.Name, finalWorkbookName, workbookPath, hasAnyData, exception)
        { }
    }
}