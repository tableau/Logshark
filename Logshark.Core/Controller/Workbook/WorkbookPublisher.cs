using log4net;
using Logshark.Common.Extensions;
using Logshark.ConnectionModel.Postgres;
using Logshark.ConnectionModel.TableauServer;
using Logshark.Core.Exceptions;
using Logshark.PluginModel.Model;
using Logshark.RequestModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Tableau.RestApi;
using Tableau.RestApi.Model;

namespace Logshark.Core.Controller.Workbook
{
    /// <summary>
    /// Contains functionality for publishing Tableau workbooks to a remote Tableau Server instance.
    /// </summary>
    public class WorkbookPublisher
    {
        protected LogsharkRequest logsharkRequest;
        protected TableauServerConnectionInfo tableauConnectionInfo;
        protected PostgresConnectionInfo postgresConnectionInfo;

        // We cache the following for efficiency, since retrieving these requires a round-trip to Tableau Server.
        protected string siteId;

        protected string projectId;
        protected string groupId;
        protected bool hasSetProjectPermissions;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public WorkbookPublisher(LogsharkRequest logsharkRequest)
        {
            this.logsharkRequest = logsharkRequest;
            tableauConnectionInfo = logsharkRequest.Configuration.TableauConnectionInfo;
            postgresConnectionInfo = logsharkRequest.Configuration.PostgresConnectionInfo;
        }

        #region Public Methods

        public void PublishWorkbooks(IPluginResponse pluginResponse)
        {
            ICollection<string> workbooksToPublish = pluginResponse.WorkbooksOutput;

            // Short circuit out if we have no work to do.
            if (!workbooksToPublish.Any())
            {
                return;
            }

            Log.InfoFormat("Publishing {0} {1} to Tableau Server..", workbooksToPublish.Count, "workbook".Pluralize(workbooksToPublish.Count));
            ICollection<PublishedWorkbookResult> publishedWorkbookResults = new List<PublishedWorkbookResult>();
            try
            {
                publishedWorkbookResults.AddRange(PublishWorkbooks(pluginResponse.PluginName, workbooksToPublish));
            }
            catch (Exception ex)
            {
                throw new PublishingException(ex.Message, ex);
            }

            // Display summary and append results to the request run context.
            Log.Info(BuildPublishingSummary(publishedWorkbookResults));
            logsharkRequest.RunContext.PublishedWorkbooks.AddRange(publishedWorkbookResults);

            // If we had any publishing failures, we want to alert the user.
            if (publishedWorkbookResults.Any(result => !result.IsSuccessful))
            {
                IEnumerable<string> failedWorkbookNames = publishedWorkbookResults.Where(result => !result.IsSuccessful)
                                                                                  .Select(result => result.Request.WorkbookName);
                string errorMessage = String.Format("The following workbooks failed to publish: {0}", String.Join(", ", failedWorkbookNames));
                throw new PublishingException(errorMessage);
            }
        }

        public static string BuildPublishingSummary(ICollection<PublishedWorkbookResult> publishedWorkbookResults)
        {
            StringBuilder summary = new StringBuilder();

            // Display results summary.
            int failureCount = publishedWorkbookResults.Count(result => !result.IsSuccessful);
            int successCount = publishedWorkbookResults.Count - failureCount;

            summary.AppendFormat("Successfully published {0} {1}! [{2} {3}]",
                                successCount, "workbook".Pluralize(successCount), failureCount, "failure".Pluralize(failureCount));

            foreach (var publishedWorkbookResult in publishedWorkbookResults)
            {
                if (publishedWorkbookResult.IsSuccessful)
                {
                    summary.AppendFormat("\n - {0}: {1}", publishedWorkbookResult.Request.WorkbookName, publishedWorkbookResult.Uri);
                }
                else
                {
                    summary.AppendFormat("\n - {0}: Failed to publish!", publishedWorkbookResult.Request.WorkbookName);
                }
            }

            return summary.ToString();
        }

        #endregion Public Methods

        #region Protected Methods

        protected ICollection<PublishedWorkbookResult> PublishWorkbooks(string pluginName, IEnumerable<string> workbooksToPublish)
        {
            var requestor = new RestApiRequestor(tableauConnectionInfo.ToUri(), tableauConnectionInfo.Username, tableauConnectionInfo.Password, tableauConnectionInfo.Site);

            try
            {
                InitializeProject(requestor);
            }
            catch (Exception ex)
            {
                throw new PublishingException(String.Format("Unable to initialize Tableau Server for publishing: {0}", ex.Message), ex);
            }

            // Publish all the workbooks.
            var publishedWorkbookResults = new List<PublishedWorkbookResult>();
            foreach (var workbook in workbooksToPublish)
            {
                PublishedWorkbookResult publishedWorkbookResult = PublishWorkbook(requestor, pluginName, workbook);
                publishedWorkbookResults.Add(publishedWorkbookResult);
            }

            return publishedWorkbookResults;
        }

        protected void InitializeProject(RestApiRequestor requestor)
        {
            // Get all of the information we'll need to compose our publishing requests, and cache it for future publishing attempts.
            if (String.IsNullOrWhiteSpace(siteId))
            {
                siteId = requestor.GetSiteId();
            }
            if (String.IsNullOrWhiteSpace(projectId))
            {
                projectId = requestor.GetOrCreateProject(logsharkRequest.ProjectName, BuildProjectDescription());
            }
            if (String.IsNullOrWhiteSpace(groupId))
            {
                groupId = requestor.GetGroupId(CoreConstants.DEFAULT_PROJECT_PERMISSIONS_GROUP);
            }
            if (!hasSetProjectPermissions)
            {
                requestor.AddDefaultProjectPermissions(projectId, groupId, CoreConstants.DEFAULT_PROJECT_PERMISSIONS);
                hasSetProjectPermissions = true;
            }
        }

        protected PublishedWorkbookResult PublishWorkbook(RestApiRequestor requestor, string pluginName, string workbookPath, bool overwriteExistingWorkbook = true)
        {
            var workbookFilename = Path.GetFileName(workbookPath);

            // Tag the workbook with the name of the plugin that generated it.
            logsharkRequest.WorkbookTags.Add(pluginName);

            Log.InfoFormat("Publishing workbook '{0}' to {1}..", workbookFilename, tableauConnectionInfo.ToUri());

            var publishWorkbookRequest = new PublishWorkbookRequest(workbookPath)
            {
                PluginName = pluginName,
                SiteId = siteId,
                SiteName = tableauConnectionInfo.Site,
                ProjectId = projectId,
                ProjectName = logsharkRequest.ProjectName,
                DatasourceUserName = postgresConnectionInfo.Username,
                DatasourcePassword = postgresConnectionInfo.Password,
                Tags = logsharkRequest.WorkbookTags,
                OverwriteExistingWorkbook = overwriteExistingWorkbook,
                ShowSheetsAsTabs = true,
                publishingTimeoutSeconds = tableauConnectionInfo.PublishingTimeoutSeconds
            };

            PublishedWorkbookResult result = requestor.PublishWorkbookWithEmbeddedCredentials(publishWorkbookRequest);
            int attemptsMade = 1;

            while (!result.IsSuccessful && attemptsMade < CoreConstants.WORKBOOK_PUBLISHING_MAX_ATTEMPTS)
            {
                Log.WarnFormat("Workbook publishing attempt #{0} failed.  Retrying in {1} {2}..",
                                attemptsMade, CoreConstants.WORKBOOK_PUBLISHING_RETRY_DELAY_SEC, "second".Pluralize(CoreConstants.WORKBOOK_PUBLISHING_RETRY_DELAY_SEC));
                Thread.Sleep(1000 * CoreConstants.WORKBOOK_PUBLISHING_RETRY_DELAY_SEC);

                result = requestor.PublishWorkbookWithEmbeddedCredentials(publishWorkbookRequest);
                attemptsMade++;
            }

            if (!result.IsSuccessful)
            {
                Log.ErrorFormat("Publishing of workbook '{0}' failed: {1} [{2} {3} made]", workbookFilename, result.ErrorMessage, attemptsMade, "attempt".Pluralize(attemptsMade));
            }

            return result;
        }

        /// <summary>
        /// Generates a project description for the project where workbooks will be published.
        /// </summary>
        /// <returns>Project description for the project where workbooks will be published. </returns>
        protected string BuildProjectDescription()
        {
            if (!String.IsNullOrWhiteSpace(logsharkRequest.ProjectDescription))
            {
                return logsharkRequest.ProjectDescription;
            }
            else
            {
                IEnumerable<string> pluginsExecuted = logsharkRequest.RunContext.PluginTypesToExecute.Select(pluginType => pluginType.Name);

                var sb = new StringBuilder();
                sb.AppendFormat("Generated from logset <b>'{0}'</b> on {1} by {2}.<br>", logsharkRequest.Target.OriginalTarget, DateTime.Now.ToString("M/d/yy"), Environment.UserName);
                sb.Append("<br>");
                sb.AppendFormat(" Logset Hash: <b>{0}</b><br>", logsharkRequest.RunContext.LogsetHash);
                sb.AppendFormat(" Postgres DB: <b>{0}</b><br>", logsharkRequest.PostgresDatabaseName);
                sb.AppendFormat(" Plugins Run: <b>{0}</b>", String.Join(", ", pluginsExecuted));
                return sb.ToString();
            }
        }

        #endregion Protected Methods
    }
}