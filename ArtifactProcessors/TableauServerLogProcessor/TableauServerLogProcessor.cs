using LogParsers.Base;
using Logshark.ArtifactProcessorModel;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsing;
using Logshark.Common.Helpers;
using Logshark.PluginLib.Model;
using Logshark.RequestModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor
{
    /// <summary>
    /// Tableau Server Artifact Processor
    /// </summary>
    public class TableauServerLogProcessor : IArtifactProcessor
    {
        private static readonly ISet<Regex> supportedFilePatterns = new HashSet<Regex>
        {
            new Regex(@"^.*\.(log|txt|yml|csv|properties|conf|zip).*$", RegexOptions.Compiled)
        };

        private static readonly ISet<Type> supportedPluginInterfaces = new HashSet<Type>
        {
            typeof(IServerPlugin)
        };

        #region IArtifactProcessor Implementation

        // Friendly name of the artifact type.
        public string ArtifactType
        {
            get { return "Server"; }
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
            get
            {
                return new HashSet<string>
                {
                    ParserConstants.BuildVersionCollectionName,
                    ParserConstants.ConfigCollectionName
                };
            }
        }

        // Indicates whether this artifact processor can service the given request.
        public bool CanProcess(LogsharkRequest request)
        {
            return IsServerLogSet(request.RunContext.RootLogDirectory);
        }

        // Custom hashing function for this artifact type.  Typically this returns an MD5-style hash value.
        public string ComputeArtifactHash(LogsharkRequest request)
        {
            return LogsetHashUtil.GetLogSetHash(request.Target);
        }

        #endregion IArtifactProcessor Implementation

        /// <summary>
        /// Indicates whether the given path appears to be a Tableau Server logset.
        /// </summary>
        private static bool IsServerLogSet(string rootLogDirectory)
        {
            bool hasBuildVersionFile = File.Exists(Path.Combine(rootLogDirectory, "buildversion.txt"));
            bool hasWorkgroupYmlFile = File.Exists(Path.Combine(rootLogDirectory, "config", "workgroup.yml"));
            return hasBuildVersionFile && hasWorkgroupYmlFile;
        }
    }
}