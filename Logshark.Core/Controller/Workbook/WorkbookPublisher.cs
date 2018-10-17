using log4net;
using Logshark.Common.Extensions;
using Logshark.ConnectionModel.Postgres;
using Logshark.ConnectionModel.TableauServer;
using Logshark.Core.Exceptions;
using Logshark.PluginModel.Model;
using Optional;
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
    internal class WorkbookPublisher
    {
        // Default permissions that should be enabled on any newly-created projects in Tableau Server.
        protected static readonly IDictionary<capabilityTypeName, capabilityTypeMode> DefaultProjectPermissions = new Dictionary<capabilityTypeName, capabilityTypeMode>
        {
            { capabilityTypeName.ExportData, capabilityTypeMode.Allow },
            { capabilityTypeName.ExportXml, capabilityTypeMode.Allow },
            { capabilityTypeName.ViewUnderlyingData, capabilityTypeMode.Allow }
        };

        // Default group name to grant the default permissions to.
        protected const string DefaultProjectPermissionsGroup = "All Users";

        // The maximum number of times that publishing a single workbook will be attempted.
        protected const int WorkbookPublishingMaxAttempts = 3;

        // The delay between workbook publishing retries, in seconds.
        protected const int WorkbookPublishingRetryDelaySec = 5;

        protected readonly ITableauServerConnectionInfo tableauConnectionInfo;
        protected readonly Option<PostgresConnectionInfo> postgresConnectionInfo;
        protected readonly PublishingOptions publishingOptions;
        protected readonly IRestApiRequestor restApiRequestor;

        // We cache the following for efficiency, since retrieving these requires a round-trip to Tableau Server.
        protected string siteId;
        protected string projectId;
        protected string groupId;
        protected bool hasSetProjectPermissions;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public WorkbookPublisher(
            ITableauServerConnectionInfo tableauConnectionInfo,
            Option<PostgresConnectionInfo> postgresConnectionInfo,
            PublishingOptions publishingOptions,
            IRestApiRequestor restApiRequestor)
        {
            this.tableauConnectionInfo = tableauConnectionInfo;
            this.postgresConnectionInfo = postgresConnectionInfo;
            this.publishingOptions = publishingOptions;
            this.restApiRequestor = restApiRequestor;
        }

        #region Public Methods

        public ICollection<PublishedWorkbookResult> PublishWorkbooks(IPluginResponse pluginResponse)
        {
            ICollection<string> workbooksToPublish = pluginResponse.WorkbooksOutput;
            ICollection<PublishedWorkbookResult> publishedWorkbookResults = new List<PublishedWorkbookResult>();

            // Short circuit out if we have no work to do.
            if (!workbooksToPublish.Any())
            {
                return publishedWorkbookResults;
            }

            Log.InfoFormat("Publishing {0} {1} to Tableau Server..", workbooksToPublish.Count, "workbook".Pluralize(workbooksToPublish.Count));
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

            // If we had any publishing failures, we want to alert the user.
            if (publishedWorkbookResults.Any(result => !result.IsSuccessful))
            {
                IEnumerable<string> failedWorkbookNames = publishedWorkbookResults.Where(result => !result.IsSuccessful)
                                                                                  .Select(result => result.Request.WorkbookName);
                string errorMessage = String.Format("The following workbooks failed to publish: {0}", String.Join(", ", failedWorkbookNames));
                throw new PublishingException(errorMessage);
            }

            return publishedWorkbookResults;
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
            try
            {
                InitializeProject(restApiRequestor);
            }
            catch (Exception ex)
            {
                throw new PublishingException(String.Format("Unable to initialize Tableau Server for publishing: {0}", ex.Message), ex);
            }

            // Publish all the workbooks.
            var publishedWorkbookResults = new List<PublishedWorkbookResult>();
            foreach (var workbook in workbooksToPublish)
            {
                PublishedWorkbookResult publishedWorkbookResult = PublishWorkbook(restApiRequestor, pluginName, workbook);
                publishedWorkbookResults.Add(publishedWorkbookResult);
            }

            return publishedWorkbookResults;
        }

        protected void InitializeProject(IRestApiRequestor requestor)
        {
            // Get all of the information we'll need to compose our publishing requests, and cache it for future publishing attempts.
            if (String.IsNullOrWhiteSpace(siteId))
            {
                siteId = requestor.GetSiteId();
            }
            if (String.IsNullOrWhiteSpace(projectId))
            {
                projectId = requestor.GetOrCreateProject(publishingOptions.ProjectName, publishingOptions.ProjectDescription);
            }
            if (String.IsNullOrWhiteSpace(groupId))
            {
                groupId = requestor.GetGroupId(DefaultProjectPermissionsGroup);
            }
            if (!hasSetProjectPermissions)
            {
                requestor.AddDefaultProjectPermissions(projectId, groupId, DefaultProjectPermissions);
                hasSetProjectPermissions = true;
            }
        }

        protected PublishedWorkbookResult PublishWorkbook(IRestApiRequestor requestor, string pluginName, string workbookPath)
        {
            var workbookFilename = Path.GetFileName(workbookPath);

            Log.InfoFormat("Publishing workbook '{0}' to {1}..", workbookFilename, tableauConnectionInfo.Uri);

            var publishWorkbookRequest = new PublishWorkbookRequest(workbookPath)
            {
                PluginName = pluginName,
                SiteId = siteId,
                SiteName = tableauConnectionInfo.Site,
                ProjectId = projectId,
                ProjectName = publishingOptions.ProjectName,
                // Tag the workbook with the name of the plugin that generated it.
                Tags = new HashSet<string>(publishingOptions.Tags) { pluginName },
                OverwriteExistingWorkbook = publishingOptions.OverwriteExistingWorkbooks,
                ShowSheetsAsTabs = true,
                PublishingTimeoutSeconds = tableauConnectionInfo.PublishingTimeoutSeconds
            };

            postgresConnectionInfo.MatchSome(user =>
            {
                publishWorkbookRequest.DatasourceUserName = user.Username;
                publishWorkbookRequest.DatasourcePassword = user.Password;
            });

            PublishedWorkbookResult result = requestor.PublishWorkbookWithEmbeddedCredentials(publishWorkbookRequest);
            int attemptsMade = 1;

            while (!result.IsSuccessful && attemptsMade < WorkbookPublishingMaxAttempts)
            {
                Log.WarnFormat("Workbook publishing attempt #{0} failed.  Retrying in {1} {2}..",
                                attemptsMade, WorkbookPublishingRetryDelaySec, "second".Pluralize(WorkbookPublishingRetryDelaySec));
                Thread.Sleep(1000 * WorkbookPublishingRetryDelaySec);

                result = requestor.PublishWorkbookWithEmbeddedCredentials(publishWorkbookRequest);
                attemptsMade++;
            }

            if (!result.IsSuccessful)
            {
                Log.ErrorFormat("Publishing of workbook '{0}' failed: {1} [{2} {3} made]", workbookFilename, result.ErrorMessage, attemptsMade, "attempt".Pluralize(attemptsMade));
            }

            return result;
        }

        #endregion Protected Methods
    }
}