using LogParsers.Base.Extensions;
using LogParsers.Base.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LogParsers.Base.Parsers
{
    public abstract class AbstractMultiLineRegexParser : AbstractRegexParser
    {
        protected string bufferedLine;

        /// <summary>
        /// A set of regex patterns that denote the start of a valid log line.  Used to optimize read-ahead logic.
        /// </summary>
        protected abstract IList<Regex> LineDelimiterRegexes { get; }

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

        protected AbstractMultiLineRegexParser()
        {
        }

        protected AbstractMultiLineRegexParser(LogFileContext fileContext)
            : base(fileContext)
        {
        }

        /// <summary>
        /// The basic strategy of the multiline regex parser is to read & append lines until we hit a line that matches one of our LineDelimiterRegexes, then parse everything we've collected into a single document.  
        /// The delimiting line will be buffered for the next time ParseLogDocument is called.
        /// </summary>
        public override JObject ParseLogDocument(TextReader reader)
        {
            var sb = new StringBuilder();

            // Read a line (from the buffer, if it exists); bail out if we can't.
            LineCounter.Increment();
            var line = ReadLine(reader);
            if (String.IsNullOrWhiteSpace(line))
            {
                return null;
            }
            sb.Append(line);

            // Keep reading & appending more lines until we hit one that matches a known delimiter pattern
            bool nextLineIsMatch = false;
            int nonDocumentLines = 0;
            while (!nextLineIsMatch)
            {
                bufferedLine = ReadLine(reader);

                // If we failed to read a line, we need to break out
                if (bufferedLine == null)
                {
                    break;
                }

                // Check if the line we just read is a new log line
                nextLineIsMatch = IsNewLogLine(bufferedLine);

                if (!nextLineIsMatch)
                {
                    sb.Append("\n" + bufferedLine);
                    nonDocumentLines++;
                    bufferedLine = null;
                }
            }

            // Capture groups into dictionary
            IDictionary<string, object> fields = FindAndApplyRegexMatch(sb.ToString());

            // Give up if we didn't parse any data out of the line
            if (fields.Count == 0)
            {
                return null;
            }

            // Convert timestamp to internal common format.
            if (fields.ContainsKey("ts"))
            {
                fields["ts"] = TimestampStandardizer.Standardize(fields["ts"].ToString());
            }

            // Convert timezone/offset to internal common format.
            if (fields.ContainsKey("ts_offset"))
            {
                fields["ts_offset"] = TimeZoneStandardizer.StandardizeTimeZone(fields["ts_offset"].ToString());
            }

            // Convert dictionary to JSON and strip any properties with values on the blacklist
            var json = InsertMetadata(fields.ConvertToJObject().RemovePropertiesWithValue(defaultBlacklistedValues));

            // Update LineCounter to "skip" any multilines we read.
            LineCounter.IncrementBy(nonDocumentLines);

            return json;
        }

        protected override string ReadLine(TextReader reader)
        {
            string line;

            if (bufferedLine != null)
            {
                line = String.Copy(bufferedLine);
                bufferedLine = null;
            }
            else
            {
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
            }

            return line;
        }

        protected virtual bool IsNewLogLine(string line)
        {
            return LineDelimiterRegexes.Any(regex => regex.IsMatch(line));
        }
    }
}
