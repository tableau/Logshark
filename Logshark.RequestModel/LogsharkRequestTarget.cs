using Logshark.Common.Extensions;
using Logshark.Common.Helpers;
using Logshark.RequestModel.Exceptions;
using System;
using System.IO;

namespace Logshark.RequestModel
{
    public enum LogsetTarget
    {
        Directory,
        File,
        Hash
    };

    /// <summary>
    /// Helper class that is used to hold state and context about a log processing target.
    /// </summary>
    public class LogsharkRequestTarget
    {
        // The absolute path to a log processing target, or the MD5 hash value of an existing processed target.
        public string Target { get; protected set; }

        // The type of target we're working with.
        public LogsetTarget Type { get; protected set; }

        // The size of the target, in bytes.
        public long? Size { get; protected set; }

        public LogsharkRequestTarget(string requestedTarget)
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
                    Type = LogsetTarget.Directory;
                    Size = DiskSpaceHelper.GetDirectorySize(absolutePath);
                }
                else
                {
                    Type = LogsetTarget.File;
                    Size = DiskSpaceHelper.GetFileSize(absolutePath);
                }
                Target = absolutePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            else if (requestedTarget.IsValidMD5())
            {
                // Since this isn't a path to a file or directory and it looks like an MD5, we assume it is.
                Type = LogsetTarget.Hash;
                Target = requestedTarget;
            }
            else
            {
                throw new LogsharkRequestInitializationException("Target must be a valid file, directory or MD5 hash!");
            }
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