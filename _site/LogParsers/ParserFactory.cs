using System;
using System.Collections.Generic;
using System.IO;
using LogParsers.Helpers;

namespace LogParsers
{
    /// <summary>
    /// Primary contact point for requesting the appropriate parser instance for a given logfile.
    /// </summary>
    public class ParserFactory
    {
        protected readonly string rootLogLocation;

        public ParserFactory(string rootLogLocation)
        {
            this.rootLogLocation = rootLogLocation;
        }

        // Maps subdirectories within the root logs directory to the classes responsible for mapping concrete parsers to their contents.
        private static readonly IDictionary<string, Type> DirectoryMap = new Dictionary<string, Type>
        {
            { @"backgrounder", typeof(BackgrounderParserBuilder) },
            { @"cacheserver", typeof(CacheServerParserBuilder) },
            { @"clustercontroller", typeof(ClusterControllerParserBuilder) },
            { @"config", typeof(ConfigParserBuilder) },
            { @"dataengine", typeof(DataEngineParserBuilder) },
            { @"dataserver", typeof(DataserverParserBuilder) },
            { @"desktop", typeof(DesktopParserBuilder)},
            { @"filestore", typeof(FilestoreParserBuilder) },
            { @"httpd", typeof(HttpdParserBuilder) },
            { @"licensing", typeof(LicensingParserBuilder) },
            { @"logs", typeof(LogsParserBuilder) },
            { @"pgsql", typeof(PgsqlParserBuilder) },
            { @"searchserver", typeof(SearchServerParserBuilder) },
            { @"service", typeof(ServiceParserBuilder) },
            { @"solr", typeof(SolrParserBuilder) },
            { @"tabadmin", typeof(TabAdminParserBuilder) },
            { @"tabadminservice", typeof(TabAdminServiceParserBuilder) },
            { @"vizportal", typeof(VizportalParserBuilder) },
            { @"vizqlserver", typeof(VizqlParserBuilder) },
            { @"wgserver", typeof(WgServerParserBuilder) },
            { @"zookeeper", typeof(ZookeeperParserBuilder) }
        };

        /// <summary>
        /// Create an instance of the correct parser type for a given log file.
        /// </summary>
        /// <param name="fileName">The logfile to be parsed.</param>
        /// <returns>Parser that can parse the log.</returns>
        public IParser GetParser(string fileName)
        {
            var parserBuilder = GetParserBuilder(fileName);
            var fileContext = new LogFileContext(fileName, rootLogLocation);

            return parserBuilder.GetParser(fileContext);
        }

        /// <summary>
        /// Create an instance of the correct parser type for a given log file.
        /// </summary>
        /// <param name="fileContext">Context object for the logfile to be parsed.</param>
        /// <returns>Parser that can parse the log.</returns>
        public IParser GetParser(LogFileContext fileContext)
        {
            var parserBuilder = GetParserBuilder(fileContext.FilePath);

            return parserBuilder.GetParser(fileContext);
        }

        /// <summary>
        /// Retrieve a collection of all available parsers known by the factory.
        /// </summary>
        /// <returns>List of all available parser types.</returns>
        public static ISet<IParser> GetAllParsers()
        {
            // Build up set of all known parser builders.
            ISet<IParserBuilder> parserBuilders = new HashSet<IParserBuilder>();

            IParserBuilder rootParserBuilder = (IParserBuilder) Activator.CreateInstance(typeof (RootParserBuilder));
            parserBuilders.Add(rootParserBuilder);
            foreach (var parserType in DirectoryMap.Keys)
            {
                var parserBuilder = Activator.CreateInstance(DirectoryMap[parserType]) as IParserBuilder;

                if (parserBuilder != null)
                {
                    parserBuilders.Add(parserBuilder);
                }
            }

            // Build up set of all known parsers by all parser builders.
            ISet<IParser> parsers = new HashSet<IParser>();
            foreach (var parserBuilder in parserBuilders)
            {
                foreach (var parser in parserBuilder.GetAllParsers())
                {
                    parsers.Add(parser);
                }
            }

            return parsers;
        }

        public static ISet<IParser> GetDesktopParsers()
        {
            IList<string> desktopCollections = new List<string>
            {
                ParserConstants.DesktopCollectionName,
                ParserConstants.ProtocolServerCollectionName,
                ParserConstants.DataengineCollectionName
            };
            ISet<IParser> desktopParsers = new HashSet<IParser>();

            foreach (IParser parser in GetAllParsers())
            {
                if (desktopCollections.Contains(parser.CollectionSchema.CollectionName))
                {
                    desktopParsers.Add(parser);
                }
            }

            return desktopParsers;
        }

        public static ISet<IParser> GetServerParsers()
        {
            ISet<IParser> serverParsers = new HashSet<IParser>();
            foreach (IParser parser in GetAllParsers())
            {
                if (parser.CollectionSchema.CollectionName != ParserConstants.DesktopCollectionName)
                {
                    serverParsers.Add(parser);
                }
            }

            return serverParsers;
        }

        /// <summary>
        /// Determines whether a given log file is supported as a parsable file type.
        /// </summary>
        /// <param name="fileName">The absolute filepath/name of the log file.</param>
        /// <returns>True if the file is parsable.</returns>
        public bool IsSupported(string fileName)
        {
            // Sanity check.
            if (String.IsNullOrWhiteSpace(fileName) || !File.Exists(fileName))
            {
                throw new ArgumentException("Invalid filename!");
            }

            // Defer to the parser builder for this file.
            return GetParserBuilder(fileName).IsSupported(fileName);
        }

        /// <summary>
        /// Retrieve the correct parser builder for a given file.
        /// </summary>
        /// <param name="fileName">The absolute path to a log file.</param>
        /// <returns>ParserBuilder object for the file.</returns>
        protected IParserBuilder GetParserBuilder(string fileName)
        {
            // Get a list of all the subdirectories between this log file and the root of the extracted log zip,
            // then recursively walk that list looking for matches to our DirectoryMap dictionary.
            var parentDirs = ParserUtil.GetParentLogDirs(fileName, rootLogLocation);

            foreach (var dir in parentDirs)
            {
                if (DirectoryMap.ContainsKey(dir))
                {
                    Type parserBuilderType = DirectoryMap[dir];
                    var parserBuilder = Activator.CreateInstance(parserBuilderType) as IParserBuilder;
                    return parserBuilder;
                }
            }

            // If we didn't find a match for the directory this log lives in, try the root parser builder.
            return new RootParserBuilder();
        }
    }
}
