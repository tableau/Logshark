using System;
using System.Collections.Generic;
using System.Linq;

namespace Logshark.PluginLib.Persistence
{
    public abstract class BaseConcurrentPersister<T> : IPersister<T> where T : new()
    {
        internal readonly IList<IInsertionThread<T>> insertionThreadPool;
        protected int currentThreadIndex;
        protected IDictionary<Type, long> RecordsPersisted;

        public bool IsRunning { get; private set; }

        public long ItemsPersisted
        {
            get { return GetItemsPersisted(); }
        }

        protected long GetItemsPersisted()
        {
            long itemsPersisted = 0;
            try
            {
                itemsPersisted += insertionThreadPool.Sum(insertionThread => insertionThread.ItemsPersisted);
            }
            catch
            {
            }

            return itemsPersisted;
        }

        public long ItemsPendingInsertion
        {
            get { return GetItemsPendingInsertion(); }
        }

        protected long GetItemsPendingInsertion()
        {
            long itemsPendingInsertion = 0;
            try
            {
                itemsPendingInsertion += insertionThreadPool.Sum(insertionThread => insertionThread.ItemsPendingInsertion);
            }
            catch
            {
            }

            return itemsPendingInsertion;
        }

        public int GetPoolSize()
        {
            if (insertionThreadPool == null)
            {
                return 0;
            }
            else
            {
                return insertionThreadPool.Count;
            }
        }

        protected BaseConcurrentPersister(IDictionary<Type, long> recordsPersisted)
        {
            insertionThreadPool = new List<IInsertionThread<T>>();
            IsRunning = true;
            RecordsPersisted = recordsPersisted;
        }

        ~BaseConcurrentPersister()
        {
            CleanupThreadPool();
        }

        public void Enqueue(T item)
        {
            if (item == null)
            {
                return;
            }
            IInsertionThread<T> insertionThread = GetNextInsertionThread();
            insertionThread.Enqueue(item);
        }

        public void Enqueue(IEnumerable<T> items)
        {
            if (items == null)
            {
                return;
            }
            foreach (T item in items)
            {
                Enqueue(item);
            }
        }

        public void Shutdown()
        {
            foreach (IInsertionThread<T> insertionThread in insertionThreadPool)
            {
                insertionThread.Shutdown();
            }

            if (RecordsPersisted != null)
            {
                Type recordType = typeof(T);
                lock (RecordsPersisted)
                {
                    if (!RecordsPersisted.ContainsKey(recordType))
                    {
                        RecordsPersisted.Add(recordType, 0);
                    }

                    RecordsPersisted[recordType] += ItemsPersisted;
                }
            }

            IsRunning = false;
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

        protected virtual void CleanupThreadPool()
        {
            if (insertionThreadPool != null)
            {
                foreach (var insertionThread in insertionThreadPool)
                {
                    insertionThread.Destroy();
                }
            }
        }
    }
}