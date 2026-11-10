using System;
using LogShark.Writers.Containers;

namespace LogShark.Writers
{
    public interface IWriterFactory : IPluginWriterProvider, IDisposable
    {
        IWorkbookGenerator GetWorkbookGenerator();
        IWorkbookPublisher GetWorkbookPublisher(PublisherSettings publisherSettings);
    }
}
