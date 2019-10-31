using System;
using System.Collections.Generic;

namespace Logshark.PluginModel.Model
{
    public interface IPersister<in T> : IDisposable where T : new()
    {
        /// <summary>
        /// The count of items persisted over the lifetime of this persister.
        /// </summary>
        long ItemsPersisted { get; }

        /// <summary>
        /// Enqueues an item for persistence.
        /// </summary>
        void Enqueue(T item);

        /// <summary>
        /// Enqueues multiple items for persistence.
        /// </summary>
        void Enqueue(IEnumerable<T> items);
    }
}