using Logshark.Core.Controller.Plugin;
using Logshark.RequestModel;
using System.Collections.Generic;

namespace Logshark.Core.Helpers
{
    /// <summary>
    /// Handles logic around what data is required from a logset.
    /// </summary>
    internal static class LogsetDependencyHelper
    {
        #region Public Methods

        /// <summary>
        /// Diffs the collections we need to process the current logset against an existing set of collections for a given product type.
        /// </summary>
        /// <param name="request">The Logshark request object.</param>
        /// <param name="existingLogsetType">The product type of the existing logset.</param>
        /// <param name="existingCollections">The collections present in the existing logset.</param>
        /// <returns>Set of collections which are required to process the current request, but which don't exist already.</returns>
        public static ISet<string> GetMissingRequiredCollections(LogsharkRequest request, IEnumerable<string> existingCollections)
        {
            var requiredCollections = GetCollectionDependencies(request);
            requiredCollections.ExceptWith(existingCollections);

            return requiredCollections;
        }

        /// <summary>
        /// Retrieves the collections that a request requires.
        /// </summary>
        /// <param name="request">The Logshark request object.</param>
        /// <returns>Set of collection names required to process request.</returns>
        public static ISet<string> GetCollectionDependencies(LogsharkRequest request)
        {
            return PluginLoader.GetCollectionDependencies(request.RunContext.PluginTypesToExecute);
        }

        /// <summary>
        /// Indicates whether a given collection name is required for processing a given Logshark request.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="request">The Logshark request object.</param>
        /// <returns>True if collection is required by this request.</returns>
        public static bool IsCollectionRequiredForRequest(string collectionName, LogsharkRequest request)
        {
            if (request.ProcessFullLogset)
            {
                return true;
            }

            if (request.RunContext.RequiredCollections.Contains(collectionName))
            {
                return true;
            }

            var collectionDependencies = GetCollectionDependencies(request);
            return collectionDependencies.Contains(collectionName);
        }

        #endregion Public Methods
    }
}