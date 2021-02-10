using System;
using LogShark.Containers;
using LogShark.Exceptions;
using LogShark.Writers.Containers;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using LogShark.Shared;
using Polly;
using Tools.TableauServerRestApi;
using Tools.TableauServerRestApi.Containers;
using Tools.TableauServerRestApi.Enums;
using Tools.TableauServerRestApi.Exceptions;
using Tools.TableauServerRestApi.Models;
using static Tools.TableauServerRestApi.Containers.PublishWorkbookRequest;

namespace LogShark.Writers
{
    public class WorkbookPublisher : IWorkbookPublisher
    {
        private static readonly Dictionary<CapabilityName, CapabilityMode> Permissions = new Dictionary<CapabilityName, CapabilityMode>
        {
            { CapabilityName.ExportData, CapabilityMode.Allow },
            { CapabilityName.ExportImage, CapabilityMode.Allow },
            { CapabilityName.ExportXml, CapabilityMode.Allow },
            { CapabilityName.Filter, CapabilityMode.Allow },
            { CapabilityName.Read, CapabilityMode.Allow },
            { CapabilityName.ViewUnderlyingData, CapabilityMode.Allow },
        };

        private readonly LogSharkConfiguration _config;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly PublisherSettings _publisherSettings;
        private readonly DataSourceCredentials _dbCreds;

        public WorkbookPublisher(LogSharkConfiguration config, PublisherSettings publisherSettings, ILoggerFactory loggerFactory)
        : this (config, publisherSettings, null, loggerFactory) { }

        public WorkbookPublisher(LogSharkConfiguration config, PublisherSettings publisherSettings, DataSourceCredentials creds, ILoggerFactory loggerFactory)
        {
            _config = config;
            _publisherSettings = publisherSettings;
            _dbCreds = creds;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<WorkbookPublisher>();
        }

        public async Task<PublisherResults> PublishWorkbooks(
            string projectName,
            IEnumerable<CompletedWorkbookInfo> completedWorkbooks,
            IEnumerable<string> workbookTags)
        {
            var tableauServerInfo = _publisherSettings.TableauServerInfo;

            try
            {
                using var restApi = await Retry.DoWithRetries<RestApiException, TableauServerRestApi>(
                    "WorkbookPublisher -> Init TS API",
                    _logger,
                    () => TableauServerRestApi.InitApi(tableauServerInfo, _loggerFactory.CreateLogger<TableauServerRestApi>()),
                    60,
                    120);

                var projectInfo = await Retry.DoWithRetries<RestApiException, ProjectInfo>(
                    "WorkbookPublisher -> Create Project",
                    _logger,
                    async () =>
                    {
                        projectName = await FindUniqueProjectName(projectName, restApi);
                        var projectDescription = GetProjectDescription();

                        var newProjectInfo = await restApi.CreateProject(projectName, projectDescription, _publisherSettings.ParentProjectInfo.Id, _publisherSettings.ParentProjectInfo.Name);
                        await SetDefaultPermissions(restApi, newProjectInfo);
                        return newProjectInfo;
                    });
                
                var tags = workbookTags?.ToList();
                var publishingResults = completedWorkbooks
                    .Select(cwi => PublishWorkbook(projectInfo.Id, cwi, restApi, tags).Result) // .Result here ensures that we publish one workbook at a time instead of trying to push 10+ of them at the same time
                    .ToList();

                return new PublisherResults(tableauServerInfo.Url, tableauServerInfo.Site, restApi.ContentUrl, projectInfo.Name, publishingResults);
            }
            catch (RestApiException ex)
            {
                _logger.LogError("Exception encountered when attempting to sign in or create the project: {exceptionMessage}", ex.Message);
                return new PublisherResults(tableauServerInfo.Url, tableauServerInfo.Site, null, projectName, null, ex);
            }
        }

        private static async Task<string> FindUniqueProjectName(string projectName, TableauServerRestApi restApi)
        {
            if (await restApi.GetSiteProjectByName(projectName) == null)
            {
                return projectName;
            }

            var suffix = 2;
            while (await restApi.GetSiteProjectByName($"{projectName}-{suffix}") != null)
            {
                suffix++;
            }

            return $"{projectName}-{suffix}";
        }

        private async Task<WorkbookPublishResult> PublishWorkbook(string projectId, CompletedWorkbookInfo completedWorkbookInfo, TableauServerRestApi restApi, IList<string> tags)
        {
            if (!completedWorkbookInfo.GeneratedSuccessfully)
            {
                return WorkbookPublishResult.Fail(
                    completedWorkbookInfo.OriginalWorkbookName,
                    new WorkbookPublishingException($"Workbook {completedWorkbookInfo.OriginalWorkbookName} was not generated successfully. Skipping publishing", completedWorkbookInfo.Exception));
            }

            var publishWorkbookRequest = new PublishWorkbookRequest(
                completedWorkbookInfo.WorkbookPath,
                projectId,
                completedWorkbookInfo.FinalWorkbookName,
                overwriteExistingWorkbook: true)
            {
                Credentials = _dbCreds,
            };

            WorkbookPublishResult workbookPublishResult;
            try
            {
                var retryOnExceptionPolicy = Policy
                    .Handle<RestApiException>()
                    .WaitAndRetryAsync(
                        new []
                        {
                            TimeSpan.FromSeconds(30),
                            TimeSpan.FromSeconds(60), 
                        },
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogDebug("Got an exception trying to publish workbook `{failedWorkbookName}`. This is retry number {retryCount}. Exception was: `{exceptionMessage}`", completedWorkbookInfo.FinalWorkbookName ?? "null", retryCount, exception?.Message ?? "null");
                    });
                
                var handleExceptionPolicy = Policy<WorkbookPublishResult>
                    .Handle<RestApiException>(ex => ex.IsTimeoutException)
                    .FallbackAsync(WorkbookPublishResult.Timeout(completedWorkbookInfo.FinalWorkbookName));
                    
                workbookPublishResult = await retryOnExceptionPolicy
                    .WrapAsync(handleExceptionPolicy)
                    .ExecuteAsync(async () =>
                    {
                        var workbookInfo = await restApi.PublishWorkbook(publishWorkbookRequest);
                        return WorkbookPublishResult.Success(completedWorkbookInfo.FinalWorkbookName, workbookInfo);
                    });
            }
            catch (RestApiException ex)
            {
                var errorMessage = $"Failed to publish workbook `{completedWorkbookInfo.WorkbookPath}` after multiple retries. Exception was: {ex.Message}";
                _logger.LogError(errorMessage);
                return WorkbookPublishResult.Fail(completedWorkbookInfo.OriginalWorkbookName, new WorkbookPublishingException(errorMessage));
            }

            if (workbookPublishResult.PublishState == WorkbookPublishResult.WorkbookPublishState.Success &&
                _publisherSettings.ApplyPluginProvidedTagsToWorkbooks 
                && tags != null 
                && tags.Count > 0)
            {
                await AddTagsToWorkbook(workbookPublishResult, tags, restApi);
            }

            if (workbookPublishResult.PublishState == WorkbookPublishResult.WorkbookPublishState.Success)
            {
                _logger.LogDebug("Workbook `{publishedWorkbookName}` was published to Tableau Server as ID `{newWorkbookId}`", publishWorkbookRequest.WorkbookNameOnTableauServer, workbookPublishResult.PublishedWorkbookId);
            }
            else
            {
                _logger.LogDebug("Publishing attempt for workbook `{originalWorkbookName}` returned non-successful publishing status. Status: {publishingStatus}", publishWorkbookRequest.WorkbookNameOnTableauServer, workbookPublishResult.PublishState.ToString());
            }

            return workbookPublishResult;
        }

        private async Task SetDefaultPermissions(TableauServerRestApi restApi, ProjectInfo projectInfo)
        {
            var groupsToProvideWithDefaultPermissions = _publisherSettings.GroupsToProvideWithDefaultPermissions;
            if (groupsToProvideWithDefaultPermissions == null || groupsToProvideWithDefaultPermissions.Count == 0)
            {
                return;
            }

            _logger.LogDebug("Attempting to set default workbook permissions for {groupsCount} groups on project {projectName}", groupsToProvideWithDefaultPermissions.Count, projectInfo.Name);

            var groups = await restApi.GetSiteGroups();
            var groupsToUpdate = groups.Where(group => groupsToProvideWithDefaultPermissions.Contains(group.Name)).ToList();

            foreach (var groupInfo in groupsToUpdate)
            {
                await restApi.AddDefaultWorkbookPermissions(projectInfo.Id, groupInfo.Id, Permissions);
                _logger.LogDebug("Added default workbook permissions for {groupName} on project {projectName}", groupInfo.Name, projectInfo.Name);
            }

            if (groupsToUpdate.Count == 0)
            {
                var requestedGroupsAsString = string.Join("; ", groupsToProvideWithDefaultPermissions);
                _logger.LogWarning("Configuration requested to set default workbook permissions for {groupsCount} group(s), however none of them were found on Tableau Server. Groups requested: {groupsRequested}", groupsToProvideWithDefaultPermissions.Count, requestedGroupsAsString);
            }
            else if (groupsToUpdate.Count < groupsToProvideWithDefaultPermissions.Count)
            {
                var missingGroups = groupsToProvideWithDefaultPermissions.Except(groupsToUpdate.Select(group => group.Name));
                var missingGroupsAsString = string.Join("; ", missingGroups);
                _logger.LogWarning("Configuration requested to set default workbook permissions for {groupsCount} group(s), however some of the groups were not found. Missing groups {missingGroups}", groupsToProvideWithDefaultPermissions.Count, missingGroupsAsString);
            }
            else
            {
                _logger.LogInformation("Successfully set default workbook permissions on {groupCount} group(s)", groupsToProvideWithDefaultPermissions.Count);
            }
        }
        
        private string GetProjectDescription()
        {
            var projectDescription = new StringBuilder();
            projectDescription.Append($"Generated from log set <b>'{_config.OriginalLocation}'</b> on {DateTime.Now:M/d/yy} by {Environment.UserName}.<br>");
            projectDescription.Append("<br>");
            projectDescription.Append($"Original filename: {HttpUtility.HtmlEncode(_config.OriginalFileName)}");
            projectDescription.Append("<br>");
            
            var pluginText = string.IsNullOrEmpty(_config.RequestedPlugins)
                ? "<b>All</b> (default value when required plugins are not explicitly specified)"
                : $"<b>{_config.RequestedPlugins}</b>";
            projectDescription.Append($"Plugins Requested: {pluginText}");

            if (!string.IsNullOrEmpty(_config.TableauServerProjectDescriptionFooterHtml))
            {
                projectDescription.Append("<br>");
                projectDescription.Append(_config.TableauServerProjectDescriptionFooterHtml);
            }
            
            return projectDescription.ToString();
        }

        private async Task AddTagsToWorkbook(WorkbookPublishResult workbookInfo, IList<string> tagsToAdd, TableauServerRestApi restApi)
        {
            try
            {
                await Retry.DoWithRetries<RestApiException, IList<TagInfo>>(
                    nameof(WorkbookPublisher),
                    _logger,
                    async () => await restApi.AddTagsToWorkbook(workbookInfo.PublishedWorkbookId, tagsToAdd));
            }
            catch (RestApiException ex)
            {
                _logger.LogWarning(
                    "Failed to add tags to workbook '{workbookName}' ({workbookId}). Exception message: {exceptionAddingTags}",
                    workbookInfo.PublishedWorkbookName ?? "(null)",
                    workbookInfo.PublishedWorkbookId ?? "(null)",
                    ex.Message);
            }
        }
    }
}
