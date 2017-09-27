using System.IO;
using LogParsers.Base.Helpers;
using Newtonsoft.Json.Linq;

namespace LogParsers.Base.Parsers
{
    /// <summary>
    /// Represents a parser capable of parsing JSON documents from a specific log file.
    /// </summary>
    public interface IParser
    {
        /// <summary>
        /// The name & index information for the resulting document collection.
        /// </summary>
        CollectionSchema CollectionSchema { get; }

        /// <summary>
        /// Flag that indicates whether we've finished parsing.
        /// </summary>
        bool FinishedParsing { get; }

        /// <summary>
        /// Flag that indicates whether this parser ever reads multiple lines to construct a single document.
        /// </summary>
        bool IsMultiLineLogType { get; }

        /// <summary>
        /// Parses a single JSON document, starting at the current cursor position of the reader.
        /// </summary>
        /// <param name="reader">An initialized TextReader pointing to a log file.</param>
        /// <returns>A single JSON document containing log data.</returns>
        JObject ParseLogDocument(TextReader reader);
    }
}