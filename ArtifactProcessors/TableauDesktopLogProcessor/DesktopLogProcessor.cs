using LogParsers.Base;
using Logshark.ArtifactProcessorModel;
using Logshark.ArtifactProcessors.TableauDesktopLogProcessor.ParserMapping;
using Logshark.ArtifactProcessors.TableauDesktopLogProcessor.PluginInterfaces;
using Logshark.Common.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Logshark.ArtifactProcessors.TableauDesktopLogProcessor
{
    public sealed class DesktopLogProcessor : IArtifactProcessor
    {
        private static readonly ISet<string> requiredCollections = new HashSet<string>();

        private static readonly ISet<Regex> supportedFilePatterns = new HashSet<Regex>
        {
            new Regex(@"^.*\.(log|txt|zip).*$", RegexOptions.Compiled)
        };

        private static readonly ISet<Type> supportedPluginInterfaces = new HashSet<Type>
        {
            typeof(IDesktopPlugin)
        };

        #region IArtifactProcessor Implementation

        public string ArtifactType
        {
            get { return "Desktop"; }
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
            bool hasTabsvcYmlFile = File.Exists(Path.Combine(rootLogLocation, "tabsvc.yml"));

            // Given that these logs get zipped by hand usually we need to check either the root or the Logs subdirectory.
            bool hasLogTxtInRoot = File.Exists(Path.Combine(rootLogLocation, "log.txt"));
            bool hasLogTxtInLogsSubdir = File.Exists(Path.Combine(rootLogLocation, "Logs", "log.txt"));

            // If we don't have a tabsvc.yml file then we know it's not a server log.
            // If we have a log.txt then we know it's most likely a desktop log.
            return !hasTabsvcYmlFile && (hasLogTxtInRoot || hasLogTxtInLogsSubdir);
        }

        public string ComputeArtifactHash(string rootLogLocation)
        {
            return HashUtility.ComputeDirectoryHash(rootLogLocation);
        }

        public IDictionary<string, object> GetAdditionalFileMetadata(LogFileContext fileContext)
        {
            // This is a compatibility shim to allow Desktop to leverage certain Server plugins which expect the "worker" field to be present.
            return new Dictionary<string, object>
            {
                { "worker", "0" }
            };
        }

        public IParserFactory GetParserFactory(string rootLogLocation)
        {
            return new ParserFactory(rootLogLocation);
        }

        #endregion IArtifactProcessor Implementation
    }
}