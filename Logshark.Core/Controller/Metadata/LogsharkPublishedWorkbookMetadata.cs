using Logshark.ConnectionModel.TableauServer;
using ServiceStack.DataAnnotations;
using System;
using Tableau.RestApi.Model;

namespace Logshark.Core.Controller.Metadata
{
    internal sealed class LogsharkPublishedWorkbookMetadata
    {
        private readonly LogsharkRunMetadata runMetadata;

        public string Hostname { get; set; }

        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; set; }

        public bool? IsSuccessful { get; set; }

        [Index]
        [References(typeof(LogsharkRunMetadata))]
        public int LogsharkRunMetadataId
        {
            get { return runMetadata.Id; }
        }

        public string PluginName { get; set; }

        public int Port { get; set; }

        public string ProjectId { get; set; }

        public string ProjectName { get; set; }

        public string PublishingErrorMessage { get; set; }

        public string PublishingUsername { get; set; }

        public string SiteId { get; set; }

        public string SiteName { get; set; }

        public string Tags { get; set; }

        public string Uri { get; set; }

        public string WorkbookId { get; set; }

        public string WorkbookName { get; set; }

        public LogsharkPublishedWorkbookMetadata()
        {
        }

        public LogsharkPublishedWorkbookMetadata(PublishedWorkbookResult publishedWorkbook, LogsharkRunMetadata runMetadata, TableauServerConnectionInfo tableauConnectionInfo)
        {
            this.runMetadata = runMetadata;
            Hostname = tableauConnectionInfo.Hostname;
            IsSuccessful = publishedWorkbook.IsSuccessful;
            PluginName = publishedWorkbook.Request.PluginName;
            Port = tableauConnectionInfo.Port;
            ProjectId = publishedWorkbook.Request.ProjectId;
            ProjectName = publishedWorkbook.Request.ProjectName;
            PublishingErrorMessage = publishedWorkbook.ErrorMessage;
            PublishingUsername = tableauConnectionInfo.Username;
            SiteId = publishedWorkbook.Request.SiteId;
            SiteName = publishedWorkbook.Request.SiteName;
            Tags = String.Join(",", publishedWorkbook.Request.Tags);
            if (publishedWorkbook.Uri != null)
            {
                Uri = publishedWorkbook.Uri.ToString();
            }
            WorkbookId = publishedWorkbook.WorkbookId;
            WorkbookName = publishedWorkbook.Request.WorkbookName;
        }
    }
}