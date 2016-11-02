using System;

namespace Tableau.RestApi
{
    /// <summary>
    /// Handles mapping various Rest API endpoints to URIs.
    /// </summary>
    public static class Endpoints
    {
        public static Uri GetAddDefaultPermissionsWorkbookUri(Uri baseUri, string siteId, string projectId)
        {
            var endpoint = String.Format("sites/{0}/projects/{1}/default-permissions/workbooks", siteId, projectId);
            return BuildRestApiUri(baseUri, endpoint);
        }

        public static Uri GetAddTagsToWorkbookUri(Uri baseUri, string siteId, string workbookId)
        {
            var endpoint = String.Format("sites/{0}/workbooks/{1}/tags", siteId, workbookId);
            return BuildRestApiUri(baseUri, endpoint);
        }

        public static Uri GetCreateProjectUri(Uri baseUri, string siteId)
        {
            var endpoint = String.Format("sites/{0}/projects", siteId);
            return BuildRestApiUri(baseUri, endpoint);
        }

        public static Uri GetDeleteWorkbookUri(Uri baseUri, string siteId, string workbookId)
        {
            var endpoint = String.Format("sites/{0}/workbooks/{1}", siteId, workbookId);
            return BuildRestApiUri(baseUri, endpoint);
        }

        public static Uri GetPublishWorkbookUri(Uri baseUri, string siteId, bool overwrite)
        {
            var endpoint = String.Format("sites/{0}/workbooks?overwrite={1}", siteId, overwrite);
            return BuildRestApiUri(baseUri, endpoint);
        }

        public static Uri GetQueryGroupsUri(Uri baseUri, string siteId, int pageNumber)
        {
            var endpoint = String.Format("sites/{0}/groups?pageNumber={1}&pageSize={2}", siteId, pageNumber, Constants.MaxResponsePageSize);
            return BuildRestApiUri(baseUri, endpoint);
        }

        public static Uri GetQueryProjectsUri(Uri baseUri, string siteId, int pageNumber)
        {
            var endpoint = String.Format("sites/{0}/projects?pageNumber={1}&pageSize={2}", siteId, pageNumber, Constants.MaxResponsePageSize);
            return BuildRestApiUri(baseUri, endpoint);
        }

        public static Uri GetQuerySiteUri(Uri baseUri, string siteName)
        {
            var endpoint = String.Format("sites/{0}?key=name", siteName);
            return BuildRestApiUri(baseUri, endpoint);
        }

        public static Uri GetSignInUri(Uri baseUri)
        {
            var endpoint = "auth/signin";
            return BuildRestApiUri(baseUri, endpoint);
        }

        public static Uri GetUpdateProjectUri(Uri baseUri, string siteId, string projectId)
        {
            var endpoint = String.Format("sites/{0}/projects/{1}", siteId, projectId);
            return BuildRestApiUri(baseUri, endpoint);
        }

        private static Uri BuildRestApiUri(Uri baseUri, string apiEndpoint)
        {
            return new Uri(baseUri, String.Format("api/{0}/{1}", Constants.RestApiVersion, apiEndpoint));
        }
    }
}