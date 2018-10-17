using LogParsers.Base.Parsers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LogParsers.Base.ParserBuilders
{
    /// <summary>
    /// Base class from which a derived ParserBuilder class can simply define the FileMap property to get a good default implementation.
    /// </summary>
    public abstract class BaseParserBuilder : IParserBuilder
    {
        protected abstract IDictionary<string, Type> FileMap { get; }

        /// <summary>
        /// Retrieves the correct parser for a given log file.
        /// </summary>
        /// <param name="logFileContext">Context about the log file to retrieve a parser for.</param>
        /// <returns>Parser object that supports the specified log.</returns>
        public virtual IParser GetParser(LogFileContext logFileContext)
        {
            // Check to see if this file is in our map of known file types that we have parsers for.
            foreach (var fileMapping in FileMap.Keys)
            {
                var filePattern = new Regex(fileMapping);
                if (filePattern.IsMatch(logFileContext.FileName))
                {
                    // New up parser.
                    var parser = Activator.CreateInstance(FileMap[fileMapping], logFileContext) as IParser;
                    return parser;
                }
            }

            // Didn't find a match in the fileMap dictionary.
            return null;
        }

        /// <summary>
        /// Retrieve a list of all available parsers.
        /// </summary>
        /// <returns>List of all available parsers.</returns>
        public virtual IList<IParser> GetAllParsers()
        {
            return FileMap.Keys.Select(parser => Activator.CreateInstance(FileMap[parser]) as IParser).ToList();
        }

        /// <summary>
        /// Determine whether a file with the given filename is supported as a parsable file.
        /// </summary>
        /// <param name="file">The absolute path of the file to query support for.</param>
        /// <returns>True if file is a supported parsable file.</returns>
        public bool IsSupported(string file)
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                throw new ArgumentException("File path cannot be null or empty!");
            }

            string fileName = Path.GetFileName(file);
            if (fileName == null)
            {
                throw new ArgumentException("File does not exist!");
            }

            foreach (string fileMapping in FileMap.Keys)
            {
                var filePattern = new Regex(fileMapping);
                if (filePattern.IsMatch(fileName))
                {
                    return true; 
                }
            }
            return false;
        }
    }
}
