namespace LogShark.LogParser.Containers
{
    public class LogSetInfo
    {
        public bool IsZip { get; }
        public string Path { get; }
        public string Prefix { get; }
        public string RootPath { get; }

        public LogSetInfo(string path, string prefix, bool isZip, string rootPath)
        {
            IsZip = isZip;
            Path = path;
            Prefix = prefix;
            RootPath = rootPath;
        }
    }
}