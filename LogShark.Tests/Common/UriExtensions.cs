using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LogShark.Tests.Common
{
    public static class UriExtensions
    {
        public static string GetRelativePathFrom(this Uri sourceUri, Uri targetUri)
        {
            return Uri.UnescapeDataString(sourceUri.MakeRelativeUri(targetUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        public static string GetRelativePathFromCurrentDirectory(this Uri targetUri)
        {
            var folder = Directory.GetCurrentDirectory();
            if (!folder.EndsWith(Path.DirectorySeparatorChar))
            {
                folder += Path.DirectorySeparatorChar;
            }

            return new Uri(folder).GetRelativePathFrom(targetUri);
        }
    }
}
