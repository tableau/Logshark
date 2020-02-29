using System.IO;

namespace LogShark.Extensions
{
    public static class PathExtensions
    {
        public static string FullyQualifyPathIfRelative(this string path, string rootDir)
        {
            if (!Path.IsPathFullyQualified(path))
            {
                return Path.Join(rootDir, path);
            }
            else
            {
                return path;
            }
        }
    }
}
