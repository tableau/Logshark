namespace Logshark.PluginLib.Persistence
{
    public interface IPersisterFactory<in T> where T : new()
    {
        IPersister<T> BuildPersister();
    }
}