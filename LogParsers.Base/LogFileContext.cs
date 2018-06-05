using LogParsers.Base.Helpers;
using System;
using System.Collections.Generic;
using System.IO;

namespace LogParsers.Base
{
    /// <summary>
    /// Provides context around a logfile.
    /// </summary>
    public sealed class LogFileContext
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

        // The line number offset of this logfile, used to preserve state if this is a portion of a different file.
        public long LineOffset { get; private set; }

        // Stores any artifact-specific information about this file.
        public IDictionary<string, object> ArtifactSpecificFileMetadata { get; private set; }

        public LogFileContext(string absoluteFilePath, string rootLogDirectory, Func<LogFileContext, IDictionary<string, object>> metadataRetrievalCallback = null, string logicalFileName = null, long lineOffset = 0)
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
            ArtifactSpecificFileMetadata = GetArtifactSpecificFileMetadata(metadataRetrievalCallback);
        }

        public override string ToString()
        {
            if (String.IsNullOrWhiteSpace(FileLocationRelativeToRoot))
            {
                return FileName;
            }

            return String.Format(@"{0}\{1}", FileLocationRelativeToRoot, FileName);
        }

        /// <summary>
        /// Safely invokes a callback to retrieve artifact-specific file metadata for this log file context. We handle it this way for a couple of reasons:
        ///   1. We want to provide flexibility.
        ///   2. We can't trust that an external class did due diligence with regards to exception handling.
        ///   3. We want to minimize the amount that this assembly needs to know about how the rest of the application works.
        /// </summary>
        private IDictionary<string, object> GetArtifactSpecificFileMetadata(Func<LogFileContext, IDictionary<string, object>> metadataRetrievalCallback)
        {
            if (metadataRetrievalCallback == null)
            {
                return new Dictionary<string, object>();
            }

            try
            {
                IDictionary<string, object> metadata = metadataRetrievalCallback.Invoke(this);
                return metadata ?? new Dictionary<string, object>();
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }
    }
}