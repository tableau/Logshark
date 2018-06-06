using Logshark.ArtifactProcessorModel;
using Logshark.Core.Controller.Initialization;
using Logshark.RequestModel;
using System;
using System.Collections.Generic;

namespace Logshark.Core.Controller.Parsing
{
    internal class LogsetParsingRequest
    {
        // The target logset to parse.
        public LogsharkRequestTarget Target { get; protected set; }

        // The unique fingerprint of the logset.
        public string LogsetHash { get; protected set; }

        // The artifact processor to use for parsing the logset.
        public IArtifactProcessor ArtifactProcessor { get; protected set; }

        // The log collections that the user has requested be parsed.
        public ISet<string> CollectionsToParse { get; protected set; }

        // If enabled, any previously cached parsed data will be ignored.
        public bool ForceParse { get; protected set; }

        // The timestamp this request was created.
        public DateTime CreationTimestamp { get; protected set; }

        public LogsetParsingRequest(RunInitializationResult initializationResult, bool forceParse)
        {
            Target = initializationResult.Target;
            LogsetHash = initializationResult.LogsetHash;
            ArtifactProcessor = initializationResult.ArtifactProcessor;
            CollectionsToParse = initializationResult.CollectionsRequested;
            ForceParse = forceParse;
            CreationTimestamp = DateTime.UtcNow;
        }
    }
}