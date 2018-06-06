using System.Collections.Generic;
using Tableau.RestApi.Model;

namespace Tableau.RestApi
{
    public interface IRestApiRequestor
    {
        /// <summary>
        /// Sets the default project permissions on a given project.
        /// </summary>
        /// <param name="projectId">The id of the project to set default permissions on.</param>
        /// <param name="groupId">The id of the group to add capabilities for.</param>
        /// <param name="permissionMapping">A mapping of the permissions to set.</param>
        void AddDefaultProjectPermissions(string projectId, string groupId, IDictionary<capabilityTypeName, capabilityTypeMode> permissionMapping);

        /// <summary>
        /// Retrieves the group ID for the given group, or null if no match is found.
        /// </summary>
        /// <param name="groupName">The name of the group to query the ID of.</param>
        /// <returns>The Group ID for the inputted group name.</returns>
        string GetGroupId(string groupName);

        /// <summary>
        /// Queries for a project by name.  If no matching project is found, it will be created.
        /// </summary>
        /// <param name="projectName">The name of the project to search for.</param>
        /// <param name="createdProjectDescription">If we are creating the project, the description to set.</param>
        /// <returns>Project ID for queried/created project.</returns>
        string GetOrCreateProject(string projectName, string createdProjectDescription = Constants.DefaultCreatedProjectDescription);

        /// <summary>
        /// Retrieves the site ID for the given site.
        /// </summary>
        /// <returns>The Site ID for the inputted site name.</returns>
        string GetSiteId();

        PublishedWorkbookResult PublishWorkbookWithEmbeddedCredentials(PublishWorkbookRequest publishRequest);

        /// <summary>
        /// Return a list of all projects present on the site associated with this RestApiRequestor.
        /// </summary>
        /// <returns>List of all projects present on the site.</returns>
        IEnumerable<projectType> QueryProjects();
    }
}