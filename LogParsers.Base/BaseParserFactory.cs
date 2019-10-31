using LogParsers.Base.Helpers;
using LogParsers.Base.ParserBuilders;
using LogParsers.Base.Parsers;
using System;
using System.Collections.Generic;
using System.IO;

namespace LogParsers.Base
{
    public abstract class BaseParserFactory : IParserFactory
    {
        protected readonly string rootLogLocation;

        // Maps of subdirectories within the root logs directory to the classes responsible for mapping concrete parsers to their contents.
        protected abstract IDictionary<string, Type> DirectoryMap { get; }

        protected BaseParserFactory(string rootLogLocation)
        {
            this.rootLogLocation = rootLogLocation;
        }

        /// <summary>
        /// Create an instance of the correct parser type for a given log file.
        /// </summary>
        /// <param name="fileName">The logfile to be parsed.</param>
        /// <returns>Parser that can parse the log.</returns>
        public IParser GetParser(string fileName)
        {
            return GetParser(new LogFileContext(fileName, rootLogLocation));
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
        /// Determines whether a given log file is supported as a parsable file type.
        /// </summary>
        /// <param name="fileName">The absolute filepath/name of the log file.</param>
        /// <returns>True if the file is parsable.</returns>
        public bool IsSupported(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || !File.Exists(fileName))
            {
                throw new ArgumentException("Invalid filename!");
            }

            // Defer to the parser builder for this file.
            return GetParserBuilder(fileName).IsSupported(fileName);
        }

        public ISet<IParser> GetAllParsers()
        {
            // Build up set of all known parser builders.
            ISet<IParserBuilder> parserBuilders = new HashSet<IParserBuilder>();

            IParserBuilder rootParserBuilder = GetRootParserBuilder();
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

        /// <summary>
        /// Retrieve the correct parser builder for a given file.
        /// </summary>
        /// <param name="fileName">The absolute path to a log file.</param>
        /// <returns>ParserBuilder object for the file.</returns>
        protected virtual IParserBuilder GetParserBuilder(string fileName)
        {
            // Get a list of all the subdirectories between this log file and the root of the extracted log zip,
            // then recursively walk that list looking for matches to our DirectoryMap dictionary.
            var parentDirs = ParserUtil.GetParentLogDirs(fileName, rootLogLocation);

            foreach (var dir in parentDirs)
            {
                if (DirectoryMap.ContainsKey(dir))
                {
                    var parserBuilderType = DirectoryMap[dir];
                    var parserBuilder = Activator.CreateInstance(parserBuilderType) as IParserBuilder;
                    return parserBuilder;
                }
            }

            // If we didn't find a match for the directory this log lives in, try the root parser builder.
            return GetRootParserBuilder();
        }

        /// <summary>
        /// Retrieves the parser builder for the root log directory; i.e. not a subdirectory.
        /// </summary>
        /// <returns></returns>
        protected abstract IParserBuilder GetRootParserBuilder();
    }
}