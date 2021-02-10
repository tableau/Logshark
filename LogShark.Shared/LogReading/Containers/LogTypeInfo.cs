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
        public Func<Stream, string, ILogReader> LogReaderProvider { get; }

        public LogTypeInfo(LogType logType, Func<Stream, string, ILogReader> logReaderProvider, List<Regex> fileLocations)
        {
            if (fileLocations == null || fileLocations.Count == 0)
            {
                throw new ArgumentException($"{nameof(fileLocations)} cannot be null or empty (encountered for log type {logType})");
            }

            LogType = logType;
            FileLocations = fileLocations;
            LogReaderProvider = logReaderProvider ?? throw new ArgumentException($"{nameof(logReaderProvider)} cannot be null or empty (encountered for log type {logType})");
        }

        public bool FileBelongsToThisType(string filename)
        {
            return FileLocations.Any(regex => regex.IsMatch(filename));
        }
    }
}