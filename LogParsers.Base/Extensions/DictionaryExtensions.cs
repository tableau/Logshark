using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace LogParsers.Base.Extensions
{
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Converts a dictionary into a JObject.
        /// </summary>
        /// <param name="fields">The dictionary to convert.</param>
        /// <returns>JObject version of the dictionary.</returns>
        public static JObject ConvertToJObject(this IDictionary<string, object> fields)
        {
            return JObject.FromObject(fields);
        }

        /// <summary>
        /// Takes a collection of key/value pairs and pivots on a character in the key name to create a hierarchical tree-like structure.
        /// </summary>
        public static IDictionary<string, object> PivotToHierarchy(this IDictionary<string, object> dictionary, char pivotCharacter = '.')
        {
            var hierarchy = new Dictionary<string, object>();

            foreach (KeyValuePair<string, object> keyValuePair in dictionary)
            {
                var keySegments = keyValuePair.Key.Split(new[] { pivotCharacter }, StringSplitOptions.RemoveEmptyEntries);
                var keySegmentQueue = new Queue<string>(keySegments);
                AddKeyValuePairToHierarchy(hierarchy, keySegmentQueue, keyValuePair.Value);
            }

            return hierarchy;
        }

        /// <summary>
        /// Recursively adds a key/value pair to a nested dictionary, assuming the key has been split into segments.
        /// </summary>
        private static void AddKeyValuePairToHierarchy(IDictionary<string, object> hierarchy, Queue<string> keySegments, object value)
        {
            // Grab a key segment from the queue.
            string firstKeySegment = keySegments.Dequeue();

            // If the queue is now empty, we've reached the "bottom" of the hierarchy. This is the terminating condition for the recursion.
            if (!keySegments.Any())
            {
                if (!hierarchy.ContainsKey(firstKeySegment))
                {
                    hierarchy.Add(firstKeySegment, value);
                }
                return;
            }

            // Check if an entry with a matching name already exists; if it doesn't, create it.
            if (!hierarchy.ContainsKey(firstKeySegment))
            {
                hierarchy.Add(firstKeySegment, new Dictionary<string, object>());
            }

            // Recursively call this method on the next layer in the hierarchy.
            var nestedDictionary = hierarchy[firstKeySegment] as IDictionary<string, object>;
            if (nestedDictionary != null)
            {
                AddKeyValuePairToHierarchy(nestedDictionary, keySegments, value);
            }
        }
    }
}