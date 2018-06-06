using LogParsers.Base.Parsers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace Logshark.Tests.ServerLogProcessorTests
{
    /// <summary>
    /// Assorted helper methods to ease unit test authoring for this assembly.
    /// </summary>
    public class ParserTestHelpers
    {
        /// <summary>
        /// Parse a log file into a collection of JSON documents.
        /// </summary>
        /// <param name="filePath">The absolute path to the log file.</param>
        /// <param name="parser">The parser to use.</param>
        /// <param name="mockWorkerName">A fake worker name.</param>
        /// <returns>List of parsed JSON documents.</returns>
        public static IList<JObject> ParseFile(string filePath, IParser parser, string mockWorkerName = "worker0")
        {
            IList<JObject> documents = new List<JObject>();
            using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var bs = new BufferedStream(fs))
                {
                    using (var reader = new StreamReader(bs))
                    {
                        // Parse all loglines.
                        while (!parser.FinishedParsing)
                        {
                            var parsedDocument = parser.ParseLogDocument(reader);
                            if (parsedDocument != null)
                            {
                                documents.Add(parsedDocument);
                            }
                        }
                    }
                }
            }

            return documents;
        }

        /// <summary>
        /// Parses a single log line into a JSON document.
        /// </summary>
        /// <param name="logLine">The raw log line to parse.</param>
        /// <param name="parser">The parser to use.</param>
        /// <returns>A single parsed JSON document.</returns>
        public static string ParseSingleLine(string logLine, IParser parser)
        {
            using (TextReader reader = new StringReader(logLine))
            {
                return parser.ParseLogDocument(reader).ToString(Formatting.None);
            }
        }

        /// <summary>
        /// Retrieves the path to the Test Data directory.
        /// </summary>
        public static string GetTestDataPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Parsers\Tests\_TestData");
        }
    }
}