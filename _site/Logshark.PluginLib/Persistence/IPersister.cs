using System.Collections.Generic;

namespace Logshark.PluginLib.Persistence
{
    public interface IPersister<in T> where T : new()
    {
        /// <summary>
        /// Indicates whether this persister is currently running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// The count of items persisted over the lifetime of this persister.
        /// </summary>
        long ItemsPersisted { get; }

        /// <summary>
        /// The count of items queued but not yet persisted.
        /// </summary>
        long ItemsPendingInsertion { get; }

        /// <summary>
        /// The size of the pool for this persister.
        /// </summary>
        int GetPoolSize();

        /// <summary>
        /// Enqueues an item for persistence.
        /// </summary>
        /// <param name="item"></param>
        void Enqueue(T item);

        /// <summary>
        /// Enqueues multiple items for persistence.
        /// </summary>
        /// <param name="items"></param>
        void Enqueue(IEnumerable<T> items);

        /// <summary>
        /// Shuts the persister down and waits for any queued items to finish persisting.
        /// </summary>
        void Shutdown();
    }
}