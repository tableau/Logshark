using System.Collections.Generic;

namespace LogShark.Shared.LogReading.Containers
{
    public class LogSetInfo
    {
        public List<string> FilePaths { get; }
        public bool IsZip { get; }
        public string Path { get; }
        public string Prefix { get; }
        public string RootPath { get; }

        public LogSetInfo(List<string> filePaths, string path, string prefix, bool isZip, string rootPath)
        {
            FilePaths = filePaths;
            IsZip = isZip;
            Path = path;
            Prefix = prefix;
            RootPath = rootPath;
        }
    }
}

