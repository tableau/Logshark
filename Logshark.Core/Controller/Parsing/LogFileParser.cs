using log4net;
using LogParsers.Base;
using LogParsers.Base.Parsers;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Reflection;

namespace Logshark.Core.Controller.Parsing
{
    internal class LogFileParser
    {
        protected readonly IParser parser;
        protected readonly IDocumentWriter writer;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public LogFileParser(IParser parser, IDocumentWriter writer)
        {
            this.writer = writer;
            this.parser = parser;
        }

        /// <summary>
        /// Parse the given log file.
        /// </summary>
        /// <returns>Count of documents that were successfully parsed.</returns>
        public long Parse(LogFileContext logFile)
        {
            long processedDocumentCount = 0;

            using (var reader = new StreamReader(new FileStream(logFile.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                while (!parser.FinishedParsing)
                {
                    // Parse a document.
                    JObject document = parser.ParseLogDocument(reader);
                    if (document != null)
                    {
                        DocumentWriteResult result = writer.Write(document);
                        switch (result.Result)
                        {
                            case DocumentWriteResultType.Failure:
                                Log.WarnFormat("Failed to write document parsed from file '{0}': {1}", logFile, result.ErrorMessage);
                                break;
                            case DocumentWriteResultType.SuccessWithWarning:
                                Log.WarnFormat($"Document from file '{logFile}' processed with warning: {result.ErrorMessage}");
                                break;
                        }

                        processedDocumentCount++;
                    }
                }
            }

            writer.Shutdown();

            return processedDocumentCount;
        }
    }
}