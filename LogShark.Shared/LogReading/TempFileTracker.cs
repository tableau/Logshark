using System;
using System.Collections.Generic;
using System.IO;

namespace LogShark.LogParser
{
    public class TempFileTracker : IDisposable
    {
        private readonly IList<DirectoryInfo> _dirs = new List<DirectoryInfo>();

        public void AddDirectory(string directoryPath)
        {
            _dirs.Add(new DirectoryInfo(directoryPath));
        }
        
        public void Dispose()
        {
            foreach (var directoryInfo in _dirs)
            {
                directoryInfo.Delete(true);
            }
        }
    }
}