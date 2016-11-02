using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using LogParsers.Helpers;

namespace LogParsers
{
    /// <summary>
    /// This class is used for situations where we want to parse an entire file into a single output document with nested documents comprised of some complex structure.
    /// </summary>
    public abstract class AbstractSingleDocumentRegexParser : AbstractRegexParser
    {
        /// <summary>
        /// The base line number behavior isn't applicable to this parser type.
        /// </summary>
        protected override bool UseLineNumbers { get { return false; } }

        /// <summary>
        /// Flag that indicates whether this parser reads multiple lines to parse a single document.
        /// </summary>
        public override bool IsMultiLineLogType
        {
            get
            {
                return true;
            }
        }

        protected AbstractSingleDocumentRegexParser() { }
        protected AbstractSingleDocumentRegexParser(LogFileContext fileContext) : base(fileContext) { }

        /// <summary>
        /// Parses all lines using regex and outputs a single Json document with nested documents for each line.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public override JObject ParseLogDocument(TextReader reader)
        {
            // We have two possible output schema: complex records or simple key/value pairs.
            IList<IDictionary<string, object>> records = new List<IDictionary<string, object>>();
            IDictionary<string, object> pairs = new Dictionary<string, object>();

            // Parse each line using the regex into a set of key/value pairs.
            string line;
            while ((line = ReadLine(reader)) != null)
            {
                LineCounter.Increment();
                IDictionary<string, object> fields = FindAndApplyRegexMatch(line);

                if (fields.Count > 0)
                {
                    // In certain cases, we just want to write a key/value pair.
                    if (fields.Count == 2 && fields.ContainsKey("key") && fields.ContainsKey("value"))
                    {
                        pairs.Add(fields["key"].ToString(), fields["value"]);
                    }
                    else
                    {
                        fields.Add("line", LineCounter.CurrentValue.ToString());
                        records.Add(fields);
                    }
                }
            }

            // Bail out if we didn't successfully parse anything.
            if (records.Count == 0 && pairs.Count == 0)
            {
                return null;
            }

            // Serialize and return result.
            JToken jtoken;
            if (pairs.Count > 0)
            {
                // jtoken = pairs.SerializeToJson();
                jtoken = JObject.FromObject(pairs);
            }
            else
            {
                jtoken = JArray.FromObject(records);
            }

            var json = new JObject { { "contents", jtoken } };
            FinishedParsing = true;

            return InsertMetadata(json);
        }
    }
}