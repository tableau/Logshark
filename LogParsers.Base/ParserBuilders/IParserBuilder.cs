using LogParsers.Base.Parsers;
using System.Collections.Generic;

namespace LogParsers.Base.ParserBuilders
{
    /// <summary>
    /// Represents a factory-style class that can create IParser instances for logs within a single directory in the log tree structure.
    /// </summary>
    public interface IParserBuilder
    {
        /// <summary>
        /// Get an instance of an IParser object that can parse the specified log file.
        /// </summary>
        /// <param name="fileContext">Context about the file to parse.</param>
        /// <returns>An instance of IParser capable of parsing the log file.</returns>
        IParser GetParser(LogFileContext fileContext);

        /// <summary>
        /// Get a list of all available parsers. Used primarily for getting metadata about collection names & indexes.
        /// </summary>
        /// <returns>List of all available parsers for the log structure.</returns>
        IList<IParser> GetAllParsers();

        /// <summary>
        /// Determine whether a particular file is supported by this parser, based on name.
        /// </summary>
        /// <param name="fileName">The absolute filename/path to a log file.</param>
        /// <returns>True if the file is supported by this parser.</returns>
        bool IsSupported(string fileName);
    }
}
