using System;
using System.Collections.Generic;
using LogShark.Writers.Containers;

namespace LogShark.Writers
{
    public interface IWriter<T> : IDisposable
    {
        void AddLine(T objectToWrite);
        void AddLines(IEnumerable<T> objectsToWrite);
        WriterLineCounts Close();
    }
}