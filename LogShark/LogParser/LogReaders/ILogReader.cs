using System.Collections.Generic;
using System.IO;
using LogShark.LogParser.Containers;

namespace LogShark.LogParser.LogReaders
{
    public interface ILogReader
    {
        IEnumerable<ReadLogLineResult> ReadLines();
    }
}