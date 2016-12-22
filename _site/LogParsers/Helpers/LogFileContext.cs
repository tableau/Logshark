using System;
using System.IO;

namespace LogParsers.Helpers
{
    /// <summary>
    /// Provides context around a logfile.
    /// </summary>
    public class LogFileContext
    {
        // Name of physical file, including extension.
        public string FileName { get; private set; }

        // Logical filename, used for presentation purposes.
        public string LogicalFileName { get; private set; }

        // Absolute path on disk to the file.
        public string FilePath { get; private set; }

        // Absolute path to the root of the associated logset.
        public string RootLogDirectory { get; private set; }

        // This file's location relative to the logset root.
        public string FileLocationRelativeToRoot { get; private set; }

        // Size of this file, in bytes.
        public long FileSize { get; private set; }

        // The time at which the file was last modified, in UTC.
        public DateTime LastWriteTime { get; private set; }

        // The index of the Tableau Server worker associated with this logfile.
        public int WorkerIndex { get; private set; }

        // The line number offset of this logfile, used to preserve state if this is a portion of a different file.
        public long LineOffset { get; private set; }

        public LogFileContext(string absoluteFilePath, string rootLogDirectory, string logicalFileName = null, long lineOffset = 0)
        {
            FileName = Path.GetFileName(absoluteFilePath);
            LogicalFileName = logicalFileName ?? FileName;
            LineOffset = lineOffset;
            RootLogDirectory = rootLogDirectory;
            FileLocationRelativeToRoot = String.Join(@"\", ParserUtil.GetParentLogDirs(absoluteFilePath, RootLogDirectory));
            FilePath = absoluteFilePath;
            var fileInfo = new FileInfo(absoluteFilePath);
            if (fileInfo.Exists)
            {
                FileSize = fileInfo.Length;
                LastWriteTime = fileInfo.LastWriteTimeUtc;
            }
            WorkerIndex = ParserUtil.GetWorkerIndex(absoluteFilePath, RootLogDirectory);
        }

        public override string ToString()
        {
            if (String.IsNullOrWhiteSpace(FileLocationRelativeToRoot))
            {
                return FileName;
            }

            return String.Format(@"{0}\{1}", FileLocationRelativeToRoot, FileName);
        }
    }
}
