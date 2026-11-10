using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LogShark.Shared.LogReading.Readers;

namespace LogShark.Shared.LogReading.Containers
{
    public class LogTypeInfo
    {
        public LogType LogType { get; }
        public List<Regex> FileLocations { get; }
        // Filename-only patterns used in adhoc mode, where files are dumped flat and the folder
        // structure the regular FileLocations rely on is absent. Null means the log type is not
        // matchable in adhoc mode (e.g. types that can only be identified by their folder path).
        public List<Regex> AdhocFileLocations { get; }
        public Func<Stream, string, ILogReader> LogReaderProvider { get; }

        public LogTypeInfo(LogType logType, Func<Stream, string, ILogReader> logReaderProvider, List<Regex> fileLocations, List<Regex> adhocFileLocations = null)
        {
            if (fileLocations == null || fileLocations.Count == 0)
            {
                throw new ArgumentException($"{nameof(fileLocations)} cannot be null or empty (encountered for log type {logType})");
            }

            LogType = logType;
            FileLocations = fileLocations;
            AdhocFileLocations = adhocFileLocations;
            LogReaderProvider = logReaderProvider ?? throw new ArgumentException($"{nameof(logReaderProvider)} cannot be null or empty (encountered for log type {logType})");
        }

        public bool FileBelongsToThisType(string filename)
        {
            return FileLocations.Any(regex => regex.IsMatch(filename));
        }

        public bool FileBelongsToThisType(string filename, bool adhocMode)
        {
            if (!adhocMode)
            {
                return FileBelongsToThisType(filename);
            }

            // In adhoc mode we only match against filename-only patterns. Log types without adhoc
            // patterns are not classifiable from a flat file dump and are skipped.
            return AdhocFileLocations != null && AdhocFileLocations.Any(regex => regex.IsMatch(filename));
        }
    }
}