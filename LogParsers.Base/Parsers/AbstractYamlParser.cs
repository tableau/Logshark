using System.Collections.Generic;
using System.IO;
using LogParsers.Base.Helpers;
using Newtonsoft.Json.Linq;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace LogParsers.Base.Parsers
{
    public abstract class AbstractYamlParser : BaseParser
    {
        /// <summary>
        /// Flag that indicates whether this parser reads multiple lines to parse a single document.
        /// </summary>
        public override bool IsMultiLineLogType { get { return true; } }

        protected override bool UseLineNumbers { get { return false; } }

        protected AbstractYamlParser() { }
        protected AbstractYamlParser(LogFileContext fileContext) : base(fileContext) { }

        /// <summary>
        /// Reads the entire file and parses it as a collection of YAML documents, then converts it all to JSON.
        /// </summary>
        /// <param name="reader">Text reader pointed at log file.</param>
        /// <returns>JObject containing data from YAML documents.</returns>
        public override JObject ParseLogDocument(TextReader reader)
        {
            // Parse entire document into a single YAML document object.
            var text = reader.ReadToEnd();

            var documents = ParseYamlObjects(text);
            if (documents == null || documents.Count == 0)
            {
                FinishedParsing = true;
                return null;
            }

            return TransformYamlToJson(documents);
        }

        protected abstract JObject TransformYamlToJson(IList<object> documents);

        /// <summary>
        /// Parses a string into a collection of YAML documents.
        /// </summary>
        /// <param name="text">A string containing one or more YAML documents.</param>
        /// <returns>List of parsed documents.</returns>
        protected virtual IList<object> ParseYamlObjects(string text)
        {
            IList<object> documents = new List<object>();

            var input = new StringReader(text);
            var reader = new Parser(input);
            var deserializer = new Deserializer();

            // Consume the stream start event "manually"
            reader.Expect<StreamStart>();

            while (reader.Accept<DocumentStart>())
            {
                // Deserialize the document
                var document = deserializer.Deserialize(reader);

                documents.Add(document);
            }

            return documents;
        }
    }
}
