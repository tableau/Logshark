using System;
using ServiceStack.DataAnnotations;
using Tableau.RestApi.Models;

namespace Logshark.Controller.Metadata.Run
{
    public class LogsharkPublishedWorkbookMetadata
    {
        private readonly LogsharkRunMetadata runMetadata;

        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; set; }

        [Index]
        [References(typeof(LogsharkRunMetadata))]
        public int LogsharkRunMetadataId
        {
            get { return runMetadata.Id; }
        }

        public string PluginName { get; set; }

        public string Uri { get; set; }

        public string Hostname { get; set; }

        public int Port { get; set; }

        public string PublishingUsername { get; set; }

        public string SiteId { get; set; }

        public string SiteName { get; set; }

        public string ProjectId { get; set; }

        public string ProjectName { get; set; }

        public string WorkbookId { get; set; }

        public string WorkbookName { get; set; }

        public string Tags { get; set; }

        public LogsharkPublishedWorkbookMetadata()
        {
        }

        public LogsharkPublishedWorkbookMetadata(LogsharkRequest request, LogsharkRunMetadata runMetadata, PublishedWorkbookResult publishedWorkbook)
        {
            this.runMetadata = runMetadata;
            PluginName = publishedWorkbook.Request.PluginName;
            Uri = publishedWorkbook.Uri.ToString();
            Hostname = request.Configuration.TableauConnectionInfo.Hostname;
            Port = request.Configuration.TableauConnectionInfo.Port;
            PublishingUsername = request.Configuration.TableauConnectionInfo.Username;
            SiteId = publishedWorkbook.Request.SiteId;
            SiteName = publishedWorkbook.Request.SiteName;
            ProjectId = publishedWorkbook.Request.ProjectId;
            ProjectName = publishedWorkbook.Request.ProjectName;
            WorkbookId = publishedWorkbook.WorkbookId;
            WorkbookName = publishedWorkbook.Request.WorkbookName;
            Tags = String.Join(",", publishedWorkbook.Request.Tags);
        }
    }
}