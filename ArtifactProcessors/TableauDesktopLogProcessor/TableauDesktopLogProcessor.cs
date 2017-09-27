using LogParsers.Base;
using Logshark.ArtifactProcessorModel;
using Logshark.Common.Helpers;
using Logshark.PluginLib.Model;
using Logshark.RequestModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Tableau.DesktopLogProcessor.Parsing;

namespace Tableau.DesktopLogProcessor
{
    public class TableauDesktopLogProcessor : IArtifactProcessor
    {
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

        public IParserFactory GetParserFactory(string rootLogLocation)
        {
            return new ParserFactory(rootLogLocation);
        }

        public ISet<Regex> SupportedFilePatterns
        {
            get { return supportedFilePatterns; }
        }

        public ISet<Type> SupportedPluginInterfaces
        {
            get { return supportedPluginInterfaces; }
        }

        public ISet<string> RequiredCollections
        {
            get { return new HashSet<string>(); }
        }

        public bool CanProcess(LogsharkRequest request)
        {
            return IsDesktopLogSet(request.RunContext.RootLogDirectory);
        }

        public string ComputeArtifactHash(LogsharkRequest request)
        {
            return LogsetHashUtil.GetLogSetHash(request.Target);
        }

        #endregion IArtifactProcessor Implementation

        private static bool IsDesktopLogSet(string rootLogDirectory)
        {
            bool hasTabsvcYmlFile = File.Exists(Path.Combine(rootLogDirectory, "tabsvc.yml"));

            // Given that these logs get zipped by hand usually we need to check either the root or the Logs subdirectory.
            bool hasLogTxtInRoot = File.Exists(Path.Combine(rootLogDirectory, "log.txt"));
            bool hasLogTxtInLogsSubdir = File.Exists(Path.Combine(rootLogDirectory, "Logs", "log.txt"));

            // If we don't have a tabsvc.yml file then we know it's not a server log.
            // If we have a log.txt then we know it's most likely a desktop log.
            return !hasTabsvcYmlFile && (hasLogTxtInRoot || hasLogTxtInLogsSubdir);
        }
    }
}