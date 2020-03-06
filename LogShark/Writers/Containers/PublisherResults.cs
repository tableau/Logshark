using System;
using System.Collections.Generic;

namespace LogShark.Writers.Containers
{
    public class PublisherResults
    {
        public bool CreatedProjectSuccessfully => ExceptionCreatingProject == null;
        public Exception ExceptionCreatingProject { get; }
        public string ProjectName { get; }
        public string PublishedProjectUrl => GetProjectUrl();
        public List<WorkbookPublishResult> PublishedWorkbooksInfo { get; }
        public string TableauServerContentUrl { get; }
        public string TableauServerSite { get; }
        public string TableauServerUrl { get; }
        
        public PublisherResults(
            string tableauServerUrl,
            string tableauServerSite,
            string tableauServerContentUrl,
            string projectName, 
            List<WorkbookPublishResult> publishedWorkbooksInfo,
            Exception exceptionCreatingProject = null)
        {
            TableauServerUrl = tableauServerUrl;
            TableauServerSite = tableauServerSite;
            TableauServerContentUrl = tableauServerContentUrl;
            ProjectName = projectName;
            PublishedWorkbooksInfo = publishedWorkbooksInfo;
            ExceptionCreatingProject = exceptionCreatingProject;
        }

        private string GetProjectUrl()
        {
            if (!CreatedProjectSuccessfully)
            {
                return null;
            }
            
            return TableauServerSite.Equals(string.Empty, StringComparison.OrdinalIgnoreCase)
                ? $"{TableauServerUrl}#/projects?search={ProjectName}"
                : $"{TableauServerUrl}#/site/{TableauServerContentUrl}/projects?search={ProjectName}";
        }
    }
}