using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LogShark.Containers;
using LogShark.Writers;
using LogShark.Writers.Containers;

namespace LogShark.Tests.Plugins.Helpers
{
    public class TestWriterFactory : IWriterFactory
    {
        public IDictionary<DataSetInfo, object> Writers { get; }

        public TestWriterFactory()
        {
            Writers = new Dictionary<DataSetInfo, object>();
        }

        public IWriter<T> GetWriter<T>(DataSetInfo dataSetInfo)
        {
            var testWriter = new TestWriter<T>(dataSetInfo);
            Writers.Add(dataSetInfo, testWriter);
            return testWriter;
        }

        public IWorkbookGenerator GetWorkbookGenerator()
        {
            throw new System.NotImplementedException();
        }

        public IWorkbookPublisher GetWorkbookPublisher(PublisherSettings publisherSettings)
        {
            throw new System.NotImplementedException();
        }

        public void AssertAllWritersDisposedState(bool expectedDisposedState)
        {
            foreach (var (_, writer) in Writers)
            {
                var dynamicWriter = (dynamic) writer;
                ((bool) dynamicWriter.WasDisposed).Should().Be(expectedDisposedState);
            }
        }

        public TestWriter<T> GetWriterByName<T>(string name)
        {
            return Writers.First(pair => pair.Key.Name == name).Value as TestWriter<T>;
        }

        public TestWriter<T> GetOneWriterAndVerifyOthersAreEmpty<T>(string writerName, int expectedWriterCount)
        {
            Writers.Count.Should().Be(expectedWriterCount);
            TestWriter<T> testWriterToReturn = null;
            foreach (var (dataSetInfo, writer) in Writers)
            {
                if (dataSetInfo.Name == writerName)
                {
                    testWriterToReturn = writer as TestWriter<T>;
                    continue;
                }
                
                var dynamicWriter = (dynamic) writer;
                ((List<object>)dynamicWriter.ReceivedObjects).Count.Should().Be(0);
            }

            return testWriterToReturn;
        }

        public TestWriter<T> GetOneWriterAndVerifyOthersAreEmptyAndDisposed<T>(string writerName, int expectedWriterCount)
        {
            AssertAllWritersDisposedState(true);
            return GetOneWriterAndVerifyOthersAreEmpty<T>(writerName, expectedWriterCount);
        }

        public void AssertAllWritersAreDisposedAndEmpty(int expectedWriterCount)
        {
            GetOneWriterAndVerifyOthersAreEmptyAndDisposed<object>(null, expectedWriterCount);
        }

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }
    }
}