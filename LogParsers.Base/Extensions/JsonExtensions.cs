using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LogParsers.Base.Extensions
{
    /// <summary>
    /// Extension methods for the Newtonsoft.Json libraries.
    /// </summary>
    public static class JsonExtensions
    {
        #region Public Methods

        /// <summary>
        /// Removes all property nodes from a JObject which have a value matching the given blacklisted values.
        /// </summary>
        /// <param name="json">The JObject to parse.</param>
        /// <param name="blacklistedValues">A collection of blacklisted values.</param>
        /// <returns></returns>
        public static JObject RemovePropertiesWithValue(this JObject json, IList<string> blacklistedValues)
        {
            if (blacklistedValues == null || blacklistedValues.Count == 0)
            {
                return json;
            }

            return json.RemovePropertiesWithBlacklistedValue(blacklistedValues);
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Remove any child tokens from a JToken that have a value matching the blacklist.
        /// </summary>
        /// <param name="token">This JToken.</param>
        /// <param name="blacklistedValues">A collection of strings considered blacklisted.</param>
        /// <returns>JObject in which all properties with values matching the blacklist have been removed.</returns>
        private static JObject RemovePropertiesWithBlacklistedValue(this JObject token, IEnumerable<string> blacklistedValues)
        {
            IList<JToken> tokensToRemove = new List<JToken>();
            foreach (JProperty childToken in token.Properties())
            {
                if (childToken.Value is JValue && blacklistedValues.Contains(childToken.Value.ToString()))
                {
                    tokensToRemove.Add(childToken);
                }
                else if (childToken.Value is JContainer)
                {
                    JContainer container = (JContainer)childToken.Value;
                    container.RemovePropertiesWithBlacklistedValue(blacklistedValues);
                }
            }

            foreach (JToken tokenToRemove in tokensToRemove)
            {
                tokenToRemove.Remove();
            }

            return token;
        }

        /// <summary>
        /// Remove any child tokens from a JContainer that have a value matching the blacklist.
        /// </summary>
        /// <param name="container">This JContainer.</param>
        /// <param name="blacklistedValues">A collection of strings considered blacklisted.</param>
        private static void RemovePropertiesWithBlacklistedValue(this JContainer container, IEnumerable<string> blacklistedValues)
        {
            IList<JToken> tokensToRemove = new List<JToken>();
            foreach (JToken innerToken in container)
            {
                if (innerToken is JValue)
                {
                    JValue innerValue = innerToken as JValue;
                    if (blacklistedValues.Contains(innerValue.ToString(CultureInfo.InvariantCulture)))
                    {
                        tokensToRemove.Add(innerToken);
                    }
                }
                else if (innerToken is JObject)
                {
                    JObject innerJObject = innerToken as JObject;
                    innerJObject.RemovePropertiesWithBlacklistedValue(blacklistedValues);
                }
            }

            foreach (JToken tokenToRemove in tokensToRemove)
            {
                tokenToRemove.Remove();
            }
        }

        #endregion Private Methods
    }
}