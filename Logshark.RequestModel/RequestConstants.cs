using System.Collections.Generic;

namespace Logshark.RequestModel
{
    public static class RequestConstants
    {
        // The name of the Postgres database where any metadata should be stored.
        public const string LOGSHARK_METADATA_DATABASE_NAME = "logshark_metadata";

        // The default port that MongoDB runs on if run locally.
        public static readonly int MONGO_LOCAL_PORT_DEFAULT = 27017;

        // List of all Postgres DB names that the user should not be allowed to use.
        public static readonly ISet<string> PROTECTED_DATABASE_NAMES = new HashSet<string>
        {
            LOGSHARK_METADATA_DATABASE_NAME,
            "postgres"
        };

        public const string UNKNOWN_LOGSET_TYPE = "unknown";
    }
}