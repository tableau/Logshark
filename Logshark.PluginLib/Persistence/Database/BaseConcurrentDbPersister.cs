using Logshark.PluginModel.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Logshark.PluginLib.Persistence.Database
{
    public abstract class BaseConcurrentDbPersister<T> : IPersister<T> where T : new()
    {
        internal readonly IList<IInsertionThread<T>> insertionThreadPool;
        protected int currentThreadIndex;

        private bool disposed;

        public bool IsRunning { get; private set; }

        public long ItemsPersisted
        {
            get { return insertionThreadPool.Sum(insertionThread => insertionThread.ItemsPersisted); }
        }

        protected BaseConcurrentDbPersister(int persisterPoolSize = PluginLibConstants.DEFAULT_PERSISTER_POOL_SIZE)
        {
            insertionThreadPool = new List<IInsertionThread<T>>(persisterPoolSize);
            IsRunning = true;
        }

        ~BaseConcurrentDbPersister()
        {
            if (insertionThreadPool != null)
            {
                foreach (var insertionThread in insertionThreadPool)
                {
                    insertionThread.Destroy();
                }
            }

            Dispose(false);
        }

        public void Enqueue(T item)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (item == null)
            {
                return;
            }

            IInsertionThread<T> insertionThread = GetNextInsertionThread();
            insertionThread.Enqueue(item);
        }

        public void Enqueue(IEnumerable<T> items)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (items == null)
            {
                return;
            }
            foreach (T item in items)
            {
                Enqueue(item);
            }
        }

        internal IInsertionThread<T> GetNextInsertionThread()
        {
            lock (this)
            {
                if (currentThreadIndex >= insertionThreadPool.Count)
                {
                    currentThreadIndex = 0;
                }

                return insertionThreadPool[currentThreadIndex++];
            }
        }

        #region IDisposable Implementation 

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing && IsRunning)
                {
                    foreach (IInsertionThread<T> insertionThread in insertionThreadPool)
                    {
                        insertionThread.Shutdown();
                    }

                    IsRunning = false;
                }

                disposed = true;
            }
        }

        #endregion IDisposable Implementation 
    }
}