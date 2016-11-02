using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Tableau.RestApi.Extensions;
using Tableau.RestApi.Helpers;
using Tableau.RestApi.Models;

namespace Tableau.RestApi
{
    /// <summary>
    /// Contains methods for common actions against the Tableau Server REST API.
    /// </summary>
    public class RestApiRequestor
    {
        protected Uri baseUri;
        protected string userName;
        protected string password;
        protected string siteName;
        protected string siteId;
        protected string authToken;

        public RestApiRequestor(Uri baseUri, string userName, string password, string siteName)
        {
            this.baseUri = baseUri;
            this.userName = userName;
            this.password = password;
            this.siteName = siteName;
        }

        #region Public Methods

        public PublishedWorkbookResult PublishWorkbookWithEmbeddedCredentials(PublishWorkbookRequest publishRequest)
        {
            PublishedWorkbookResult publishedWorkbookResult = new PublishedWorkbookResult(publishRequest);

            // Construct URI & compose request payload.
            var uri = Endpoints.GetPublishWorkbookUri(baseUri, publishRequest.SiteId, publishRequest.OverwriteExistingWorkbook);
            tsRequest requestPayload = new tsRequest
            {
                Item = new workbookType
                {
                    name = Path.GetFileNameWithoutExtension(publishRequest.FilePath),
                    showTabs = publishRequest.ShowSheetsAsTabs,
                    showTabsSpecified = publishRequest.ShowSheetsAsTabs,
                    project = new projectType
                    {
                        id = publishRequest.ProjectId
                    },
                    connectionCredentials = new connectionCredentialsType
                    {
                        name = publishRequest.DatasourceUserName,
                        password = publishRequest.DatasourcePassword,
                        embed = true,
                        embedSpecified = true
                    }
                }
            };

            // Construct multipart request body using a boundary string to delimit sections.
            var boundaryString = Guid.NewGuid().ToString().Replace("-", "");
            string contentType = String.Format("multipart/mixed; boundary={0}", boundaryString);
            byte[] requestBody = PublishRequestBuilder.BuildRequestBody(publishRequest.FilePath, requestPayload, boundaryString);

            // Issue request.
            var errorMessage = String.Format("Failed to publish workbook '{0}'", publishRequest.WorkbookName);
            ApiRequest apiRequest = new ApiRequest(uri, HttpMethod.Post, null, requestBody, GetAuthToken(), contentType);
            try
            {
                tsResponse response = apiRequest.TryIssueRequest(errorMessage);

                publishedWorkbookResult.IsSuccessful = true;
                publishedWorkbookResult.WorkbookId = response.GetWorkbook().id;
                publishedWorkbookResult.Uri = GetWorkbookUrl(response.GetWorkbook().contentUrl);
            }
            catch (Exception ex)
            {
                publishedWorkbookResult.IsSuccessful = false;
                publishedWorkbookResult.ErrorMessage = ex.Message;
            }

            // Add any tags to the newly-published workbook. We swallow any errors here.
            if (publishedWorkbookResult.IsSuccessful)
            {
                try
                {
                    AddTagsToWorkbook(publishedWorkbookResult.WorkbookId, publishRequest.Tags);
                }
                catch { }
            }

            return publishedWorkbookResult;
        }

        /// <summary>
        /// Deletes a workbook from the server.
        /// </summary>
        /// <param name="workbookId">The ID of the workbook to be deleted.</param>
        public void DeleteWorkbook(string workbookId)
        {
            var uri = Endpoints.GetDeleteWorkbookUri(baseUri, GetSiteId(), workbookId);

            var errorMessage = String.Format("Failed to delete workbook '{0}'", workbookId);
            ApiRequest request = new ApiRequest(uri, HttpMethod.Delete, null, null, GetAuthToken());
            request.TryIssueRequest(errorMessage);
        }

        /// <summary>
        /// Adds tags to an existing workbook.
        /// </summary>
        /// <param name="workbookId">The Workbook ID of the workbook to add tags to.</param>
        /// <param name="tags">A list of tags to add to the workbook.</param>
        public void AddTagsToWorkbook(string workbookId, ISet<string> tags)
        {
            if (tags.Count == 0)
            {
                return;
            }

            var uri = Endpoints.GetAddTagsToWorkbookUri(baseUri, GetSiteId(), workbookId);

            // Copy tags into tag array type.
            tagType[] tagArray = new tagType[tags.Count];
            for (int i = 0; i < tags.Count; i++)
            {
                tagType tag = new tagType { label = tags.ToList()[i] };
                tagArray[i] = tag;
            }

            // Construct payload.
            tsRequest requestPayload = new tsRequest()
            {
                Item = new tagListType()
                {
                    tag = tagArray
                }
            };

            // Issue request.
            var errorMessage = "Failed to add tags to workbook";
            ApiRequest request = new ApiRequest(uri, HttpMethod.Put, null, requestPayload.SerializeBody(), authToken);
            request.TryIssueRequest(errorMessage);
        }

        /// <summary>
        /// Queries for a project by name.  If no matching project is found, it will be created.
        /// </summary>
        /// <param name="projectName">The name of the project to search for.</param>
        /// <param name="createdProjectDescription">If we are creating the project, the description to set.</param>
        /// <returns>Project ID for queried/created project.</returns>
        public string GetOrCreateProject(string projectName, string createdProjectDescription = Constants.DefaultCreatedProjectDescription)
        {
            // Try to get the project ID.  If it exists, just return it and we're done.  Otherwise, we'll need to create a new project.
            var projectId = GetProjectId(projectName);
            if (!String.IsNullOrWhiteSpace(projectId))
            {
                return projectId;
            }
            else
            {
                return CreateProject(projectName, createdProjectDescription);
            }
        }

        /// <summary>
        /// Creates a new project with the given name and description.
        /// </summary>
        /// <param name="projectName">The name of the project to create.</param>
        /// <param name="projectDescription">The description of the project to create.</param>
        /// <returns>The Project ID of the project that was created.</returns>
        public string CreateProject(string projectName, string projectDescription = Constants.DefaultCreatedProjectDescription)
        {
            // Get Site ID for the site we'll create a project in and use it to construct endpoint uri.
            var uri = Endpoints.GetCreateProjectUri(baseUri, GetSiteId());

            // Construct payload.
            tsRequest requestPayload = new tsRequest()
            {
                Item = new projectType()
                {
                    name = projectName,
                    description = projectDescription
                }
            };

            // Issue request.
            var errorMessage = String.Format("Failed to create project '{0}' on site '{1}'", projectName, siteName);
            ApiRequest request = new ApiRequest(uri, HttpMethod.Post, null, requestPayload.SerializeBody(), authToken);
            tsResponse response = request.TryIssueRequest(errorMessage);

            // Return ID of created project.
            projectType createdProject = response.GetProject();
            return createdProject.id;
        }

        /// <summary>
        /// Retrieves the name and description details for a given project.
        /// </summary>
        /// <param name="projectId">The ID of the project to retrieve the description for.</param>
        /// <returns>The name and description for the given project ID.</returns>
        public Tuple<string, string> GetProjectNameAndDescription(string projectId)
        {
            Uri uri = Endpoints.GetUpdateProjectUri(baseUri, GetSiteId(), projectId);

            // Construct payload.
            tsRequest requestPayload = new tsRequest { Item = new projectType() };

            // Issue request.
            var errorMessage = String.Format("Failed to retrieve project description for project '{0}' in site '{1}'", projectId, siteName);
            ApiRequest request = new ApiRequest(uri, HttpMethod.Put, null, requestPayload.SerializeBody(), GetAuthToken());
            tsResponse response = request.TryIssueRequest(errorMessage);

            projectType project = response.GetProject();
            return new Tuple<string, string>(project.name, project.description);
        }

        /// <summary>
        /// Retrieves the project ID for the given project, or null if no match is found.
        /// </summary>
        /// <param name="projectName">The name of the project to query the ID of.</param>
        /// <returns>The Project ID for the inputted project name.</returns>
        public string GetProjectId(string projectName)
        {
            int currentPage = 1;
            bool morePagesToCheck = true;
            while (morePagesToCheck)
            {
                // Construct URI specific to the current page of data to fetch.
                var uri = Endpoints.GetQueryProjectsUri(baseUri, GetSiteId(), currentPage);

                // Issue request.
                var errorMessage = String.Format("Failed to retrieve project list for site '{0}'", siteName);
                ApiRequest request = new ApiRequest(uri, HttpMethod.Get, null, null, GetAuthToken());
                tsResponse response = request.TryIssueRequest(errorMessage);

                // Rip project names out of response and check for a match.
                projectListType projectList = response.GetProjectList();
                foreach (var project in projectList.project)
                {
                    if (project.name == projectName)
                    {
                        return project.id;
                    }
                }

                // If we've read all the project names in and still haven't found a match, give up.  Otherwise check the next page.
                paginationType paginationData = response.GetPaginationType();
                if (currentPage * Constants.MaxResponsePageSize >= Convert.ToInt32(paginationData.totalAvailable))
                {
                    morePagesToCheck = false;
                }
                currentPage++;
            }

            // No match found.
            return null;
        }

        /// <summary>
        /// Updates the project name & description fields for a given project.
        /// </summary>
        /// <param name="projectId">The ID of the existing project to update.</param>
        /// <param name="newProjectName">The new project name to set.</param>
        /// <param name="newProjectDescription">The new project description to set.</param>
        public void UpdateProject(string projectId, string newProjectName, string newProjectDescription)
        {
            Uri uri = Endpoints.GetUpdateProjectUri(baseUri, GetSiteId(), projectId);

            // Construct payload.
            tsRequest requestPayload = new tsRequest
            {
                Item = new projectType
                {
                    name = newProjectName,
                    description = newProjectDescription
                }
            };

            // Issue request.
            var errorMessage = String.Format("Failed to update project in site '{0}'", siteName);
            ApiRequest request = new ApiRequest(uri, HttpMethod.Put, null, requestPayload.SerializeBody(), GetAuthToken());
            request.TryIssueRequest(errorMessage);
        }

        /// <summary>
        /// Retrieves the group ID for the given group, or null if no match is found.
        /// </summary>
        /// <param name="groupName">The name of the group to query the ID of.</param>
        /// <returns>The Group ID for the inputted group name.</returns>
        public string GetGroupId(string groupName)
        {
            int currentPage = 1;
            bool morePagesToCheck = true;
            while (morePagesToCheck)
            {
                // Construct URI specific to the current page of data to fetch.
                var uri = Endpoints.GetQueryGroupsUri(baseUri, GetSiteId(), currentPage);

                // Issue request.
                var errorMessage = String.Format("Failed to retrieve group list for site '{0}'", siteName);
                ApiRequest request = new ApiRequest(uri, HttpMethod.Get, null, null, GetAuthToken());
                tsResponse response = request.TryIssueRequest(errorMessage);

                // Rip group names out of response and check for a match.
                groupListType groupList = response.GetGroupList();
                foreach (var group in groupList.group)
                {
                    if (group.name == groupName)
                    {
                        return group.id;
                    }
                }

                // If we've read all the group names in and still haven't found a match, give up.  Otherwise check the next page.
                paginationType paginationData = response.GetPaginationType();
                if (currentPage * Constants.MaxResponsePageSize >= Convert.ToInt32(paginationData.totalAvailable))
                {
                    morePagesToCheck = false;
                }
                currentPage++;
            }

            // No match found.
            return null;
        }

        /// <summary>
        /// Sets the default project permissions on a given project.
        /// </summary>
        /// <param name="projectId">The id of the project to set default permissions on.</param>
        /// <param name="groupId">The id of the group to add capabilities for.</param>
        /// <param name="permissionMapping">A mapping of the permissions to set.</param>
        public void AddDefaultProjectPermissions(string projectId, string groupId, IDictionary<capabilityTypeName, capabilityTypeMode> permissionMapping)
        {
            Uri uri = Endpoints.GetAddDefaultPermissionsWorkbookUri(baseUri, GetSiteId(), projectId);

            // Construct payload.
            tsRequest requestPayload = new tsRequest
            {
                Item = new permissionsType
                {
                    granteeCapabilities = new[]
                    {
                        new granteeCapabilitiesType
                        {
                            Item = new groupType
                            {
                                id = groupId
                            },
                            capabilities = permissionMapping.Select(permission => new capabilityType { name = permission.Key, mode = permission.Value }).ToArray()
                        }
                    }
                }
            };

            // Issue request.
            var errorMessage = String.Format("Failed to set default workbook permissions for project in site '{0}'", siteName);
            ApiRequest request = new ApiRequest(uri, HttpMethod.Put, null, requestPayload.SerializeBody(), GetAuthToken());
            request.TryIssueRequest(errorMessage);
        }

        /// <summary>
        /// Retrieves the site ID for the given site.
        /// </summary>
        /// <returns>The Site ID for the inputted site name.</returns>
        public string GetSiteId()
        {
            if (String.IsNullOrWhiteSpace(siteId))
            {
                var uri = Endpoints.GetQuerySiteUri(baseUri, siteName);

                // Issue request.
                var errorMessage = String.Format("Failed to retrieve site ID for site '{0}'", siteName);
                ApiRequest request = new ApiRequest(uri, HttpMethod.Get, null, null, GetAuthToken());
                tsResponse response = request.TryIssueRequest(errorMessage);

                // Extract site ID.
                siteType site = response.GetSite();
                siteId = site.id;
            }

            return siteId;
        }

        /// <summary>
        /// Authenticates the user with Tableau Server.
        /// </summary>
        /// <returns>The auth token for the authenticated user's session.</returns>
        public string Authenticate()
        {
            var uri = Endpoints.GetSignInUri(baseUri);

            // Construct payload.
            tsRequest requestPayload = new tsRequest()
            {
                Item = new tableauCredentialsType()
                {
                    name = userName,
                    password = password,
                    site = new siteType()
                    {
                        contentUrl = GetSiteAsContentUrl()
                    }
                }
            };

            // Issue request.
            var errorMessage = String.Format("Failed to authenticate user '{0}'", userName);
            ApiRequest request = new ApiRequest(uri, HttpMethod.Post, null, requestPayload.SerializeBody());
            tsResponse response = request.TryIssueRequest(errorMessage);

            // Extract authentication token.
            tableauCredentialsType credentials = response.GetTableauCredentials();
            return credentials.token;
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Retrieves an auth token, calling Authenticate if necessary.
        /// </summary>
        /// <returns>An auth token for the authenticated user's session.</returns>
        protected string GetAuthToken()
        {
            if (String.IsNullOrWhiteSpace(authToken))
            {
                authToken = Authenticate();
            }

            return authToken;
        }

        /// <summary>
        /// Returns a full URL to a workbook with a given name.
        /// </summary>
        /// <param name="workbookContentUrl">The content URL value for the workbook.</param>
        /// <returns>Full URL to the workbook.</returns>
        protected Uri GetWorkbookUrl(string workbookContentUrl)
        {
            string uriSuffix;
            if (IsDefaultSite())
            {
                uriSuffix = String.Format("workbooks/{0}", workbookContentUrl);
            }
            else
            {
                uriSuffix = String.Format("t/{0}/workbooks/{1}", siteName, workbookContentUrl);
            }

            return new Uri(baseUri, uriSuffix);
        }

        /// <summary>
        /// Retrieves the site name as it is reflected in the content URL.  Tableau Server encodes the Default Site as an empty Content URL.
        /// </summary>
        /// <returns>The site name as it appears in a content URL.</returns>
        protected string GetSiteAsContentUrl()
        {
            if (IsDefaultSite())
            {
                return "";
            }
            else
            {
                return siteName;
            }
        }

        /// <summary>
        /// Indicates whether this requestor is working with the "Default" Tableau Server site.
        /// </summary>
        /// <returns>True if using the Default site.</returns>
        protected bool IsDefaultSite()
        {
            return siteName.ToLowerInvariant() == "default";
        }

        #endregion Protected Methods
    }
}
