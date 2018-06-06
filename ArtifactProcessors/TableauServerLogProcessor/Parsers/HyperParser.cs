using LogParsers.Base;
using LogParsers.Base.Helpers;
using LogParsers.Base.Parsers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers
{
    /// <summary>
    /// Parses Hyper logs to JSON.
    /// </summary>
    public sealed class HyperParser : AbstractJsonParser, IParser
    {
        private static readonly string collectionName = ParserConstants.HyperCollectionName;
        private static readonly IList<string> indexNames = new List<string> { "k", "file", "pid", "req", "sess", "sev", "worker" };
        private static readonly CollectionSchema collectionSchema = ParserUtil.CreateCollectionSchema(collectionName, indexNames);

        private static readonly IList<Func<JObject, JObject>> jsonTransformationChain = new List<Func<JObject, JObject>>
        {
            ReplaceRawTimestampWithStandardizedTimestamp,
            StripPropertiesWithBlacklistedValues,
            ReplaceValuePayloadIntegerFieldsWithStrings
        };

        public override CollectionSchema CollectionSchema
        {
            get
            {
                return collectionSchema;
            }
        }

        protected override IList<Func<JObject, JObject>> TransformationChain
        {
            get { return jsonTransformationChain; }
        }

        public HyperParser()
        {
        }

        public HyperParser(LogFileContext fileContext)
            : base(fileContext)
        {
        }

        /// <summary>
        /// Hyper stores & logs many fields as unsigned 64-bit ints.  In case, for example, we ever need to return a row count greater than the number of atoms in the earth.
        /// This transformation takes care of this by replace *all* integer key/value pairs nested under the "v" JSON sub-object with string representations.  This makes using these values
        /// a little tougher, but ultimately this is more flexible and less cumbersome than the alternatives.
        /// </summary>
        private static JObject ReplaceValuePayloadIntegerFieldsWithStrings(JObject json)
        {
            // Build an enumeration of all integer sub-properties of the "v" payload
            var valuePayloadIntegerProperties = json.SelectTokens("v")
                                      .Children()
                                      .Where(token => token.Type == JTokenType.Property)
                                      .OfType<JProperty>()
                                      .Where(property => property.Value.Type == JTokenType.Integer && property.HasValues)
                                      .ToList();

            // Replace all integer properties we found with string representations
            foreach (JProperty integerProperty in valuePayloadIntegerProperties)
            {
                integerProperty.Replace(new JProperty(integerProperty.Name, integerProperty.Value.ToString()));
            }

            return json;
        }
    }
}