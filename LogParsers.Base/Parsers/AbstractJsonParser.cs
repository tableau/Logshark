using System;
using System.Globalization;
using System.IO;
using LogParsers.Base.Extensions;
using LogParsers.Base.Helpers;
using Newtonsoft.Json.Linq;

namespace LogParsers.Base.Parsers
{
    /// <summary>
    /// Establishes the abstract class that other JSON log parsers should inherit from.
    /// </summary>
    public abstract class AbstractJsonParser : BaseParser
    {
        private readonly string[] defaultBlacklistedValues = { String.Empty, "-" };
        private static readonly string jsonDateFormatString = "yyyy/MM/dd HH:mm:ss.fff";

        /// <summary>
        /// A collection of values that will act as a property filter -- any properties with values matching an
        /// item in the blacklist won't appear in the parsed JSON object. We do this to keep our records tidy.
        /// </summary>
        protected virtual string[] BlacklistedValues
        {
            get { return defaultBlacklistedValues; }
        }

        protected AbstractJsonParser()
        {
        }

        protected AbstractJsonParser(LogFileContext fileContext)
            : base(fileContext)
        {
        }

        /// <summary>
        /// The default implementation just applies the blacklist logic to the existing JSON string, but we allow inheritors
        /// to override this logic as necessary.
        /// </summary>
        /// <param name="reader">Cursor to some sort of string data reader pointed at a valid JSON string.</param>
        /// <returns>JObject containing parsed record with all properties with blacklisted values removed.</returns>
        public override JObject ParseLogDocument(TextReader reader)
        {
            string line = ReadLine(reader);
            if (String.IsNullOrWhiteSpace(line))
            {
                return null;
            }

            JObject json;
            try
            {
                json = JObject.Parse(line);
            }
            catch (Exception)
            {
                return null;
            }

            ReplaceRawTimestampWithStandardizedTimestamp(json);

            return InsertMetadata(json.RemovePropertiesWithValue(BlacklistedValues));
        }

        /// <summary>
        /// Replaces the raw timestamp in a JObject with a "standardized" version.
        /// </summary>
        /// <param name="json">The JSON object to do the timestamp replacement in.</param>
        protected void ReplaceRawTimestampWithStandardizedTimestamp(JObject json)
        {
            // Convert timestamp to internal common format.
            JToken timestampToken = json["ts"];
            if (timestampToken != null)
            {
                // If this is a JSON Date object, we need to convert it to .NET DateTime first.
                string timestampString;
                if (timestampToken.Type == JTokenType.Date)
                {
                    DateTime timestamp = (DateTime)timestampToken.ToObject(typeof(DateTime));
                    timestampString = timestamp.ToString(jsonDateFormatString, CultureInfo.InvariantCulture);
                }
                else
                {
                    timestampString = timestampToken.ToString();
                }

                var standardizedTimestamp = TimestampStandardizer.Standardize(timestampString);
                timestampToken.Replace(new JValue(standardizedTimestamp));
            }
        }
    }
}