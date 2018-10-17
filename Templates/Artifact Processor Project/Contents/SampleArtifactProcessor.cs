using LogParsers.Base;
using Logshark.ArtifactProcessorModel;
using Logshark.ArtifactProcessors.$safeprojectname$.Parsing;
using Logshark.ArtifactProcessors.$safeprojectname$.PluginInterfaces;
using Logshark.Common.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Logshark.ArtifactProcessors.$safeprojectname$
{
    /// <summary>
    /// $safeprojectname$ Artifact Processor
    /// TODO: Add this project as a build dependency to the Logshark.CLI project.
    /// </summary>
    public class $safeprojectname$ : IArtifactProcessor
    {
        private static readonly ISet<Regex> supportedFilePatterns = new HashSet<Regex>
        {
            // TODO: Update this to match the supported file patterns within the artifact payload.
            new Regex(@"^.*\.(log|txt).*$", RegexOptions.Compiled)
        };

        private static readonly ISet<Type> supportedPluginInterfaces = new HashSet<Type>
        {
            // TODO: Update this interface name to an appropriate name for the kind of artifact being processed.
            typeof(ISamplePluginInterface)
        };

        #region IArtifactProcessor Implementation

        // Friendly name of the artifact type.
        public string ArtifactType
        {
            // TODO: Update this to a shortened one-word identifier for this artifact type.
            get { return "MyArtifactType"; }
        }

        // Fetches a ParserFactory object, which can be used to fetch an IParser for any given file in the artifact payload.
        public IParserFactory GetParserFactory(string rootLogLocation)
        {
            return new ParserFactory(rootLogLocation);
        }

        // List of all supported file patterns within the root log directory.  Determines which files are extracted.
        public ISet<Regex> SupportedFilePatterns
        {
            get { return supportedFilePatterns; }
        }

        // List of all supported plugin interfaces.  Determines which plugins are loaded.
        public ISet<Type> SupportedPluginInterfaces
        {
            get { return supportedPluginInterfaces; }
        }

        // List of MongoDB collections that will always be considered "required" when processing artifacts of this type.
        public ISet<string> RequiredCollections
        {
            get { return new HashSet<string>(); }
        }

        // Indicates whether this artifact processor can service the given request.
        public bool CanProcess(string rootLogLocation)
        {
            // TODO: Your logic goes here.   This logic should be as specific as possible, as only one artifact processor may match a given artifact.
            // You can inspect the archive contents at the following path: request.RunContext.RootLogDirectory
            throw new NotImplementedException("CanProcess must be defined for this artifact type!");
        }

        // Custom hashing function for this artifact type.  Typically this returns an MD5-style hash value.
        public string ComputeArtifactHash(string rootLogLocation)
        {
            // TODO: Verify that the following default is sufficiently unique for this payloads of this artifact type, or implement custom hash logic.
            return HashUtility.ComputeDirectoryHash(rootLogLocation);
        }

        public IDictionary<string, object> GetAdditionalFileMetadata(LogFileContext fileContext)
        {
            // TODO: Optionally extract any additional metadata to store on each parsed document.
            return new Dictionary<string, object>();
        }

        #endregion IArtifactProcessor Implementation
    }
}