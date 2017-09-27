using System.Collections.Generic;

namespace LogParsers.Base.Helpers
{
    /// <summary>
    /// Provides a way to encapsulate information about a document collection.
    /// </summary>
    public sealed class CollectionSchema
    {
        public string CollectionName { get; private set; }
        public IList<string> Indexes { get; private set; }

        public CollectionSchema(string collectionName)
        {
            CollectionName = collectionName;
            Indexes = new List<string>();
        }

        /// <summary>
        /// Adds an index with the specified name to the collection.
        /// </summary>
        /// <param name="index">The index to add</param>
        public void AddIndex(string index)
        {
            Indexes.Add(index);
        }
    }
}
