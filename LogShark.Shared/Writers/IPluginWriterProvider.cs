using LogShark.Containers;

namespace LogShark.Writers
{
    public interface IPluginWriterProvider
    {
        IWriter<T> GetWriter<T>(DataSetInfo dataSetInfo);
    }
}
