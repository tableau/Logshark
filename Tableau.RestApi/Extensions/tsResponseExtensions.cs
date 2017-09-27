using System;
using System.Net.Http;

namespace Tableau.RestApi.Extensions
{
    /// <summary>
    /// Extension methods for the auto-generated tsResponse class. Most of these are helper methods to handle a lot of the casting operations.
    /// A note on casing conventions: The Tableau Server REST API XSD schema uses camel-casing throughout.  In order to maximize flexibility for new API versions,
    /// this library relies on generated classes using the XSD schema, so many types are camel-cased here as well.
    /// </summary>
    public static class tsResponseExtensions
    {
        #region Public Methods

        public static connectionType GetConnection(this tsResponse response)
        {
            return ExtractItemByType(response, typeof(connectionType)) as connectionType;
        }

        public static connectionListType GetConnectionList(this tsResponse response)
        {
            return ExtractItemByType(response, typeof(connectionListType)) as connectionListType;
        }

        public static dataSourceType GetDataSource(this tsResponse response)
        {
            return ExtractItemByType(response, typeof(dataSourceType)) as dataSourceType;
        }

        public static dataSourceListType GetDataSourceList(this tsResponse response)
        {
            return ExtractItemByType(response, typeof(dataSourceListType)) as dataSourceListType;
        }

        public static errorType GetError(this tsResponse response)
        {
            return ExtractItemByType(response, typeof(errorType)) as errorType;
        }

        public static favoriteListType GetFavoriteList(this tsResponse response)
        {
            return ExtractItemByType(response, typeof(favoriteListType)) as favoriteListType;
        }

        public static fileUploadType GetFileUpload(this tsResponse response)
        {
            return ExtractItemByType(response, typeof(fileUploadType)) as fileUploadType;
        }

        public static groupType GetGroup(this tsResponse response)
        {
            return ExtractItemByType(response, typeof(groupType)) as groupType;
        }

        public static groupListType GetGroupList(this tsResponse response)
        {
            return ExtractItemByType(response, typeof(groupListType)) as groupListType;
        }

        public static jobType GetJob(this tsResponse response)
        {
            return ExtractItemByType(response, typeof(jobType)) as jobType;
        }

        public static paginationType GetPaginationType(this tsResponse response)
        {
            return ExtractItemByType(response, typeof(paginationType)) as paginationType;
        }

        public static permissionsType GetPermissions(this tsResponse response)
        {
            return ExtractItemByType(response, typeof(permissionsType)) as permissionsType;
        }

        public static projectType GetProject(this tsResponse response)
        {
            return ExtractItemByType(response, typeof(projectType)) as projectType;
        }

        public static projectListType GetProjectList(this tsResponse response)
        {
            return ExtractItemByType(response, typeof(projectListType)) as projectListType;
        }

        public static siteType GetSite(this tsResponse response)
        {
            return ExtractItemByType(response, typeof(siteType)) as siteType;
        }

        public static siteListType GetSiteList(this tsResponse response)
        {
            return ExtractItemByType(response, typeof(siteListType)) as siteListType;
        }

        public static tableauCredentialsType GetTableauCredentials(this tsResponse response)
        {
            return ExtractItemByType(response, typeof(tableauCredentialsType)) as tableauCredentialsType;
        }

        public static tagListType GetTags(this tsResponse response)
        {
            return ExtractItemByType(response, typeof(tagListType)) as tagListType;
        }

        public static userType GetUser(this tsResponse response)
        {
            return ExtractItemByType(response, typeof(userType)) as userType;
        }

        public static userListType GetUserList(this tsResponse response)
        {
            return ExtractItemByType(response, typeof(userListType)) as userListType;
        }

        public static viewListType GetViews(this tsResponse response)
        {
            return ExtractItemByType(response, typeof(viewListType)) as viewListType;
        }

        public static workbookType GetWorkbook(this tsResponse response)
        {
            return ExtractItemByType(response, typeof(workbookType)) as workbookType;
        }

        public static workbookListType GetWorkbookList(this tsResponse response)
        {
            return ExtractItemByType(response, typeof(workbookListType)) as workbookListType;
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Searches a tsResponse's Items array for an object matching the given type.
        /// </summary>
        /// <param name="response">This tsResponse object.</param>
        /// <param name="type">The type to search for.</param>
        /// <returns>A reference to item of the given type.</returns>
        private static object ExtractItemByType(this tsResponse response, Type type)
        {
            foreach (var item in response.Items)
            {
                if (item.GetType() == type)
                {
                    return item;
                }

                if (item.GetType() == typeof(errorType))
                {
                    var error = item as errorType;
                    throw new HttpRequestException(error.summary);
                }
            }
            throw new ArgumentException(String.Format("No '{0}' item is present in response", type));
        }

        #endregion Private Methods
    }
}