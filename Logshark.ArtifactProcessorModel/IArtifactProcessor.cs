using LogParsers.Base;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Logshark.ArtifactProcessorModel
{
    public interface IArtifactProcessor
    {
        // Friendly name of the artifact type that the artifact processor handles.
        string ArtifactType { get; }

        // List of MongoDB collections that will always be considered "required" when processing artifacts of this type.
        ISet<string> RequiredCollections { get; }

        // List of all supported file patterns within the root log directory.  Determines which files are extracted.
        ISet<Regex> SupportedFilePatterns { get; }

        // List of all supported plugin interfaces.  Determines which plugins are loaded.
        ISet<Type> SupportedPluginInterfaces { get; }

        // Indicates whether this artifact processor can service the given request.
        bool CanProcess(string rootLogLocation);

        // Custom hashing function for this artifact type.  This should return an MD5-style hash value.
        string ComputeArtifactHash(string rootLogLocation);

        // Retrieves any artifact-specific metadata for the given file context.
        IDictionary<string, object> GetAdditionalFileMetadata(LogFileContext fileContext);

        // Fetches a ParserFactory object, which can be used to fetch an IParser for any given file in the artifact payload.
        IParserFactory GetParserFactory(string rootLogLocation);
    }
}