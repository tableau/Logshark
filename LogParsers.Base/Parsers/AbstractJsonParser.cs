using LogParsers.Base.Extensions;
using LogParsers.Base.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace LogParsers.Base.Parsers
{
    /// <summary>
    /// Establishes the abstract class that other JSON log parsers should inherit from.
    /// </summary>
    public abstract class AbstractJsonParser : BaseParser
    {
        private static readonly IList<Func<JObject, JObject>> defaultJsonTransformationChain = new List<Func<JObject, JObject>>
        {
            ReplaceRawTimestampWithStandardizedTimestamp,
            StripPropertiesWithBlacklistedValues
        };

        // A collection of values that will act as a property filter -- any properties with values matching an item in the blacklist won't appear in the parsed JSON object. We do this to keep our records tidy.
        private static readonly string[] blacklistedPropertyValues = { String.Empty, "-" };

        private static readonly string jsonDateFormatString = "yyyy/MM/dd HH:mm:ss.fff";

        /// <summary>
        /// An ordered sequence of transforms that will be applied to all parsed JSON objects.
        /// </summary>
        protected virtual IList<Func<JObject, JObject>> TransformationChain
        {
            get { return defaultJsonTransformationChain; }
        }

        protected AbstractJsonParser()
        {
        }

        protected AbstractJsonParser(LogFileContext fileContext)
            : base(fileContext)
        {
        }

        /// <summary>
        /// Parse a single JSON log document from the given reader.
        /// </summary>
        /// <param name="reader">Cursor to some sort of string data reader pointed at a valid JSON string.</param>
        /// <returns>JObject containing parsed record transformed according to the specified transformation sequence.</returns>
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

            json = ApplyTransformationChain(json);

            return InsertMetadata(json);
        }

        /// <summary>
        /// Applies the transformation sequence to the given JSON object.
        /// </summary>
        protected JObject ApplyTransformationChain(JObject json)
        {
            return TransformationChain.Aggregate(json, (current, transform) => transform(current));
        }

        /// <summary>
        /// Replaces the raw timestamp in a JObject with a "standardized" version.
        /// </summary>
        /// <param name="json">The JSON object to do the timestamp replacement in.</param>
        protected static JObject ReplaceRawTimestampWithStandardizedTimestamp(JObject json)
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

            return json;
        }

        protected static JObject StripPropertiesWithBlacklistedValues(JObject json)
        {
            return json.RemovePropertiesWithValue(blacklistedPropertyValues);
        }
    }
}