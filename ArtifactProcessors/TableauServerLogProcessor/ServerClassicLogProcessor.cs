using LogParsers.Base;
using LogParsers.Base.Helpers;
using Logshark.ArtifactProcessorModel;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.ParserMapping.Classic;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.PluginInterfaces;
using Logshark.Common.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor
{
    /// <summary>
    /// Tableau Server Classic Artifact Processor
    /// Processes the Tableau Server 9.X+ Windows-style log format
    /// </summary>
    public sealed class ServerClassicLogProcessor : IArtifactProcessor
    {
        private static readonly ISet<string> requiredCollections = new HashSet<string>
        {
            ParserConstants.ConfigCollectionName
        };

        private static readonly ISet<Regex> supportedFilePatterns = new HashSet<Regex>
        {
            new Regex(@"^.*\.(log|txt|yml|csv|properties|conf|zip).*$", RegexOptions.Compiled)
        };

        private static readonly ISet<Type> supportedPluginInterfaces = new HashSet<Type>
        {
            typeof(IServerClassicPlugin)
        };

        #region IArtifactProcessor Implementation

        public string ArtifactType
        {
            get { return "Server"; }
        }

        public ISet<string> RequiredCollections
        {
            get { return requiredCollections; }
        }

        public ISet<Regex> SupportedFilePatterns
        {
            get { return supportedFilePatterns; }
        }

        public ISet<Type> SupportedPluginInterfaces
        {
            get { return supportedPluginInterfaces; }
        }

        public bool CanProcess(string rootLogLocation)
        {
            return File.Exists(Path.Combine(rootLogLocation, "config", "workgroup.yml"));
        }

        public string ComputeArtifactHash(string rootLogLocation)
        {
            return HashUtility.ComputeDirectoryHash(rootLogLocation);
        }

        public IDictionary<string, object> GetAdditionalFileMetadata(LogFileContext fileContext)
        {
            return new Dictionary<string, object>
            {
                { "worker", GetWorkerId(fileContext) }
            };
        }

        public IParserFactory GetParserFactory(string rootLogLocation)
        {
            return new ServerClassicParserFactory(rootLogLocation);
        }

        #endregion IArtifactProcessor Implementation

        /// <summary>
        /// Given a log file path, attempt to glean a worker index from it.
        /// </summary>
        /// <returns>Id of worker node.</returns>
        private string GetWorkerId(LogFileContext fileContext)
        {
            string workerIndex = ParserUtil.GetParentLogDirs(fileContext.FilePath, fileContext.RootLogDirectory)
                                           .Where(parent => parent.StartsWith("worker"))
                                           .Select(name => name.Replace("worker", ""))
                                           .DefaultIfEmpty("0")
                                           .First();

            return workerIndex;
        }
    }
}