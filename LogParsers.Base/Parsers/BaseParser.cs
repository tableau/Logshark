using System;
using System.IO;
using LogParsers.Base.Helpers;
using Newtonsoft.Json.Linq;

namespace LogParsers.Base.Parsers
{
    /// <summary>
    /// Base parser that all other concrete parsers should inherit from.  Provides automatic line counting and incrementing, as well as pre-implemented metadata insertion.
    /// </summary>
    public abstract class BaseParser : IParser
    {
        protected LineCounter LineCounter { get; private set; }
        protected LogFileContext FileContext { get; private set; }

        /// <summary>
        /// The CollectionSchema contains information about the collection name & indexes associated with this kind of parser.
        /// </summary>
        public abstract CollectionSchema CollectionSchema
        {
            get;
        }

        /// <summary>
        /// Flag that indicates whether this parser reads multiple lines to parse a single document.
        /// </summary>
        public virtual bool IsMultiLineLogType
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Flag to indicate whether this parser is done parsing the file.
        /// </summary>
        public virtual bool FinishedParsing
        {
            get;
            protected set;
        }

        /// <summary>
        /// Flag to indicate whether line numbers are relevant to this kind of parser.  Default is true.
        /// </summary>
        protected virtual bool UseLineNumbers { get { return true; } }

        protected BaseParser()
        {
            LineCounter = new LineCounter();
            FileContext = null;
        }

        protected BaseParser(LogFileContext fileContext)
        {
            LineCounter = new LineCounter(fileContext.LineOffset);
            FileContext = fileContext;
        }

        /// <summary>
        /// Parses a single Json log document from a text reader.
        /// </summary>
        /// <param name="reader">An open reader to a log file.</param>
        /// <returns>Json document, or else null if no parse is possible.</returns>
        public abstract JObject ParseLogDocument(TextReader reader);

        /// <summary>
        /// A default implementation for safely reading a single line from the reader.  Handles line counting and EOF detection.
        /// </summary>
        /// <param name="reader">An open reader to a log file.</param>
        /// <returns>A line of text, or null if EOF or an exception is encountered.</returns>
        protected virtual string ReadLine(TextReader reader)
        {
            if (UseLineNumbers)
            {
                LineCounter.Increment();
            }

            string line;
            try
            {
                line = reader.ReadLine();
            }
            catch (Exception)
            {
                return null;
            }
            if (line == null)
            {
                FinishedParsing = true;
            }

            return line;
        }

        /// <summary>
        /// Inserts metadata about the log file into a Json object.  Also sets a custom id field.
        /// </summary>
        /// <param name="json">A Json object containing log information.</param>
        /// <returns>The Json object with available file context, id and line metadata added.</returns>
        public virtual JObject InsertMetadata(JObject json)
        {
            if (FileContext != null)
            {
                string id = String.Format(@"{0}/{1}", FileContext.FileLocationRelativeToRoot.Replace('\\', '/'), FileContext.LogicalFileName);
                if (UseLineNumbers)
                {
                    id = String.Format("{0}-{1}", id, LineCounter.CurrentValue);
                }
                json.AddFirst(new JProperty("_id", id));
                json.Add(new JProperty("file_path", FileContext.FileLocationRelativeToRoot));
                json.Add(new JProperty("file", FileContext.LogicalFileName));
                json.Add(new JProperty("worker", FileContext.WorkerIndex));
            }

            if (UseLineNumbers)
            {
                json.Add(new JProperty("line", LineCounter.CurrentValue));
            }

            return json;
        }
    }
}
