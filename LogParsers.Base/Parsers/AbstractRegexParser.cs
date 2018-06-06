using LogParsers.Base.Extensions;
using LogParsers.Base.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace LogParsers.Base.Parsers
{
    public abstract class AbstractRegexParser : BaseParser
    {
        protected readonly string[] defaultBlacklistedValues = { String.Empty, "", "-" };

        /// <summary>
        /// A collection of regexes that are known good matches for this log type.  These regexes must utilize named capture groups.
        /// </summary>
        protected abstract IList<Regex> Regexes { get; }

        /// <summary>
        /// A collection of values that will act as a property filter -- any properties with values matching an
        /// item in the blacklist won't appear in the parsed JSON object. We do this to keep our records tidy.
        /// </summary>
        protected virtual string[] BlacklistedValues
        {
            get { return defaultBlacklistedValues; }
        }

        protected AbstractRegexParser()
        {
        }

        protected AbstractRegexParser(LogFileContext fileContext)
            : base(fileContext)
        {
        }

        /// <summary>
        /// Reads a line and parses it into a JSON document using a collection of possible regex matches.  Inserts line number/filename/workername properties and standardizes timestamp.
        /// </summary>
        /// <param name="reader">Cursor to some sort of string data reader pointed at log file.</param>
        /// <returns>JObject containing parsed record with all properties with blacklisted values removed and standardized timestamp.</returns>
        public override JObject ParseLogDocument(TextReader reader)
        {
            var line = ReadLine(reader);
            if (String.IsNullOrWhiteSpace(line))
            {
                return null;
            }

            IDictionary<string, object> fields = FindAndApplyRegexMatch(line);

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
            var json = fields.ConvertToJObject().RemovePropertiesWithValue(defaultBlacklistedValues);

            return InsertMetadata(json);
        }

        /// <summary>
        /// Finds a match in a list of potential regex match candidates and shoves resulting captured key/value pairs into a dictionary.
        /// </summary>
        /// <param name="line">Log line to apply regexes to.</param>
        /// <returns>Dictionary of key/value pairs of captured groups using a matching regex.</returns>
        protected virtual IDictionary<string, object> FindAndApplyRegexMatch(string line)
        {
            IDictionary<string, object> fields = null;
            bool foundMatch = false;
            int indexToTry = 0;

            // Loop all over the known good patterns, looking for a match.
            while (!foundMatch && indexToTry < Regexes.Count)
            {
                // Apply regex and dump all of the named capture groups into a dictionary.
                fields = Regexes[indexToTry].MatchNamedCaptures(line);

                if (fields.Count > 0)
                {
                    foundMatch = true;
                    // Make sure the matching regex is at the front of the list to optimize future matching
                    if (indexToTry > 0)
                    {
                        Regexes.MoveToFront(indexToTry);
                    }
                }
                indexToTry++;
            }

            return fields;
        }
    }
}
