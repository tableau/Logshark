using LogParsers.Base.Parsers;
using System.Collections.Generic;

namespace LogParsers.Base
{
    public interface IParserFactory
    {
        /// <summary>
        /// Create an instance of the correct parser type for a given log file.
        /// </summary>
        /// <param name="fileName">The logfile to be parsed.</param>
        /// <returns>Parser that can parse the log.</returns>
        IParser GetParser(string fileName);

        /// <summary>
        /// Create an instance of the correct parser type for a given log file.
        /// </summary>
        /// <param name="fileContext">Context object for the logfile to be parsed.</param>
        /// <returns>Parser that can parse the log.</returns>
        IParser GetParser(LogFileContext fileContext);

        /// <summary>
        /// Get a set of all parsers from this ParserFactory
        /// </summary>
        /// <returns>Set of all parsers from this factory</returns>
        ISet<IParser> GetAllParsers();

        /// <summary>
        /// Determines whether a given log file is supported as a parsable file type.
        /// </summary>
        /// <param name="fileName">The absolute filepath/name of the log file.</param>
        /// <returns>True if the file is parsable.</returns>
        bool IsSupported(string fileName);
    }
}