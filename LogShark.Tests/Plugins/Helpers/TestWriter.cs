using System.Collections.Generic;
using LogShark.Containers;
using LogShark.Writers;
using LogShark.Writers.Containers;
using Newtonsoft.Json;

namespace LogShark.Tests.Plugins.Helpers
{
    public class TestWriter<T> : IWriter<T>
    {
        public DataSetInfo DataSetInfo { get; }
        public IList<object> ReceivedObjects { get; }
        public bool WasDisposed { get; private set; }

        public TestWriter(DataSetInfo dataSetInfo)
        {
            ReceivedObjects = new List<object>();
            WasDisposed = false;
            DataSetInfo = dataSetInfo;
        }

        public void AddLine(T objectToWrite)
        {
            ReceivedObjects.Add(objectToWrite);
        }

        public void AddLines(IEnumerable<T> objectsToWrite)
        {
            foreach (var obj in objectsToWrite)
            {
                AddLine(obj);
            }
        }

        public WriterLineCounts Close()
        {
            return new WriterLineCounts(DataSetInfo, ReceivedObjects.Count, 0);
        }

        public void Dispose()
        {
            WasDisposed = true;
        }
    }
}