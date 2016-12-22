using LogParsers;
using Logshark.Controller.Plugin;
using System.Collections.Generic;
using System.IO;

namespace Logshark.Helpers
{
    /// <summary>
    /// Handles logic around what data is required from a logset.
    /// </summary>
    internal static class LogsetDependencyHelper
    {
        #region Public Methods

        /// <summary>
        /// Determines whether a given file is required to successfully process a given request.
        /// </summary>
        /// <param name="file">The absolute path to the file.</param>
        /// <param name="logsetRoot">The absolute path to the logset root.</param>
        /// <param name="request">The Logshark request to check.</param>
        /// <returns>True if file is necessary to successfully process the Logshark request.</returns>
        public static bool IsLogfileRequiredForRequest(string file, string logsetRoot, LogsharkRequest request)
        {
            if (PathHelper.IsArchive(file))
            {
                return true;
            }

            // Get the name of the collection that parsing this file would populate.
            IParser parser = GetParser(file, logsetRoot);

            // Desktop files will wind up moved to a subdirectory, so we need to check there too.
            if (parser == null)
            {
                string desktopFile = file.Replace(logsetRoot, Path.Combine(logsetRoot, "desktop"));
                parser = GetParser(desktopFile, logsetRoot);

                // We don't have a parser for it, so we must not need it.
                if (parser == null)
                {
                    return false;
                }
            }

            if (request.ProcessFullLogset)
            {
                return true;
            }

            string collectionName = parser.CollectionSchema.CollectionName.ToLowerInvariant();

            return IsCollectionRequiredForRequest(collectionName, request);
        }

        /// <summary>
        /// Diffs the collections we need to process the current logset against an existing set of collections for a given product type.
        /// </summary>
        /// <param name="request">The Logshark request object.</param>
        /// <param name="existingLogsetType">The product type of the existing logset.</param>
        /// <param name="existingCollections">The collections present in the existing logset.</param>
        /// <returns>Set of collections which are required to process the current request, but which don't exist already.</returns>
        public static ISet<string> GetMissingRequiredCollections(LogsharkRequest request, LogsetType existingLogsetType, IEnumerable<string> existingCollections)
        {
            var requiredCollections = GetCollectionDependencies(request, existingLogsetType);
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
            return PluginLoader.GetCollectionDependencies(request.RunContext.PluginTypesToExecute, request.RunContext.LogsetType);
        }

        /// <summary>
        /// Retrieves the collections that a request requires, filtered by product type.
        /// </summary>
        /// <param name="request">The Logshark request object.</param>
        /// <param name="logsetType">The product type that the logset covers.</param>
        /// <returns>Set of collection names required to process request, filtered by product type.</returns>
        public static ISet<string> GetCollectionDependencies(LogsharkRequest request, LogsetType logsetType)
        {
            return PluginLoader.GetCollectionDependencies(request.RunContext.PluginTypesToExecute, logsetType);
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

            if (request.RunContext.LogsetType != LogsetType.Desktop &&
                LogsharkConstants.REQUIRED_SERVER_COLLECTIONS.Contains(collectionName))
            {
                return true;
            }

            var collectionDependencies = GetCollectionDependencies(request, request.RunContext.LogsetType);
            return collectionDependencies.Contains(collectionName);
        }

        #endregion Public Methods

        #region Private Methods

        // Small helper method to just retrieve a parser.
        private static IParser GetParser(string file, string logsetRoot)
        {
            ParserFactory parserFactory = new ParserFactory(logsetRoot);
            return parserFactory.GetParser(file);
        }

        #endregion Private Methods
    }
}