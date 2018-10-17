using log4net;
using Logshark.Common.Extensions;
using Logshark.PluginModel.Model;
using System;
using System.IO;
using System.Reflection;

namespace Logshark.PluginLib.Persistence.Extract
{
    public sealed class ExtractPersisterFactory : IExtractPersisterFactory
    {
        private readonly string extractOutputDirectory;
        private readonly ILog pluginLog;
        private readonly string customTempDirectoryPath;
        private readonly string customLogDirectoryPath;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public ExtractPersisterFactory(string extractOutputDirectory, ILog pluginLog = null, string customTempDirectoryPath = null, string customLogDirectoryPath = null)
        {
            this.extractOutputDirectory = extractOutputDirectory;
            this.pluginLog = pluginLog;
            this.customTempDirectoryPath = customTempDirectoryPath;
            this.customLogDirectoryPath = customLogDirectoryPath;
        }

        public IPersister<T> CreateExtract<T>(Action<T> insertionCallback = null) where T : new()
        {
            return CreateExtract(String.Concat(typeof(T).Name.Pluralize(2), ".hyper"), insertionCallback);
        }

        public IPersister<T> CreateExtract<T>(string extractFilename, Action<T> insertionCallback = null) where T : new()
        {
            if (String.IsNullOrWhiteSpace(extractOutputDirectory) || extractOutputDirectory.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                throw new ArgumentException("Must provide a valid extract output directory", extractOutputDirectory);
            }
            if (String.IsNullOrWhiteSpace(extractFilename) || extractFilename.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new ArgumentException("Must provide a valid extract filename", extractOutputDirectory);
            }

            Log.InfoFormat("Building new extract '{0}'..", extractFilename);

            string extractFilePath = Path.Combine(extractOutputDirectory, extractFilename);

            return new ExtractPersister<T>(extractFilePath, insertionCallback, pluginLog, customTempDirectoryPath, customLogDirectoryPath);
        }
    }
}