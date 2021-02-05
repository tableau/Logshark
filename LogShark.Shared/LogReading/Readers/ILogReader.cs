using System.Collections.Generic;
using LogShark.Shared.LogReading.Containers;

namespace LogShark.Shared.LogReading.Readers
{
    public interface ILogReader
    {
        IEnumerable<ReadLogLineResult> ReadLines();
    }
}