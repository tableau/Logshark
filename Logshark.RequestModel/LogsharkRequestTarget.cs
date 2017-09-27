using Logshark.Common.Extensions;
using Logshark.Common.Helpers;
using Logshark.RequestModel.Exceptions;
using System;
using System.IO;

namespace Logshark.RequestModel
{
    /// <summary>
    /// Helper class that is used to hold state and context about a log processing target.
    /// </summary>
    public class LogsharkRequestTarget
    {
        // The absolute path to a log processing target, or the MD5 hash value of an existing processed target.
        public string Target { get; set; }

        // The "original" unmodified target.  We keep this around for tracking/metadata purposes.
        public string OriginalTarget { get; protected set; }

        // Indicates whether Target is a hash id of an existing processed logset.
        public bool IsHashId { get; protected set; }

        // Indicates whether Target is a directory.
        public bool IsDirectory { get; protected set; }

        // Indicates whether Target is a file.
        public bool IsFile { get; protected set; }

        // The uncompressed size of the Target, in bytes.
        public long UncompressedSize { get; set; }

        // The compressed size of the Target, in bytes.
        public long? CompressedSize { get; set; }

        // The size of what was actually required to process in order to fulfill the request, in bytes.
        public long? ProcessedSize { get; set; }

        internal LogsharkRequestTarget(string requestedTarget)
        {
            if (String.IsNullOrWhiteSpace(requestedTarget))
            {
                throw new LogsharkRequestInitializationException("Invalid request: No logset target specified!");
            }

            // A target can either be an MD5 logset hash or a file/UNC path.
            if (PathHelper.IsPathToExistingResource(requestedTarget))
            {
                // If target is a file or directory, we want to use the absolute path and trim any trailing
                // directory separator characters to establish consistency.
                string absolutePath = PathHelper.GetAbsolutePath(requestedTarget);
                if (PathHelper.IsDirectory(absolutePath))
                {
                    IsDirectory = true;
                    UncompressedSize = DiskSpaceHelper.GetDirectorySize(absolutePath);
                }
                else
                {
                    IsFile = true;
                    UncompressedSize = 0;
                    CompressedSize = DiskSpaceHelper.GetFileSize(absolutePath);
                }
                Target = absolutePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            else if (requestedTarget.IsValidMD5())
            {
                // Since this isn't a path to a file or directory and it looks like an MD5, we assume it is.
                IsHashId = true;
                Target = requestedTarget;
            }
            else
            {
                throw new LogsharkRequestInitializationException("Target must be a valid file, directory or MD5 hash!");
            }

            OriginalTarget = Target;
        }

        // Implicit conversion to allow LogsharkRequestTarget instances to be used like strings.
        public static implicit operator string(LogsharkRequestTarget requestTarget)
        {
            return requestTarget.Target;
        }

        public override string ToString()
        {
            return Target;
        }
    }
}