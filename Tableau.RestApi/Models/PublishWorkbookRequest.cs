using System.Collections.Generic;
using System.IO;

namespace Tableau.RestApi.Models
{
    /// <summary>
    /// Encapsulates state about a published workbook.
    /// </summary>
    public class PublishWorkbookRequest
    {
        public string FilePath { get; protected set; }
        public string WorkbookName { get; protected set; }

        public string PluginName { get; set; }
        public string SiteId { get; set; }
        public string SiteName { get; set; }
        public string ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string DatasourceUserName { get; set; }
        public string DatasourcePassword { get; set; }
        public ISet<string> Tags { get; set; }
        public bool OverwriteExistingWorkbook { get; set; }
        public bool ShowSheetsAsTabs { get; set; }

        public PublishWorkbookRequest(string filePath)
        {
            FilePath = filePath;
            WorkbookName = Path.GetFileName(filePath);
            Tags = new SortedSet<string>();
        }
    }
}