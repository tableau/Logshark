using LogParsers;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Tableau.RestApi;

namespace Logshark
{
    public static class LogsharkConstants
    {
        #region Extraction

        // Whitelist regex to filter out spawn logs and only allow files with log/txt/yml/properties/conf/zip as an extension.
        public static readonly Regex EXTRACTION_FILE_WHITELIST = new Regex(@"^.*\.(log|txt|yml|csv|properties|conf|zip).*$", RegexOptions.Compiled);

        // Blacklist filter regex.  File names matching these will not be extracted.
        public static readonly IList<Regex> EXTRACTION_FILE_BLACKLISTS = new List<Regex>
        {
            new Regex(@"^spawn.*$", RegexOptions.Compiled),
            new Regex(@"^tabprotosrv-error.*$", RegexOptions.Compiled),
            new Regex(@"^tableau-error.*$", RegexOptions.Compiled)
        };

        public const int EXTRACTION_STREAM_BUFFER_SIZE = 4096;

        #endregion Extraction

        #region Metadata

        public const string LOGSHARK_METADATA_DATABASE_NAME = "logshark_metadata";

        #endregion Metadata

        #region Processing

        // Collections which should have their debug log entries processed even if the processDebug flag is set to false.
        public static readonly IList<string> DEBUG_PROCESSING_COLLECTION_WHITELIST = new List<string>
        {
            "tabadmin"
        };

        // The delay between writing out a heartbeat to Mongo, in seconds.
        public const int MONGO_PROCESSING_HEARTBEAT_INTERVAL = 15;

        // The time span after which a processing heartbeat is no longer considered valid, in seconds.
        public const int MONGO_PROCESSING_HEARTBEAT_EXPIRATION_TIME = 60;

        // Max number of bytes that we will submit in a single insertion batch.
        public const int MONGO_INSERTION_BATCH_SIZE = 4194304;

        // Max number of bytes MongoDB allows in a single batch insertion.
        public const int MONGO_INSERTION_BATCH_MAX_ALLOWED_SIZE = 16777216;

        // Max number of retries for failing insertions.
        public const int MONGO_MAX_INSERTION_RETRIES = 3;

        // Amount of time to sleep between thread activity checks, in ms.
        public const int MONGO_INSERTION_THREAD_POLL_INTERVAL = 100;

        // Max amount of time to wait for MongoDB insertion threads to finish their work, in ms.
        public const int MONGO_INSERTION_THREAD_TIMEOUT = 30000;

        // The default port that MongoDB should be run on if run locally.
        public const int MONGO_LOCAL_PORT_DEFAULT = 27017;

        // Name of the metadata collection that will keep any metadata about the run.
        public const string MONGO_METADATA_COLLECTION_NAME = "metadata";

        // Name of the metadata database that will store metadata about all runs.
        public const string MONGO_METADATA_DATABASE_NAME = "metadata";

        // List of any indexes that should be created for the metadata collection.
        public static readonly IList<string> MONGO_METADATA_COLLECTION_INDEXES = new List<string> { "processed_on_date" };

        // List of all DB names that the user should not be allowed to use.
        public static readonly ISet<string> PROTECTED_DATABASE_NAMES = new HashSet<string>
        {
            LOGSHARK_METADATA_DATABASE_NAME,
            "postgres"
        };

        // List of collections that we should always process.
        public static readonly ISet<string> REQUIRED_SERVER_COLLECTIONS = new HashSet<string>
        {
            ParserConstants.BuildVersionCollectionName,
            ParserConstants.ConfigCollectionName
        };

        #endregion Processing

        #region Publishing

        // Default permissions that should be enabled on any newly-created projects in Tableau Server.
        public static readonly IDictionary<capabilityTypeName, capabilityTypeMode> DEFAULT_PROJECT_PERMISSIONS = new Dictionary<capabilityTypeName, capabilityTypeMode>
        {
            { capabilityTypeName.ExportData, capabilityTypeMode.Allow },
            { capabilityTypeName.ExportXml, capabilityTypeMode.Allow },
            { capabilityTypeName.ViewUnderlyingData, capabilityTypeMode.Allow }
        };

        public const string DEFAULT_PROJECT_PERMISSIONS_GROUP = "All Users";

        // The maximum number of times that publishing a single workbook will be attempted.
        public const int WORKBOOK_PUBLISHING_MAX_ATTEMPTS = 3;

        // The delay between workbook publishing retries, in seconds.
        public const int WORKBOOK_PUBLISHING_RETRY_DELAY_SEC = 5;

        #endregion Publishing
    }
}