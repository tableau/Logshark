using LogParsers.Base;
using Logshark.RequestModel;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Logshark.ArtifactProcessorModel
{
    public interface IArtifactProcessor
    {
        string ArtifactType { get; }

        ISet<Regex> SupportedFilePatterns { get; }

        ISet<Type> SupportedPluginInterfaces { get; }

        ISet<string> RequiredCollections { get; }

        bool CanProcess(LogsharkRequest request);

        IParserFactory GetParserFactory(string rootLogLocation);

        string ComputeArtifactHash(LogsharkRequest request);
    }
}