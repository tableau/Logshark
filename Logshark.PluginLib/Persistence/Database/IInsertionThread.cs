namespace Logshark.PluginLib.Persistence.Database
{
    internal interface IInsertionThread<in T> where T : new()
    {
        bool IsRunning { get; }
        long ItemsPersisted { get; }
        int ItemsPendingInsertion { get; }

        void Enqueue(T item);

        void Shutdown();

        void Destroy();
    }
}