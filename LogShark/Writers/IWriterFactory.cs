using System;
using LogShark.Containers;
using LogShark.Writers.Containers;

namespace LogShark.Writers
{
    public interface IWriterFactory : IDisposable
    {
        IWriter<T> GetWriter<T>(DataSetInfo dataSetInfo);      
        IWorkbookGenerator GetWorkbookGenerator();
        IWorkbookPublisher GetWorkbookPublisher(PublisherSettings publisherSettings);
    }
}