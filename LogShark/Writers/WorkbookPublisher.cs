using LogShark.Containers;
using LogShark.Exceptions;
using LogShark.Writers.Containers;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly PublisherSettings _publisherSettings;
        private readonly DataSourceCredentials _dbCreds;

        public WorkbookPublisher(PublisherSettings publisherSettings, ILoggerFactory loggerFactory)
        : this (publisherSettings, null, loggerFactory) { }

        public WorkbookPublisher(PublisherSettings publisherSettings, DataSourceCredentials creds, ILoggerFactory loggerFactory)
        {
            _publisherSettings = publisherSettings;
            _dbCreds = creds;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<WorkbookPublisher>();
        }

        public async Task<PublisherResults> PublishWorkbooks(
            string projectName,
            string projectDescription,
            IEnumerable<CompletedWorkbookInfo> completedWorkbooks,
            IEnumerable<string> workbookTags)
        {
            var tableauServerInfo = _publisherSettings.TableauServerInfo;

            try
            {
                using (var restApi = await TableauServerRestApi.InitApi(tableauServerInfo, _loggerFactory.CreateLogger<TableauServerRestApi>()))
                {
                    projectName = await FindUniqueProjectName(projectName, restApi);

                    var newProjectInfo = await restApi.CreateProject(projectName, projectDescription, _publisherSettings.ParentProjectInfo.Id, _publisherSettings.ParentProjectInfo.Name);
                    await SetDefaultPermissions(restApi, newProjectInfo);

                    var tags = workbookTags?.ToList();
                    var publishingResults = completedWorkbooks
                        .Select(cwi => PublishWorkbook(newProjectInfo.Id, cwi, restApi, tags).Result) // .Result here ensures that we publish one workbook at a time instead of trying to push 10+ of them at the same time
                        .ToList();

                    return new PublisherResults(tableauServerInfo.Url, tableauServerInfo.Site, restApi.ContentUrl, newProjectInfo.Name, publishingResults);
                }
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
                return new WorkbookPublishResult(
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

            WorkbookInfo workbookInfo;
            try
            {
                workbookInfo = await Retry.DoWithRetries<RestApiException, WorkbookInfo>(
                    nameof(WorkbookPublisher),
                    _logger,
                    async () => await restApi.PublishWorkbook(publishWorkbookRequest));
                _logger.LogDebug("Workbook `{publishedWorkbookName}` was published to Tableau Server as ID `{newWorkbookId}`", publishWorkbookRequest.WorkbookNameOnTableauServer, workbookInfo.Id);
            }
            catch (RestApiException ex)
            {
                var errorMessage = $"Failed to publish workbook `{completedWorkbookInfo.WorkbookPath}` after multiple retries. Exception was: {ex.Message}";
                _logger.LogError(errorMessage);
                return new WorkbookPublishResult(completedWorkbookInfo.OriginalWorkbookName, new WorkbookPublishingException(errorMessage));
            }

            if (_publisherSettings.ApplyPluginProvidedTagsToWorkbooks && tags != null && tags.Count > 0)
            {
                await AddTagsToWorkbook(workbookInfo, tags, restApi);
            }

            return new WorkbookPublishResult(workbookInfo.Name);
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

        private async Task AddTagsToWorkbook(WorkbookInfo workbookInfo, IList<string> tagsToAdd, TableauServerRestApi restApi)
        {
            try
            {
                await Retry.DoWithRetries<RestApiException, IList<TagInfo>>(
                    nameof(WorkbookPublisher),
                    _logger,
                    async () => await restApi.AddTagsToWorkbook(workbookInfo.Id, tagsToAdd));
            }
            catch (RestApiException ex)
            {
                _logger.LogWarning(
                    "Failed to add tags to workbook '{workbookName}' ({workbookId}). Exception message: {exceptionAddingTags}",
                    workbookInfo.Name ?? "(null)",
                    workbookInfo.Id ?? "(null)",
                    ex.Message);
            }
        }
    }
}
