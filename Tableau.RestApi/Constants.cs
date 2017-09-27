namespace Tableau.RestApi
{
    /// <summary>
    /// Compile-time constants for Tableau Server Rest API.
    /// </summary>
    public static class Constants
    {
        public const string AuthenticationHeaderKey = "X-Tableau-Auth";
        public const string DefaultCreatedProjectDescription = "Auto-generated.";
        public const string DefaultContentType = "text/plain; charset=UTF-8";
        public const string DefaultEncoding = "UTF-8";
        public const int DefaultMaxRequestAttempts = 3;
        public const int MaxResponsePageSize = 1000;
        public const string RestApiVersion = "2.2";
    }
}